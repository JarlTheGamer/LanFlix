using Lanflix.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Lanflix.Application.Features.Library.Commands.ScanLibrary;

public class ScanLibraryCommandHandler : IRequestHandler<ScanLibraryCommand, ScanLibraryResult>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<ScanLibraryCommandHandler> _logger;

    public ScanLibraryCommandHandler(
        IApplicationDbContext context,
        ILogger<ScanLibraryCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ScanLibraryResult> Handle(
        ScanLibraryCommand request,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new ScanLibraryResult();

        _logger.LogInformation("Starting library scan. FullScan: {FullScan}, Path: {Path}",
            request.FullScan, request.Path ?? "All");

        try
        {
            // TODO: Implement actual scanning logic
            // This is a placeholder that will be implemented with FFmpeg integration
            // For now, just return empty result
            
            _logger.LogInformation("Library scan completed. Files: {FilesScanned}, New: {NewContent}, Updated: {Updated}",
                result.FilesScanned, result.NewContentAdded, result.ContentUpdated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during library scan");
            result.Errors.Add(ex.Message);
        }

        stopwatch.Stop();
        result.Duration = stopwatch.Elapsed;

        return result;
    }
}
