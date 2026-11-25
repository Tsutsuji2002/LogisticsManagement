# Desktop Authorization, RBAC & Branch Scoping

## 1. Permission Gating ([ClientAuthorizationService.cs](file:///c:/Projects/LogisticsManagement/src/Client/Logistics.Client.Infrastructure/Services/ClientAuthorizationService.cs))

- Permissions are populated from `LoginResponse.User.Permissions` and refreshed via `GET /api/v1/auth/me`.
- Constants in [`ClientPermissions.cs`](file:///c:/Projects/LogisticsManagement/src/Client/Logistics.Client.Application/Permissions/ClientPermissions.cs) mirror backend `AppPermissions`.
- `SuperAdmin` role (`ClientPermissions.Roles.SuperAdmin`) evaluates true for all permission checks; all other roles are checked case-insensitively against their assigned permission set.
- Both sidebar menu items (`NavigationCatalog.GetVisibleGroups`) and workspace action buttons (Create, Edit, Issue, Cancel, Approve, Verify, Export CSV, Retry Jobs) are hidden or disabled when the user lacks permission.
- **Defense in Depth**: UI visibility is convenience only; the backend remains authoritative and any HTTP `403 Forbidden` response surfaces a clear authorization warning dialog via `UiAsyncRunner`.

## 2. Branch Context Scoping ([BranchContextService.cs](file:///c:/Projects/LogisticsManagement/src/Client/Logistics.Client.Infrastructure/Services/BranchContextService.cs))

- **Single-Branch User**: Locked to their single assigned branch (`HasSingleBranchOnly = true`); dropdown is disabled and attempting to clear or switch to an unassigned branch returns `false`.
- **Multi-Branch User**: Defaults to `DefaultBranchId` and can switch across their `AuthorizedBranches` list or select all assigned branches (`null`).
- **SuperAdmin**: Defaults to `DefaultBranchId` (or `null` company-wide) and can toggle between `"All accessible branches"` (`IsCompanyWideScope = true`) and any authorized branch.
- Because backend endpoints read `?branchId=` query/body parameters and JWT claims (`branch_id`, `branch_access`), workspaces forward `ActiveBranchId` explicitly on list/create calls in addition to `X-Branch-Id` HTTP headers.
