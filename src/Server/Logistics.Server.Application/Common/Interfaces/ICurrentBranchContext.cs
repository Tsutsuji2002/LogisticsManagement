namespace Logistics.Server.Application.Common.Interfaces;

/// <summary>
/// Scopes operations to the active operating branch context for multi-branch internal logistics operations.
/// </summary>
public interface ICurrentBranchContext
{
    long? ActiveBranchId { get; }
    bool HasActiveBranch { get; }
    bool IsAuthorizedForActiveBranch();
}
