using Lanflix.Application.Common.Exceptions;
using Lanflix.Application.Common.Interfaces;
using Lanflix.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lanflix.Application.Features.Streaming.Commands.StopStream;

public class StopStreamCommandHandler : IRequestHandler<StopStreamCommand, Unit>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<StopStreamCommandHandler> _logger;

    public StopStreamCommandHandler(
        IApplicationDbContext context,
        ILogger<StopStreamCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Unit> Handle(
        StopStreamCommand request,
        CancellationToken cancellationToken)
    {
        var session = await _context.StreamSessions
            .FirstOrDefaultAsync(s => s.SessionId == request.SessionId, cancellationToken);

        if (session == null)
        {
            throw new NotFoundException(nameof(StreamSession), request.SessionId);
        }

        // Mark session as inactive
        session.IsActive = false;
        session.EndedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Stream session {SessionId} stopped. Duration: {Duration}",
            session.SessionId,
            session.EndedAt - session.StartedAt);

        // TODO: Cleanup transcoding process if exists
        // This will be implemented when FFmpeg integration is added

        return Unit.Value;
    }
}
