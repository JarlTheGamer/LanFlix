using Serilog.Core;
using Serilog.Events;

namespace Lanflix.Infrastructure.Logging;

/// <summary>
/// Serilog enricher that redacts sensitive data from log messages
/// </summary>
public class SensitiveDataRedactionEnricher : ILogEventEnricher
{
    private static readonly string[] SensitivePropertyNames = new[]
    {
        "password",
        "pwd",
        "secret",
        "apikey",
        "api_key",
        "token",
        "authorization",
        "auth",
        "jwt",
        "bearer",
        "connectionstring",
        "connection_string"
    };

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        // Redact sensitive properties
        var propertiesToRedact = logEvent.Properties
            .Where(p => SensitivePropertyNames.Any(s => 
                p.Key.Contains(s, StringComparison.OrdinalIgnoreCase)))
            .Select(p => p.Key)
            .ToList();

        foreach (var key in propertiesToRedact)
        {
            if (logEvent.Properties.TryGetValue(key, out var value))
            {
                logEvent.RemovePropertyIfPresent(key);
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
                    key, 
                    new ScalarValue("***REDACTED***")));
            }
        }

        // Redact sensitive data in message template
        if (logEvent.MessageTemplate.Text.Contains("password", StringComparison.OrdinalIgnoreCase) ||
            logEvent.MessageTemplate.Text.Contains("secret", StringComparison.OrdinalIgnoreCase) ||
            logEvent.MessageTemplate.Text.Contains("token", StringComparison.OrdinalIgnoreCase))
        {
            // Add a warning property
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
                "SensitiveDataWarning",
                new ScalarValue("Message may contain sensitive data")));
        }
    }
}

/// <summary>
/// Extension methods for registering the sensitive data redaction enricher
/// </summary>
public static class SensitiveDataRedactionEnricherExtensions
{
    public static Serilog.LoggerConfiguration WithSensitiveDataRedaction(
        this Serilog.Configuration.LoggerEnrichmentConfiguration enrichmentConfiguration)
    {
        if (enrichmentConfiguration == null)
            throw new ArgumentNullException(nameof(enrichmentConfiguration));

        return enrichmentConfiguration.With<SensitiveDataRedactionEnricher>();
    }
}
