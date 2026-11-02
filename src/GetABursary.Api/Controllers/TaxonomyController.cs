using GetABursary.Api.DTOs.Taxonomy;
using GetABursary.Api.Extensions;
using GetABursary.Api.Services.Taxonomy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GetABursary.Api.Controllers;

/// <summary>Reference data for the University → Faculty → Qualification pickers and the field-of-study filter (FR-01, FR-30–FR-32). Anonymous and cacheable.</summary>
[ApiController]
[Route("api/v1/taxonomy")]
public class TaxonomyController(ITaxonomyService taxonomy) : ControllerBase
{
    /// <summary>Institutions sorted by name. By default only those with at least one qualification; <c>includeAll=true</c> returns every institution.</summary>
    [HttpGet("institutions")]
    [ResponseCache(Duration = 3600)]
    [ProducesResponseType<InstitutionResponse[]>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInstitutions([FromQuery] bool includeAll = false, CancellationToken ct = default)
    {
        var result = await taxonomy.GetInstitutionsAsync(includeAll, ct);

        return result.ToActionResult(this);
    }

    /// <summary>Faculties of an institution.</summary>
    [HttpGet("institutions/{id:guid}/faculties")]
    [ResponseCache(Duration = 3600)]
    [ProducesResponseType<FacultyResponse[]>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFaculties(Guid id, CancellationToken ct = default)
    {
        var result = await taxonomy.GetFacultiesAsync(id, ct);

        return result.ToActionResult(this);
    }

    /// <summary>Qualifications offered by a faculty.</summary>
    [HttpGet("faculties/{id:guid}/qualifications")]
    [ResponseCache(Duration = 3600)]
    [ProducesResponseType<QualificationResponse[]>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetQualifications(Guid id, CancellationToken ct = default)
    {
        var result = await taxonomy.GetQualificationsAsync(id, ct);

        return result.ToActionResult(this);
    }

    /// <summary>Faculties of an institution with their qualifications, in one request (the home page degree picker).</summary>
    [HttpGet("institutions/{id:guid}/qualifications")]
    [ResponseCache(Duration = 3600)]
    [ProducesResponseType<FacultyQualificationsResponse[]>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetQualificationsByInstitution(Guid id, CancellationToken ct = default)
    {
        var result = await taxonomy.GetQualificationsByInstitutionAsync(id, ct);

        return result.ToActionResult(this);
    }

    /// <summary>Fields of study as a tree: first-order (CESM) fields with their second-order children.</summary>
    [HttpGet("fields")]
    [ResponseCache(Duration = 3600)]
    [ProducesResponseType<FieldOfStudyResponse[]>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFields(CancellationToken ct = default)
    {
        var result = await taxonomy.GetFieldsOfStudyAsync(ct);

        return result.ToActionResult(this);
    }
}
