using Lanflix.Application.Common.DTOs;
using Lanflix.Application.Features.Profiles.Commands.CreateProfile;
using Lanflix.Application.Features.Profiles.Commands.UpdateProfile;
using Lanflix.Application.Features.Profiles.Queries.GetProfiles;
using Lanflix.Application.Features.Profiles.Queries.GetWatchHistory;
using Lanflix.Application.Features.Profiles.Commands.ToggleWatchlist;
using Lanflix.Application.Features.Profiles.Queries.GetWatchlist;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace Lanflix.WebApi.Controllers;

[ApiController]
[Route("api/profiles")]
public class ProfilesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ProfilesController> _logger;

    public ProfilesController(IMediator mediator, ILogger<ProfilesController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get all profiles
    /// </summary>
    [HttpGet]
    [OutputCache(PolicyName = "profiles")]
    [ProducesResponseType(typeof(List<ProfileDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ProfileDto>>> GetProfiles(
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting all profiles");

        var query = new GetProfilesQuery();
        var result = await _mediator.Send(query, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Create a new profile
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ProfileDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProfileDto>> CreateProfile(
        [FromBody] CreateProfileCommand command,
        [FromServices] IOutputCacheStore cacheStore,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating new profile: {Name}", command.Name);

        var result = await _mediator.Send(command, cancellationToken);

        // Invalidate profiles cache after creation
        await cacheStore.EvictByTagAsync("profiles", cancellationToken);
        _logger.LogDebug("Invalidated profiles cache after profile creation");

        return CreatedAtAction(
            nameof(GetProfiles),
            new { id = result.Id },
            result);
    }

    /// <summary>
    /// Update an existing profile
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProfileDto>> UpdateProfile(
        int id,
        [FromBody] UpdateProfileCommand command,
        [FromServices] IOutputCacheStore cacheStore,
        CancellationToken cancellationToken)
    {
        // Override Id from route
        command.Id = id;

        _logger.LogInformation("Updating profile {ProfileId}: {Name}", id, command.Name);

        var result = await _mediator.Send(command, cancellationToken);

        // Invalidate profiles cache after update
        await cacheStore.EvictByTagAsync("profiles", cancellationToken);
        _logger.LogDebug("Invalidated profiles cache after profile update");

        return Ok(result);
    }

    /// <summary>
    /// Get watch history for a profile
    /// </summary>
    [HttpGet("{id:int}/history")]
    [OutputCache(Duration = 300)] // 5 minutes
    [ProducesResponseType(typeof(List<WatchHistoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<WatchHistoryDto>>> GetWatchHistory(
        int id,
        [FromQuery] int? limit,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting watch history for profile {ProfileId}", id);

        var query = new GetWatchHistoryQuery
        {
            ProfileId = id,
            Limit = limit
        };

        var result = await _mediator.Send(query, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Get watchlist for a profile
    /// </summary>
    [HttpGet("{id:int}/watchlist")]
    [OutputCache(Duration = 300, Tags = new[] { "watchlist" })] // Added tag for invalidation
    [ProducesResponseType(typeof(List<ContentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<ContentDto>>> GetWatchlist(
        int id,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting watchlist for profile {ProfileId}", id);

        var query = new GetWatchlistQuery { ProfileId = id };
        var result = await _mediator.Send(query, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Add or remove content from profile's watchlist
    /// </summary>
    [HttpPost("{id:int}/watchlist/{contentId:int}")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<bool>> ToggleWatchlist(
        int id,
        int contentId,
        [FromServices] IOutputCacheStore cacheStore,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Toggling watchlist item: Profile={ProfileId}, Content={ContentId}", id, contentId);

        var command = new ToggleWatchlistCommand
        {
            ProfileId = id,
            ContentId = contentId
        };
        
        var result = await _mediator.Send(command, cancellationToken);
        
        // Invalidate watchlist cache
        // We can't easily invalidate just this profile's watchlist without dynamic tags,
        // but since we tagged GetWatchlist with "watchlist", we can invalidate that.
        // Ideally we'd use "watchlist-{id}" but attributes require constant strings.
        // For now, evicting "watchlist" affects all profiles, which is acceptable but not optimal.
        await cacheStore.EvictByTagAsync("watchlist", cancellationToken);

        return Ok(new { inWatchlist = result });
    }

    /// <summary>
    /// Delete a profile
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProfile(
        int id,
        [FromServices] Lanflix.Application.Common.Interfaces.IApplicationDbContext dbContext,
        [FromServices] IOutputCacheStore cacheStore,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting profile {ProfileId}", id);

        var profile = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(
            dbContext.Profiles, p => p.Id == id, cancellationToken);

        if (profile == null)
        {
            return NotFound(new { message = "Profile not found" });
        }

        dbContext.Profiles.Remove(profile);
        await dbContext.SaveChangesAsync(cancellationToken);

        await cacheStore.EvictByTagAsync("profiles", cancellationToken);
        _logger.LogInformation("Deleted profile {ProfileId} successfully", id);

        return Ok(new { message = "Profile deleted successfully" });
    }
}
