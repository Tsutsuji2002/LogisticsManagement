using Logistics.Client.Application.Contracts.Auth;
using Logistics.Client.Application.Interfaces;
using Logistics.Client.Application.Permissions;

namespace Logistics.Client.Infrastructure.Services;

/// <summary>
/// Maintains the active authorized branch selection for the desktop workspace session.
/// </summary>
public sealed class BranchContextService : IBranchContextService
{
    private List<AuthorizedBranchDto> _branches = [];
    private long? _activeBranchId;
    private bool _isSuperAdmin;

    public IReadOnlyList<AuthorizedBranchDto> AccessibleBranches => _branches;
    public long? ActiveBranchId => _activeBranchId;
    public AuthorizedBranchDto? ActiveBranch => _activeBranchId.HasValue
        ? _branches.FirstOrDefault(b => b.BranchId == _activeBranchId.Value)
        : null;

    public bool IsCompanyWideScope => _isSuperAdmin && !_activeBranchId.HasValue;
    public bool HasSingleBranchOnly => !_isSuperAdmin && _branches.Count <= 1;

    public event EventHandler? ActiveBranchChanged;

    public void InitializeFromProfile(UserProfileDto profile)
    {
        _branches = profile.AuthorizedBranches?.ToList() ?? [];
        _isSuperAdmin = profile.Roles.Any(r =>
            string.Equals(r, ClientPermissions.Roles.SuperAdmin, StringComparison.OrdinalIgnoreCase));

        if (_isSuperAdmin)
        {
            // Keep previously selected branch if still valid; otherwise default branch or All (null)
            _activeBranchId = profile.DefaultBranchId;
        }
        else if (profile.DefaultBranchId.HasValue && _branches.Any(b => b.BranchId == profile.DefaultBranchId.Value))
        {
            _activeBranchId = profile.DefaultBranchId.Value;
        }
        else
        {
            _activeBranchId = _branches.FirstOrDefault(b => b.IsDefault)?.BranchId ?? _branches.FirstOrDefault()?.BranchId;
        }

        ActiveBranchChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Clear()
    {
        _branches = [];
        _activeBranchId = null;
        _isSuperAdmin = false;
        ActiveBranchChanged?.Invoke(this, EventArgs.Empty);
    }

    public bool TrySetActiveBranch(long? branchId)
    {
        if (branchId == null)
        {
            // Only SuperAdmin or multi-branch users can view "all accessible branches" cleanly
            if (!_isSuperAdmin && _branches.Count <= 1) return false;
            _activeBranchId = null;
            ActiveBranchChanged?.Invoke(this, EventArgs.Empty);
            return true;
        }

        if (!_isSuperAdmin && !_branches.Any(b => b.BranchId == branchId.Value))
            return false;

        _activeBranchId = branchId.Value;
        ActiveBranchChanged?.Invoke(this, EventArgs.Empty);
        return true;
    }
}
