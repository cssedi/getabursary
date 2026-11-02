using System.Text.Json;
using GetABursary.Core.Domain.Bursaries;
using GetABursary.Core.Domain.Common.Enums;
using GetABursary.Core.Domain.Taxonomy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GetABursary.Core.Data.Seeding;

/// <summary>
/// Upserts reference data from JSON into the database, in one transaction per run.
/// Keys: field by <c>code</c>, institution by <c>slug</c>, source by <c>key</c>, faculty by (institution, <c>slug</c>),
/// qualification by (faculty, <c>slug</c>). Rows are never deleted. <c>source.enabled</c> is never overwritten once the row exists.
/// </summary>
public sealed partial class DataSeeder(GetABursaryDbContext db, string seedDirectory, ILogger<DataSeeder> logger) : IDataSeeder
{
    private const string FieldsFile = "fields-of-study.json";
    private const string InstitutionsFile = "institutions.json";
    private const string SourcesFile = "sources.json";
    private const string FacultiesFolder = "faculties";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>Returns the configured seed folder, or <c>seed</c> beside the application when nothing is configured.</summary>
    public static string ResolveSeedDirectory(string? configuredPath) =>
        string.IsNullOrWhiteSpace(configuredPath) ? Path.Combine(AppContext.BaseDirectory, "seed") : configuredPath;

