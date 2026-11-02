using System.Text.Json;
using GetABursary.Core.Data;
using GetABursary.Core.Domain.Common.Enums;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;

namespace GetABursary.Core.Repositories.Bursaries;

public sealed class BursaryReadRepository(GetABursaryDbContext db) : IBursaryReadRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    // One statement: eligibility filter + score (filtered), total count, ranking and paging (page), then funder and
    // sources for the page only. A LEFT JOIN from the count row guarantees totalCount even for an empty page.
    // Score: +3 qualification listed, best field match (+2 second-order, +1 first-order), +0.5 all fields,
    // -0.5 unknown study levels. Closed bursaries always sort last.
    private const string SearchSql = """
        WITH filtered AS (
            SELECT b.id, b.slug, b.name, b.funder_id, b.closing_date, b.status, b.study_levels,
                   b.min_average, b.coverage, b.created_at,
                   (CASE WHEN EXISTS (SELECT 1 FROM bursary_qualification bq
                                      WHERE bq.bursary_id = b.id AND bq.qualification_id = @qualification_id)
                         THEN 3.0 ELSE 0.0 END
                    + COALESCE((SELECT MAX(CASE WHEN f.parent_id IS NOT NULL THEN 2.0 ELSE 1.0 END)
                                FROM bursary_field bf
                                JOIN field_of_study f ON f.id = bf.field_of_study_id
                                WHERE bf.bursary_id = b.id AND bf.field_of_study_id = ANY(@field_ids)), 0.0)
                    + CASE WHEN b.all_fields_eligible THEN 0.5 ELSE 0.0 END
                    - CASE WHEN cardinality(b.study_levels) = 0 THEN 0.5 ELSE 0.0 END) AS score
            FROM bursary b
            WHERE b.is_published
              AND (@include_closed OR b.status <> 'Closed')
              AND (
                    (@qualification_id IS NULL AND cardinality(@field_ids) = 0)
                    OR b.all_fields_eligible
                    OR EXISTS (SELECT 1 FROM bursary_field bf
                               WHERE bf.bursary_id = b.id AND bf.field_of_study_id = ANY(@field_ids))
                    OR EXISTS (SELECT 1 FROM bursary_qualification bq
                               WHERE bq.bursary_id = b.id AND bq.qualification_id = @qualification_id)
                  )
              AND (@institution_id IS NULL
                   OR NOT EXISTS (SELECT 1 FROM bursary_institution bi WHERE bi.bursary_id = b.id)
                   OR EXISTS (SELECT 1 FROM bursary_institution bi
                              WHERE bi.bursary_id = b.id AND bi.institution_id = @institution_id))
              AND (@study_level IS NULL OR cardinality(b.study_levels) = 0 OR @study_level = ANY(b.study_levels))
              AND (@average IS NULL OR b.min_average IS NULL OR b.min_average <= @average)
              AND (@closing_from IS NULL OR b.closing_date >= @closing_from)
              AND (@closing_to IS NULL OR b.closing_date <= @closing_to)
        ),
        total AS (SELECT count(*) AS total_count FROM filtered),
        ranked AS (
            SELECT f.*, ROW_NUMBER() OVER (
                ORDER BY (CASE WHEN f.status = 'Closed' THEN 1 ELSE 0 END),
                         (CASE WHEN @sort = 0 THEN f.score END) DESC NULLS LAST,
                         (CASE WHEN @sort = 2 THEN f.created_at END) DESC NULLS LAST,
                         f.closing_date ASC NULLS LAST,
                         f.score DESC,
                         f.id) AS ord
            FROM filtered f
        ),
        page AS (SELECT * FROM ranked WHERE ord > @skip AND ord <= @skip + @take)
        SELECT t.total_count AS total_count,
               p.id AS id, p.slug AS slug, p.name AS name, fu.name AS funder_name,
               p.closing_date AS closing_date, p.status AS status, p.study_levels AS study_levels,
               p.min_average AS min_average, p.coverage AS coverage, p.score AS score,
               (SELECT COALESCE(json_agg(json_build_object('key', s.key, 'name', s.name,
                                                           'iconUrl', s.icon_url, 'url', bs.source_url)
                                         ORDER BY s.name, bs.source_url), '[]'::json)::text
                FROM bursary_source bs JOIN source s ON s.id = bs.source_id
                WHERE bs.bursary_id = p.id) AS sources_json
        FROM total t
        LEFT JOIN page p ON TRUE
        LEFT JOIN funder fu ON fu.id = p.funder_id
        ORDER BY p.ord
        """;

    public async Task<BursarySearchPage> SearchAsync(BursarySearchCriteria criteria, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(criteria);

        var parameters = new object[]
        {
            Param("qualification_id", NpgsqlDbType.Uuid, criteria.QualificationId),
            new NpgsqlParameter("field_ids", NpgsqlDbType.Array | NpgsqlDbType.Uuid) { Value = criteria.FieldIds.ToArray() },
            Param("institution_id", NpgsqlDbType.Uuid, criteria.InstitutionId),
            Param("study_level", NpgsqlDbType.Text, criteria.StudyLevel?.ToString()),
            Param("average", NpgsqlDbType.Numeric, criteria.Average),
            Param("closing_from", NpgsqlDbType.Date, criteria.ClosingFrom),
            Param("closing_to", NpgsqlDbType.Date, criteria.ClosingTo),
            new NpgsqlParameter("include_closed", NpgsqlDbType.Boolean) { Value = criteria.IncludeClosed },
            new NpgsqlParameter("sort", NpgsqlDbType.Integer) { Value = (int)criteria.Sort },
            new NpgsqlParameter("skip", NpgsqlDbType.Integer) { Value = (criteria.Page - 1) * criteria.PageSize },
            new NpgsqlParameter("take", NpgsqlDbType.Integer) { Value = criteria.PageSize },
        };

        var rows = await db.Database
            .SqlQueryRaw<SearchRecord>(SearchSql, parameters)
            .ToListAsync(ct);

        var total = rows.Count > 0 ? (int)rows[0].TotalCount : 0;
        var items = rows
            .Where(r => r.Id is not null)
            .Select(ToRow)
            .ToList();

        return new BursarySearchPage(items, total);
    }

    private static NpgsqlParameter Param<T>(string name, NpgsqlDbType type, T? value) =>
        new(name, type) { Value = value is null ? DBNull.Value : value };

    private static BursarySearchRow ToRow(SearchRecord r)
    {
        var sources = JsonSerializer.Deserialize<List<SourceJson>>(r.SourcesJson ?? "[]", JsonOptions) ?? [];

        return new BursarySearchRow(
            r.Id!.Value,
            r.Slug!,
            r.Name!,
            r.FunderName,
            r.ClosingDate,
            Enum.Parse<BursaryStatus>(r.Status!),
            (r.StudyLevels ?? []).Select(Enum.Parse<StudyLevel>).ToList(),
            r.MinAverage,
            (r.Coverage ?? []).Select(Enum.Parse<CoverageItem>).ToList(),
            sources.Select(s => new BursarySearchSource(s.Key, s.Name, s.IconUrl, s.Url)).ToList(),
            r.Score ?? 0m);
    }

    private sealed record SourceJson(string Key, string Name, string? IconUrl, string Url);

    /// <summary>Column-for-column shape of the search result set; all but the total are null on an empty page.</summary>
    private sealed class SearchRecord
    {
        public long TotalCount { get; set; }

        public Guid? Id { get; set; }

        public string? Slug { get; set; }

        public string? Name { get; set; }

        public string? FunderName { get; set; }

        public DateOnly? ClosingDate { get; set; }

        public string? Status { get; set; }

        public string[]? StudyLevels { get; set; }

        public decimal? MinAverage { get; set; }

        public string[]? Coverage { get; set; }

        public decimal? Score { get; set; }

        public string? SourcesJson { get; set; }
    }
}
