namespace Lanflix.Domain.Entities;

/// <summary>
/// Server configuration setting stored in database
/// </summary>
public class ServerSetting
{
    public int Id { get; set; }
    public required string Key { get; set; }
    public string Value { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime UpdatedAt { get; set; }
}