    public async Task<SeedResult> SeedAsync(CancellationToken ct)
    {
        var fields = await ReadJsonAsync<List<FieldOfStudyFile>>(Path.Combine(seedDirectory, FieldsFile), ct);
        var institutions = await ReadJsonAsync<List<InstitutionFile>>(Path.Combine(seedDirectory, InstitutionsFile), ct);
        var sources = await ReadJsonAsync<List<SourceFile>>(Path.Combine(seedDirectory, SourcesFile), ct);

        var results = new List<SeedFileResult>();

        var fieldsByCode = await UpsertFieldsAsync(fields, results, ct);
        var institutionsBySlug = await UpsertInstitutionsAsync(institutions, results, ct);
        await UpsertSourcesAsync(sources, results, ct);
        await UpsertFacultiesAsync(institutionsBySlug, fieldsByCode, results, ct);

        await db.SaveChangesAsync(ct);

        foreach (var result in results)
        {
            LogSeedFile(logger, result.File, result.Inserted, result.Updated);
        }

        return new SeedResult(results);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Seed {File}: {Inserted} inserted, {Updated} updated")]
    private static partial void LogSeedFile(ILogger logger, string file, int inserted, int updated);

    private async Task<Dictionary<string, FieldOfStudy>> UpsertFieldsAsync(List<FieldOfStudyFile> files, List<SeedFileResult> results, CancellationToken ct)
    {
        EnsureUnique(files, f => f.Code, FieldsFile, "code");
        EnsureUnique(files, f => f.Slug, FieldsFile, "slug");

        var filesByCode = files.ToDictionary(f => f.Code);
        var byCode = await db.FieldsOfStudy.ToDictionaryAsync(f => f.Code, ct);
        var inserted = 0;
        var updated = 0;

        // Parents first, so a child can point at a parent that was inserted in this same run.
        foreach (var item in files.OrderBy(f => ParentDepth(f, filesByCode, 0)))
        {
            var parent = item.ParentCode is null ? null : byCode[item.ParentCode];

            if (!byCode.TryGetValue(item.Code, out var field))
            {
                byCode[item.Code] = new FieldOfStudy { Code = item.Code, Name = item.Name, Slug = item.Slug, Parent = parent };
                db.FieldsOfStudy.Add(byCode[item.Code]);
                inserted++;
                continue;
            }

            var changed = false;
            if (field.Name != item.Name)
            {
                field.Name = item.Name;
                changed = true;
            }

            if (field.Slug != item.Slug)
            {
                field.Slug = item.Slug;
                changed = true;
            }

            if (field.ParentId != parent?.Id)
            {
                field.ParentId = parent?.Id;
                changed = true;
            }

            if (changed)
            {
                updated++;
            }
        }

        results.Add(new SeedFileResult(FieldsFile, inserted, updated));
        return byCode;
    }

    private async Task<Dictionary<string, Institution>> UpsertInstitutionsAsync(List<InstitutionFile> files, List<SeedFileResult> results, CancellationToken ct)
    {
        EnsureUnique(files, f => f.Slug, InstitutionsFile, "slug");
        EnsureUnique(files, f => f.ShortName, InstitutionsFile, "shortName");

        var bySlug = await db.Institutions.ToDictionaryAsync(i => i.Slug, ct);
        var inserted = 0;
        var updated = 0;

        foreach (var item in files)
        {
            var type = ParseEnum<InstitutionType>(item.Type, InstitutionsFile, "type");
            var province = ParseEnum<Province>(item.Province, InstitutionsFile, "province");

            if (!bySlug.TryGetValue(item.Slug, out var institution))
            {
                bySlug[item.Slug] = new Institution
                {
                    Name = item.Name,
                    ShortName = item.ShortName,
                    Slug = item.Slug,
                    Type = type,
                    Province = province,
                    WebsiteUrl = item.WebsiteUrl,
                };
                db.Institutions.Add(bySlug[item.Slug]);
                inserted++;
                continue;
            }

            var changed = false;
            if (institution.Name != item.Name)
            {
                institution.Name = item.Name;
                changed = true;
            }

            if (institution.ShortName != item.ShortName)
            {
                institution.ShortName = item.ShortName;
                changed = true;
            }

            if (institution.Type != type)
            {
                institution.Type = type;
                changed = true;
            }

            if (institution.Province != province)
            {
                institution.Province = province;
                changed = true;
            }

            if (institution.WebsiteUrl != item.WebsiteUrl)
            {
                institution.WebsiteUrl = item.WebsiteUrl;
                changed = true;
            }

            if (changed)
            {
                updated++;
            }
        }

        results.Add(new SeedFileResult(InstitutionsFile, inserted, updated));
        return bySlug;
    }

    private async Task UpsertSourcesAsync(List<SourceFile> files, List<SeedFileResult> results, CancellationToken ct)
    {
        EnsureUnique(files, f => f.Key, SourcesFile, "key");

        var byKey = await db.Sources.ToDictionaryAsync(s => s.Key, ct);
        var inserted = 0;
        var updated = 0;

        foreach (var item in files)
        {
            if (!byKey.TryGetValue(item.Key, out var source))
            {
                byKey[item.Key] = new Source
                {
                    Key = item.Key,
                    Name = item.Name,
                    BaseUrl = item.BaseUrl,
                    IconUrl = item.IconUrl,
                    Enabled = item.Enabled,
                    CrawlDelayMs = item.CrawlDelayMs,
                };
                db.Sources.Add(byKey[item.Key]);
                inserted++;
                continue;
            }

            // Enabled is deliberately not compared or written: admins control it once the row exists.
            var changed = false;
            if (source.Name != item.Name)
            {
                source.Name = item.Name;
                changed = true;
            }

            if (source.BaseUrl != item.BaseUrl)
            {
                source.BaseUrl = item.BaseUrl;
                changed = true;
            }

            if (source.IconUrl != item.IconUrl)
            {
                source.IconUrl = item.IconUrl;
                changed = true;
            }

            if (source.CrawlDelayMs != item.CrawlDelayMs)
            {
                source.CrawlDelayMs = item.CrawlDelayMs;
                changed = true;
            }

            if (changed)
            {
                updated++;
            }
        }

        results.Add(new SeedFileResult(SourcesFile, inserted, updated));
    }

    private async Task UpsertFacultiesAsync(
        IReadOnlyDictionary<string, Institution> institutions,
        IReadOnlyDictionary<string, FieldOfStudy> fields,
        List<SeedFileResult> results,
        CancellationToken ct)
    {
        var folder = Path.Combine(seedDirectory, FacultiesFolder);
        if (!Directory.Exists(folder))
        {
            return;
        }

        var seenInstitutions = new HashSet<string>(StringComparer.Ordinal);
        foreach (var path in Directory.GetFiles(folder, "*.json").Order(StringComparer.Ordinal))
        {
            var name = $"{FacultiesFolder}/{Path.GetFileName(path)}";
            var file = await ReadJsonAsync<FacultiesFile>(path, ct);

            if (!seenInstitutions.Add(file.InstitutionSlug))
            {
                throw new InvalidDataException($"{name}: more than one faculties file for institution '{file.InstitutionSlug}'.");
            }

            results.Add(await UpsertFacultiesFileAsync(name, file, institutions, fields, ct));
        }
    }

    private async Task<SeedFileResult> UpsertFacultiesFileAsync(
        string name,
        FacultiesFile file,
        IReadOnlyDictionary<string, Institution> institutions,
        IReadOnlyDictionary<string, FieldOfStudy> fields,
        CancellationToken ct)
    {
        if (!institutions.TryGetValue(file.InstitutionSlug, out var institution))
        {
            throw new InvalidDataException($"{name}: institutionSlug '{file.InstitutionSlug}' is not in {InstitutionsFile}.");
        }

        EnsureUnique(file.Faculties, f => f.Slug, name, "faculty slug");

        var facultiesBySlug = await db.Faculties
            .Where(f => f.InstitutionId == institution.Id)
            .Include(f => f.Qualifications)
                .ThenInclude(q => q.QualificationFields)
            .ToDictionaryAsync(f => f.Slug, ct);

        var inserted = 0;
        var updated = 0;

        foreach (var facultyFile in file.Faculties)
        {
            if (!facultiesBySlug.TryGetValue(facultyFile.Slug, out var faculty))
            {
                faculty = new Faculty { Name = facultyFile.Name, Slug = facultyFile.Slug, Institution = institution };
                db.Faculties.Add(faculty);
                facultiesBySlug[facultyFile.Slug] = faculty;
                inserted++;
            }
            else if (faculty.Name != facultyFile.Name)
            {
                faculty.Name = facultyFile.Name;
                updated++;
            }

            EnsureUnique(facultyFile.Qualifications, q => q.Slug, name, $"qualification slug in faculty '{facultyFile.Slug}'");
            var qualificationsBySlug = faculty.Qualifications.ToDictionary(q => q.Slug);

            foreach (var item in facultyFile.Qualifications)
            {
                var studyLevel = ParseEnum<StudyLevel>(item.StudyLevel, name, "studyLevel");
                var linkedFields = ResolveFields(item, fields, name);

                if (!qualificationsBySlug.TryGetValue(item.Slug, out var qualification))
                {
                    qualification = new Qualification
                    {
                        Name = item.Name,
                        Abbreviation = item.Abbreviation ?? string.Empty,
                        StudyLevel = studyLevel,
                        NqfLevel = item.NqfLevel,
                        DurationYears = item.DurationYears,
                        Slug = item.Slug,
                        Faculty = faculty,
                    };
                    db.Qualifications.Add(qualification);
                    qualificationsBySlug[item.Slug] = qualification;
                    _ = SyncFieldLinks(qualification, linkedFields);
                    inserted++;
                    continue;
                }

                var changed = false;
                if (qualification.Name != item.Name)
                {
                    qualification.Name = item.Name;
                    changed = true;
                }

                if (qualification.Abbreviation != (item.Abbreviation ?? string.Empty))
                {
                    qualification.Abbreviation = item.Abbreviation ?? string.Empty;
                    changed = true;
                }

                if (qualification.StudyLevel != studyLevel)
                {
                    qualification.StudyLevel = studyLevel;
                    changed = true;
                }

                if (qualification.NqfLevel != item.NqfLevel)
                {
                    qualification.NqfLevel = item.NqfLevel;
                    changed = true;
                }

                if (qualification.DurationYears != item.DurationYears)
                {
                    qualification.DurationYears = item.DurationYears;
                    changed = true;
                }

                if (SyncFieldLinks(qualification, linkedFields))
                {
                    changed = true;
                }

                if (changed)
                {
                    updated++;
                }
            }
        }

        return new SeedFileResult(name, inserted, updated);
    }

    /// <summary>Makes the qualification's field links exactly <paramref name="wanted"/>. Returns true if any link changed.</summary>
    private bool SyncFieldLinks(Qualification qualification, IReadOnlyList<FieldOfStudy> wanted)
    {
        var wantedIds = wanted.Select(f => f.Id).ToHashSet();
        var changed = false;

        foreach (var link in qualification.QualificationFields.Where(l => !wantedIds.Contains(l.FieldOfStudyId)).ToList())
        {
            db.QualificationFields.Remove(link);
            changed = true;
        }

        foreach (var field in wanted)
        {
            if (qualification.QualificationFields.Any(l => l.FieldOfStudyId == field.Id))
            {
                continue;
            }

            qualification.QualificationFields.Add(new QualificationField { Qualification = qualification, FieldOfStudy = field });
            changed = true;
        }

        return changed;
    }

    private static List<FieldOfStudy> ResolveFields(QualificationFile item, IReadOnlyDictionary<string, FieldOfStudy> fields, string name) =>
        item.FieldCodes.Distinct().Select(code =>
            fields.TryGetValue(code, out var field)
                ? field
                : throw new InvalidDataException($"{name}: qualification '{item.Slug}' cites unknown field code '{code}'.")).ToList();

    private static int ParentDepth(FieldOfStudyFile item, IReadOnlyDictionary<string, FieldOfStudyFile> filesByCode, int depth)
    {
        if (item.ParentCode is null)
        {
            return 0;
        }

        if (depth > filesByCode.Count || !filesByCode.TryGetValue(item.ParentCode, out var parent))
        {
            throw new InvalidDataException($"{FieldsFile}: field '{item.Code}' has an unknown or circular parentCode '{item.ParentCode}'.");
        }

        return 1 + ParentDepth(parent, filesByCode, depth + 1);
    }

    private static async Task<T> ReadJsonAsync<T>(string path, CancellationToken ct) where T : class
    {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
        try
        {
            return await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, ct)
                ?? throw new InvalidDataException($"{path} is empty.");
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"{path} is not valid seed JSON: {ex.Message}", ex);
        }
    }

    private static TEnum ParseEnum<TEnum>(string value, string file, string field) where TEnum : struct, Enum
    {
        if (Enum.TryParse<TEnum>(value, ignoreCase: false, out var parsed) && Enum.IsDefined(parsed))
        {
            return parsed;
        }

        throw new InvalidDataException($"{file}: '{value}' is not a valid {typeof(TEnum).Name} for {field}.");
    }

    private static void EnsureUnique<T>(IEnumerable<T> items, Func<T, string> key, string file, string what)
    {
        var duplicate = items.GroupBy(key).FirstOrDefault(g => g.Count() > 1);
        if (duplicate is not null)
        {
            throw new InvalidDataException($"{file}: duplicate {what} '{duplicate.Key}'.");
        }
    }
}
