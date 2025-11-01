using System.Diagnostics.Metrics;
using Lanflix.Application.Common.Interfaces;

namespace Lanflix.Infrastructure.Telemetry;

/// <summary>
/// Provides custom metrics for streaming operations
/// </summary>
public class StreamingMetrics
{
    private readonly Meter _meter;
    private readonly Counter<long> _streamStartCounter;
    private readonly Histogram<double> _streamDuration;
    private readonly ObservableGauge<int> _activeStreams;
    private readonly ObservableGauge<int> _transcodingQueueDepth;
    private readonly ITranscodingSessionManager _sessionManager;
    
    public StreamingMetrics(
        IMeterFactory meterFactory,
        ITranscodingSessionManager sessionManager)
    {
        _sessionManager = sessionManager;
        _meter = meterFactory.Create("Lanflix.Streaming");
        
        _streamStartCounter = _meter.CreateCounter<long>(
            name: "streams.started",
            unit: "{stream}",
            description: "Number of streams started");
        
        _streamDuration = _meter.CreateHistogram<double>(
            name: "stream.duration",
            unit: "s",
            description: "Stream duration in seconds");
        
        _activeStreams = _meter.CreateObservableGauge<int>(
            name: "streams.active",
            observeValue: GetActiveStreamCount,
            unit: "{stream}",
            description: "Number of currently active streams");
        
        _transcodingQueueDepth = _meter.CreateObservableGauge<int>(
            name: "transcoding.queue_depth",
            observeValue: GetTranscodingQueueDepth,
            unit: "{session}",
            description: "Number of sessions waiting for transcoding");
    }
    
    /// <summary>
    /// Records a stream start event
    /// </summary>
    public void RecordStreamStart(string streamingMode, string contentType)
    {
        _streamStartCounter.Add(1, 
            new KeyValuePair<string, object?>("streaming_mode", streamingMode),
            new KeyValuePair<string, object?>("content_type", contentType));
    }
    
    /// <summary>
    /// Records stream duration when a stream ends
    /// </summary>
    public void RecordStreamDuration(double durationSeconds, string streamingMode, bool completed)
    {
        _streamDuration.Record(durationSeconds,
            new KeyValuePair<string, object?>("streaming_mode", streamingMode),
            new KeyValuePair<string, object?>("completed", completed));
    }
    
    private int GetActiveStreamCount()
    {
        return _sessionManager.GetActiveSessionCount();
    }
    
    private int GetTranscodingQueueDepth()
    {
        // This would need to be implemented in the session manager
        // For now, return 0 as a placeholder
        return 0;
    }
}
