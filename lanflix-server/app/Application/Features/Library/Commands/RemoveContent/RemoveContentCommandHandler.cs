using Lanflix.Application.Common.Exceptions;
using Lanflix.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lanflix.Application.Features.Library.Commands.RemoveContent;

public class RemoveContentCommandHandler : IRequestHandler<RemoveContentCommand, Unit>
{
    private readonly IApplicationDbContext _context;
    private readonly ICacheService _cacheService;

    public RemoveContentCommandHandler(
        IApplicationDbContext context,
        ICacheService cacheService)
    {
        _context = context;
        _cacheService = cacheService;
    }

    public async Task<Unit> Handle(
        RemoveContentCommand request,
        CancellationToken cancellationToken)
    {
        var content = await _context.Contents
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (content == null)
        {
            throw new NotFoundException("Content", request.Id);
        }

        // Soft delete
        content.IsDeleted = true;
        content.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        // Invalidate cache
        await _cacheService.RemoveByTagAsync("library", cancellationToken);
        await _cacheService.RemoveAsync($"content:{request.Id}", cancellationToken);

        return Unit.Value;
    }
}
