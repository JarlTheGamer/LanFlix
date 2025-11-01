using Lanflix.Application.Common.DTOs;
using Lanflix.Application.Features.Library.Commands.RemoveContent;
using Lanflix.Application.Features.Library.Commands.ScanLibrary;
using Lanflix.Application.Features.Library.Queries.GetContentDetails;
using Lanflix.Application.Features.Library.Queries.GetLibraryItems;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace Lanflix.WebApi.Controllers;

[ApiController]
[Route("api/library")]
public class LibraryController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<LibraryController> _logger;

    public LibraryController(IMediator mediator, ILogger<LibraryController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get library items with pagination, filtering, and search
    /// </summary>
    [HttpGet("items")]
    [OutputCache(PolicyName = "library")]
    [ProducesResponseType(typeof(PaginatedList<ContentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedList<ContentDto>>> GetLibraryItems(
        [FromQuery] GetLibraryItemsQuery query,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Getting library items: Type={Type}, Page={Page}, PageSize={PageSize}, Search={Search}",
            query.Type, query.PageNumber, query.PageSize, query.SearchTerm);

        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get detailed information about a specific content item
    /// </summary>
    [HttpGet("items/{id:int}")]
    [OutputCache(PolicyName = "content-details")]
    [ProducesResponseType(typeof(ContentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ContentDto>> GetContentDetails(
        int id,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting content details for ID: {ContentId}", id);

        var query = new GetContentDetailsQuery { Id = id };
        var result = await _mediator.Send(query, cancellationToken);

        if (result == null)
        {
            return NotFound(new { message = $"Content with ID {id} not found" });
        }

        return Ok(result);
    }

    /// <summary>
    /// Trigger a library scan to discover new or updated content
    /// </summary>
    [HttpPost("scan")]
    [ProducesResponseType(typeof(ScanLibraryResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ScanLibraryResult>> ScanLibrary(
        [FromBody] ScanLibraryCommand command,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Starting library scan: Path={Path}, FullScan={FullScan}",
            command.Path ?? "all", command.FullScan);

        var result = await _mediator.Send(command, cancellationToken);

        _logger.LogInformation(
            "Library scan completed: Scanned={Scanned}, Added={Added}, Updated={Updated}, Removed={Removed}",
            result.FilesScanned, result.NewContentAdded, result.ContentUpdated, result.ContentRemoved);

        return Ok(result);
    }

    /// <summary>
    /// Remove a content item from the library
    /// </summary>
    [HttpDelete("items/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveContent(
        int id,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Removing content with ID: {ContentId}", id);

        var command = new RemoveContentCommand { Id = id };
        await _mediator.Send(command, cancellationToken);

        return NoContent();
    }
}
