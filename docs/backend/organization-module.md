# Organization & Employee Management Module

## 1. Module Overview

The Organization & Employee Management module provides complete CRUD operations for the enterprise's organizational structure and workforce management. It covers Companies, Branches, Departments, Warehouses, and Employees, with full branch-scoped authorization, audit logging, and employee-to-user-account lifecycle integration.

---

## 2. Architecture

### Feature Organization

```text
Logistics.Application/
└── Modules/
    └── Organization/
        ├── DTOs/
        │   ├── CompanyDto.cs
        │   ├── BranchDto.cs
        │   ├── DepartmentDto.cs
        │   ├── EmployeeDto.cs
        │   └── WarehouseDto.cs
        ├── Services/
        │   └── IOrganizationServices.cs
        └── Validators/
            └── OrganizationValidators.cs

Logistics.Infrastructure/
└── Services/
    ├── OrganizationServices.cs      # ~1950 lines: CompanyService, BranchService,
    │                                 # DepartmentService, EmployeeService, WarehouseService
    └── AuditService.cs              # Audit trail recording

Logistics.API/
└── Controllers/
    ├── Organization/
    │   ├── OrganizationCompanyController.cs
    │   ├── OrganizationBranchesController.cs
    │   ├── OrganizationDepartmentsController.cs
    │   └── OrganizationWarehousesController.cs
    └── EmployeesController.cs
```

### Service Interfaces

| Interface | Responsibility |
|---|---|
| `ICompanyService` | Primary company retrieval, update, organization overview |
| `IBranchService` | Branch CRUD, status management, sub-resource queries |
| `IDepartmentService` | Department CRUD within branches |
| `IEmployeeService` | Employee CRUD, status transitions, account provisioning, branch access |
| `IWarehouseService` | Warehouse listing and detail retrieval |
| `IAuditService` | Audit trail recording for all CUD operations |

---

## 3. API Endpoints

### Company (Single Enterprise)

| Method | Route | Permission | Description |
|---|---|---|---|
| `GET` | `/api/v1/organization/company` | `organization.company.read` | Get the primary company |
| `PUT` | `/api/v1/organization/company` | `organization.company.update` | Update company details |
| `GET` | `/api/v1/organization/overview` | `organization.overview.read` | Organization metrics overview |

### Branches

| Method | Route | Permission | Description |
|---|---|---|---|
| `GET` | `/api/v1/organization/branches` | `organization.branches.read` | List branches (paged, branch-scoped) |
| `GET` | `/api/v1/organization/branches/{id}` | `organization.branches.read` | Get branch detail (by ID or PublicId) |
| `POST` | `/api/v1/organization/branches` | `organization.branches.create` | Create a new branch |
| `PUT` | `/api/v1/organization/branches/{id}` | `organization.branches.update` | Update branch details |
| `PATCH` | `/api/v1/organization/branches/{id}/status` | `organization.branches.manage` | Activate/deactivate branch |
| `GET` | `/api/v1/organization/branches/{id}/employees` | `employees.read` | List employees in branch |
| `GET` | `/api/v1/organization/branches/{id}/departments` | `organization.departments.read` | List departments in branch |
| `GET` | `/api/v1/organization/branches/{id}/warehouses` | `organization.warehouses.read` | List warehouses in branch |
| `GET` | `/api/v1/organization/branches/{id}/summary` | `organization.branches.read` | Branch summary with metrics |

### Departments

| Method | Route | Permission | Description |
|---|---|---|---|
| `GET` | `/api/v1/organization/departments` | `organization.departments.read` | List departments (paged, branch-scoped) |
| `GET` | `/api/v1/organization/departments/{id}` | `organization.departments.read` | Get department detail |
| `POST` | `/api/v1/organization/departments` | `organization.departments.create` | Create department in a branch |
| `PUT` | `/api/v1/organization/departments/{id}` | `organization.departments.update` | Update department |
| `PATCH` | `/api/v1/organization/departments/{id}/status` | `organization.departments.manage` | Activate/deactivate department |
| `GET` | `/api/v1/organization/departments/{id}/employees` | `employees.read` | List employees in department |

### Warehouses (Read-Only)

