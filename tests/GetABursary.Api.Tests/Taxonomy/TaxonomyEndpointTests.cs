using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GetABursary.Api.DTOs.Taxonomy;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;

namespace GetABursary.Api.Tests.Taxonomy;

/// <summary>Integration tests for the <c>/api/v1/taxonomy</c> read endpoints against a seeded PostgreSQL database.</summary>
[Collection(TaxonomyPostgresFixtureDefinition.Name)]
public sealed class TaxonomyEndpointTests : IDisposable
{
    private static readonly string[] BigFiveWithQualifications = ["uct", "wits"];

    private readonly TaxonomyPostgresFixture _fixture;
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public TaxonomyEndpointTests(TaxonomyPostgresFixture fixture)
    {
        _fixture = fixture;
        _factory = fixture.CreateApiFactory();
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Fact]
    public async Task GetInstitutions_Default_Returns200WithOnlyInstitutionsThatHaveQualifications()
    {
        using var response = await _client.GetAsync("/api/v1/taxonomy/institutions");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var institutions = await response.Content.ReadFromJsonAsync<InstitutionResponse[]>();
        Assert.NotNull(institutions);
        Assert.Equal(BigFiveWithQualifications.Order(), institutions.Select(i => i.Slug).Order());
    }

    [Fact]
    public async Task GetInstitutions_IncludeAll_Returns200WithEveryInstitution()
    {
        using var response = await _client.GetAsync("/api/v1/taxonomy/institutions?includeAll=true");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var institutions = await response.Content.ReadFromJsonAsync<InstitutionResponse[]>();
        Assert.NotNull(institutions);
        Assert.Equal(26, institutions.Length);
    }

    [Fact]
    public async Task GetInstitutions_Default_IsCacheableForOneHour()
    {
        using var response = await _client.GetAsync("/api/v1/taxonomy/institutions");

        Assert.Equal(TimeSpan.FromSeconds(3600), response.Headers.CacheControl?.MaxAge);
    }

    [Fact]
    public async Task GetFaculties_KnownInstitution_Returns200WithItsFaculties()
    {
        var institutionId = await InstitutionIdAsync("uct");

        using var response = await _client.GetAsync($"/api/v1/taxonomy/institutions/{institutionId}/faculties");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var faculties = await response.Content.ReadFromJsonAsync<FacultyResponse[]>();
        Assert.NotNull(faculties);
        Assert.NotEmpty(faculties);
        Assert.Contains(faculties, f => f.Slug == "commerce");
    }

    [Fact]
    public async Task GetFaculties_UnknownInstitution_Returns404ProblemDetailsWithCode()
    {
        using var response = await _client.GetAsync($"/api/v1/taxonomy/institutions/{Guid.CreateVersion7()}/faculties");

        await AssertProblemAsync(response, HttpStatusCode.NotFound, "Taxonomy.InstitutionNotFound");
    }

    [Fact]
    public async Task GetQualifications_KnownFaculty_Returns200WithFieldCodes()
    {
        var facultyId = await FacultyIdAsync("uct", "commerce");

        using var response = await _client.GetAsync($"/api/v1/taxonomy/faculties/{facultyId}/qualifications");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var qualifications = await response.Content.ReadFromJsonAsync<QualificationResponse[]>();
        Assert.NotNull(qualifications);
        Assert.NotEmpty(qualifications);
        Assert.All(qualifications, q => Assert.NotEmpty(q.FieldCodes));
    }

    [Fact]
    public async Task GetQualifications_UnknownFaculty_Returns404ProblemDetailsWithCode()
    {
        using var response = await _client.GetAsync($"/api/v1/taxonomy/faculties/{Guid.CreateVersion7()}/qualifications");

        await AssertProblemAsync(response, HttpStatusCode.NotFound, "Taxonomy.FacultyNotFound");
    }

    [Fact]
    public async Task GetFields_Returns200WithTwentyFirstOrderFields()
    {
        using var response = await _client.GetAsync("/api/v1/taxonomy/fields");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var fields = await response.Content.ReadFromJsonAsync<FieldOfStudyResponse[]>();
        Assert.NotNull(fields);
        Assert.Equal(20, fields.Length);
        Assert.All(fields, f => Assert.Null(f.ParentCode));
    }

    private async Task<Guid> InstitutionIdAsync(string slug)
    {
        await using var db = _fixture.CreateContext();
        return await db.Institutions.AsNoTracking().Where(i => i.Slug == slug).Select(i => i.Id).SingleAsync();
    }

    private async Task<Guid> FacultyIdAsync(string institutionSlug, string facultySlug)
    {
        await using var db = _fixture.CreateContext();
        return await db.Faculties.AsNoTracking()
            .Where(f => f.Institution.Slug == institutionSlug && f.Slug == facultySlug)
            .Select(f => f.Id)
            .SingleAsync();
    }

    private static async Task AssertProblemAsync(HttpResponseMessage response, HttpStatusCode status, string errorCode)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var body = await response.Content.ReadFromJsonAsync<JsonDocument>();
        Assert.Equal((int)status, body!.RootElement.GetProperty("status").GetInt32());
        var errors = body.RootElement.GetProperty("errors");
        Assert.Contains(errors.EnumerateArray(), e => e.GetProperty("code").GetString() == errorCode);
    }
}
