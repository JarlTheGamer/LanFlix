using Lanflix.Application.Common.DTOs;
using Lanflix.Application.Common.Exceptions;
using Lanflix.Application.Common.Interfaces;
using Lanflix.Domain.Entities;
using Lanflix.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lanflix.Application.Features.Streaming.Commands.StartStream;

public class StartStreamCommandHandler : IRequestHandler<StartStreamCommand, StreamSessionDto>
{
    private readonly IApplicationDbContext _context;

    public StartStreamCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<StreamSessionDto> Handle(
        StartStreamCommand request,
        CancellationToken cancellationToken)
    {
        // Verify content exists
        var content = await _context.Contents
            .FirstOrDefaultAsync(c => c.Id == request.ContentId, cancellationToken);

        if (content == null)
        {
            throw new NotFoundException(nameof(Content), request.ContentId);
        }

        // Verify profile exists
        var profile = await _context.Profiles
            .FirstOrDefaultAsync(p => p.Id == request.ProfileId, cancellationToken);

        if (profile == null)
        {
            throw new NotFoundException(nameof(Profile), request.ProfileId);
        }

        // TODO: Implement streaming strategy selection
        // For now, default to DirectPlay
        var streamingMode = StreamingMode.DirectPlay;

        // Create stream session
        var session = new StreamSession
        {
            SessionId = Guid.NewGuid().ToString(),
            ProfileId = request.ProfileId,
            ContentId = request.ContentId,
            Mode = streamingMode,
            StartedAt = DateTime.UtcNow,
            IsActive = true,
            LastActivityAt = DateTime.UtcNow
        };

        _context.StreamSessions.Add(session);
        await _context.SaveChangesAsync(cancellationToken);

        return new StreamSessionDto
        {
            Id = session.SessionId,
            ProfileId = session.ProfileId,
            ContentId = session.ContentId,
            Mode = session.Mode,
            TranscodingProcessId = session.TranscodingProcessId,
            StartedAt = session.StartedAt,
            IsActive = session.IsActive,
            StreamUrl = $"/api/stream/{session.SessionId}/stream"
        };
    }
}