| Method | Route | Permission | Description |
|---|---|---|---|
| `GET` | `/api/v1/organization/warehouses` | `organization.warehouses.read` | List warehouses (paged, branch-scoped) |
| `GET` | `/api/v1/organization/warehouses/{id}` | `organization.warehouses.read` | Get warehouse detail with zones |

### Employees

| Method | Route | Permission | Description |
|---|---|---|---|
| `GET` | `/api/v1/employees` | `employees.read` | List employees (paged, branch-scoped) |
| `GET` | `/api/v1/employees/{code}` | `employees.read` | Get employee by code |
| `POST` | `/api/v1/employees` | `employees.create` | Create employee |
| `PUT` | `/api/v1/employees/{code}` | `employees.update` | Update employee |
| `PATCH` | `/api/v1/employees/{code}/status` | `employees.status.manage` | Change employee status |
| `GET` | `/api/v1/employees/{code}/profile` | `employees.read` | Get employee profile |
| `GET` | `/api/v1/employees/{code}/account` | `employees.account.read` | Get employee's user account |
| `POST` | `/api/v1/employees/{code}/account` | `employees.account.create` | Provision user account for employee |
| `PATCH` | `/api/v1/employees/{code}/account/status` | `employees.account.manage` | Activate/deactivate user account |
| `GET` | `/api/v1/employees/{code}/branches` | `identity.users.branch-access.read` | Get employee's branch access |

### Admin User Branch Access

| Method | Route | Permission | Description |
|---|---|---|---|
| `GET` | `/api/v1/admin/users/{publicId}/branches` | `identity.users.branch-access.read` | Get user's branch assignments |
| `POST` | `/api/v1/admin/users/{publicId}/branches` | `identity.users.branch-access.manage` | Grant branch access |
| `DELETE` | `/api/v1/admin/users/{publicId}/branches/{branchId}` | `identity.users.branch-access.manage` | Revoke branch access |

---

## 4. Authorization Model

### Branch-Scoped Access Control

All queries for branches, departments, employees, and warehouses are filtered by the requesting user's authorized branches:

- **SuperAdmin / Admin**: Bypass all branch restrictions — see all data globally.
- **Other roles**: See only data from branches in their `AccessibleBranchIds` (from `UserBranchAccess` table) plus their `DefaultBranchId`.

This is enforced at the service layer, not the controller layer.

### Permission Matrix (New Permissions)

| Permission Code | SuperAdmin | Admin | BranchManager | OperationsManager | WarehouseStaff | CustomerService | FinanceStaff | ReadOnly |
|---|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|
| `organization.company.read` | ✅ | ✅ | ✅ | ✅ | - | - | - | ✅ |
| `organization.company.update` | ✅ | ✅ | - | - | - | - | - | - |
| `organization.branches.create` | ✅ | ✅ | - | - | - | - | - | - |
| `organization.branches.update` | ✅ | ✅ | ✅ | - | - | - | - | - |
| `organization.departments.create` | ✅ | ✅ | ✅ | - | - | - | - | - |
| `organization.departments.update` | ✅ | ✅ | ✅ | - | - | - | - | - |
| `employees.read` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | - | ✅ |
| `employees.create` | ✅ | ✅ | ✅ | - | - | - | - | - |
| `employees.update` | ✅ | ✅ | ✅ | - | - | - | - | - |
| `employees.status.manage` | ✅ | ✅ | ✅ | - | - | - | - | - |
| `employees.account.create` | ✅ | ✅ | - | - | - | - | - | - |
| `employees.account.manage` | ✅ | ✅ | - | - | - | - | - | - |

---

## 5. Business Rules

### Employee Status Transitions

Valid statuses: `Active`, `Inactive`, `OnLeave`, `Resigned`, `Terminated`.

**Cascading effects on status change to Inactive / Resigned / Terminated:**

1. If the employee has a linked `UserAccount`, it is deactivated (`IsActive = false`).
2. All active `RefreshToken` records are revoked (`IsRevoked = true`).
3. All active `UserSession` records are terminated (`IsTerminated = true`).
4. An audit trail entry is recorded.

### Employee Account Provisioning

