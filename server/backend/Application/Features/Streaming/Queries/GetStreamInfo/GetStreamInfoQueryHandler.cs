using Lanflix.Application.Common.DTOs;
using Lanflix.Application.Common.Exceptions;
using Lanflix.Application.Common.Interfaces;
using Lanflix.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lanflix.Application.Features.Streaming.Queries.GetStreamInfo;

public class GetStreamInfoQueryHandler : IRequestHandler<GetStreamInfoQuery, StreamSessionDto>
{
    private readonly IApplicationDbContext _context;

    public GetStreamInfoQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<StreamSessionDto> Handle(
        GetStreamInfoQuery request,
        CancellationToken cancellationToken)
    {
        var session = await _context.StreamSessions
            .FirstOrDefaultAsync(s => s.SessionId == request.SessionId, cancellationToken);

        if (session == null)
        {
            throw new NotFoundException(nameof(StreamSession), request.SessionId);
        }

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
