using Lanflix.SharedKernel;

namespace Lanflix.Modules.Administration;

public sealed class BackgroundJobRun : Entity<Guid>
{
    private BackgroundJobRun() { }
    public string Name { get; private set; } = string.Empty;
    public string Status { get; private set; } = "pending";
    public DateTime? StartedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public string? Result { get; private set; }
    public string? Error { get; private set; }

    public static BackgroundJobRun Create(string name) => new() { Id = Guid.NewGuid(), Name = name };
    public void Start() { Status = "running"; StartedAtUtc = DateTime.UtcNow; MarkUpdated(); }
    public void Complete(string result) { Status = "completed"; Result = result; CompletedAtUtc = DateTime.UtcNow; MarkUpdated(); }
    public void Fail(string error) { Status = "failed"; Error = error[..Math.Min(error.Length, 2000)]; CompletedAtUtc = DateTime.UtcNow; MarkUpdated(); }
    public JobDto ToDto() => new(Id, Name, Status, CreatedAtUtc, StartedAtUtc, CompletedAtUtc, Result, Error);
}