- An employee starts without a user account (HR record only).
- Account provisioning creates a `UserAccount` linked to the employee.
- Roles are assigned during provisioning.
- Branch access is automatically granted for the employee's branch.
- Duplicate account creation is prevented with a 409 Conflict response.

### Branch Deactivation Guard

- A branch cannot be deactivated if it has active employees (`Status == "Active"`).
- Returns 409 Conflict with a descriptive message.

### Department Code Uniqueness

- Department codes must be unique within a branch.
- Globally, the same code can exist in different branches.

---

## 6. Audit Trail

All create, update, delete, and status-change operations are recorded in the `AuditLogs` table via `IAuditService`:

```csharp
public interface IAuditService
{
    Task LogAsync(string tableName, string action, long? recordId,
        string? oldValues, string? newValues, CancellationToken cancellationToken = default);
}
```

The implementation automatically captures:
- `UserId` from the current HTTP context (JWT claims).
- `IpAddress` from the HTTP connection.
- Timestamp in UTC.

---

## 7. Validation

All request DTOs are validated using FluentValidation:

| Validator | Key Rules |
|---|---|
| `UpdateCompanyRequestValidator` | Email format, phone length, required legal name |
| `CreateBranchRequestValidator` | Valid branch type whitelist, required code/name |
| `UpdateBranchRequestValidator` | Same as create |
| `UpdateBranchStatusRequestValidator` | Reason required when deactivating |
| `CreateDepartmentRequestValidator` | Valid branch ID, required code/name |
| `CreateEmployeeRequestValidator` | Valid branch, email, phone, gender whitelist, hire date not future |
| `UpdateEmployeeStatusRequestValidator` | Valid status whitelist, reason required for Resigned/Terminated |
| `CreateEmployeeAccountRequestValidator` | Strong password policy, at least one role |

**Branch type whitelist**: `Headquarters`, `RegionalOffice`, `TransitHub`, `DeliveryPost`, `SortingHub`, `Office`, `Hybrid`.

**Gender whitelist**: `Male`, `Female`, `Other`.

---

## 8. Test Coverage

### Unit Tests (55 total — all passing)

Includes validators for:
- `UpdateCompanyRequest`
- `CreateBranchRequest` (invalid company IDs)
- `CreateDepartmentRequest` (invalid branch ID)
- `CreateEmployeeRequest` (valid data, invalid gender)
- `CreateEmployeeAccountRequest` (no roles, weak password)

### Integration Tests (41 total — all passing)

| Test | Verifies |
|---|---|
| `GetCompany_AsSuperAdmin_ShouldReturnCompany` | Company GET endpoint returns seeded company |
| `UpdateCompany_WithValidData_ShouldUpdateAndRecordAudit` | Company update + audit log creation |
| `CreateBranch_WithDuplicateCode_ShouldReturn409Conflict` | Duplicate branch code prevention |
| `DeactivateBranch_WithActiveEmployees_ShouldReturn409Conflict` | Branch deactivation guard |
| `CreateDepartment_AndPreventDuplicateCodeInBranch` | Department CRUD + uniqueness |
| `CreateEmployee_ShouldCreateRecordWithoutUserAccount` | Employee creation without account |
| `CreateEmployeeAccount_ShouldProvisionUserAndPreventDuplicate` | Account provisioning + duplicate prevention |
| `EmployeeStatusTransitionToTerminated_ShouldDeactivateLinkedAccountAndRevokeSessions` | Full cascade on termination |
| `BranchAuthorization_BranchScopedUser_CannotAccessUnauthorizedBranch` | Branch-scoped access control |
| `OrganizationOverview_AsSuperAdmin_ShouldReturnAccurateMetrics` | Overview endpoint metrics |
| `AdminUserBranches_GrantAndRevoke_ShouldUpdateUserAssignments` | Branch access grant/revoke lifecycle |

---

## 9. Dependencies Added

- `IAuditService` → `AuditService` (registered as Scoped)
- `IDepartmentService` → `DepartmentService` (registered as Scoped)
- `IEmployeeService` → `EmployeeService` (registered as Scoped)
- `IWarehouseService` → `WarehouseService` (registered as Scoped)

All registered in `Logistics.Server.Infrastructure/DependencyInjection.cs`.
