using Lanflix.SharedKernel;

namespace Lanflix.Modules.Social;

public sealed class SocialRelationship : Entity<Guid>
{
    private SocialRelationship() { }
    public Guid SourceAccountId { get; private set; }
    public Guid TargetAccountId { get; private set; }
    public RelationshipKind Kind { get; private set; }
    public RelationshipStatus Status { get; private set; }

    public static SocialRelationship Create(Guid source, Guid target, RelationshipKind kind)
    {
        if (source == target) throw new ArgumentException("An account cannot relate to itself.");
        return new SocialRelationship
        {
            Id = Guid.NewGuid(), SourceAccountId = source, TargetAccountId = target, Kind = kind,
            Status = kind == RelationshipKind.Follow ? RelationshipStatus.Accepted : RelationshipStatus.Pending
        };
    }

    public void Accept(Guid accountId)
    {
        if (Kind != RelationshipKind.Friend || TargetAccountId != accountId)
            throw new InvalidOperationException("Only the recipient can accept this friend request.");
        Status = RelationshipStatus.Accepted;
        MarkUpdated();
    }
}
