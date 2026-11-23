# Authentication & Authorization Architecture Specification

## 1. Executive Summary

This document specifies the complete, production-ready Authentication and Authorization system for the Logistics Management System. The architecture is built as a Clean Architecture Modular Monolith in .NET 9.0 using ASP.NET Core Web API, Entity Framework Core 9, and Microsoft SQL Server. It provides secure credential verification, JWT access token issuance, transactional refresh token rotation with automated reuse detection, role-based and atomic permission-based authorization, branch-scoped access controls, and administrative user management with comprehensive audit trails.

---

## 2. Authentication Architecture & Workflow

```text
WinForms Desktop Client
        |
        | 1. POST /api/v1/auth/login { UsernameOrEmail, Password }
        v
RateLimiter Middleware (10 req/min per IP)
        |
CorrelationId & Exception Middlewares
        |
        v
AuthController.Login()
        |
        v
AuthenticationService.LoginAsync()
        |
        +---- 1. Find UserAccount by Username or Email (Include Employee, Roles, Branches)
        +---- 2. Check Lockout Status (IsLockedOut & LockoutEndAt > UtcNow)
        +---- 3. Constant-Time Password Verification (IPasswordHasherService via PBKDF2)
        +---- 4. Check User Status (IsActive == true)
        +---- 5. Check Employee Status (Employee.Status == "Active")
        +---- 6. Reset Failed Counter & Update LastLoginAt
        +---- 7. Resolve Effective Permissions & Authorized Branches
        +---- 8. Generate JWT Access Token (15 min lifespan)
        +---- 9. Generate Cryptographic 64-byte Refresh Token & Store SHA-256 Hash
        +---- 10. Create UserSession & Record LoginHistory
        |
        v
Return ApiResponse<LoginResponse>
        |
        v
WinForms Desktop Client (Stores tokens securely in Windows Credential Manager / Memory)
        |
        | 2. Bearer Access Token in Authorization header + X-Branch-Id
        v
Protected API Endpoints (PermissionAuthorizationHandler & BranchAuthorizationHandler)
```

---

## 3. Password Management & Hashing Strategy

1. **Hashing Algorithm**: Uses ASP.NET Core Identity's standard `PasswordHasher<UserAccount>`, which implements **PBKDF2 with HMAC-SHA256**, 128-bit cryptographically random salt, and 100,000 hash iterations (.NET 9 V3 format).
2. **Reversible Encryption Prohibited**: Passwords are never stored in plaintext or reversible formats.
3. **Constant-Time Verification**: Verification uses constant-time byte comparisons to prevent timing side-channel attacks.
4. **Automatic Rehash**: When password verification returns `PasswordVerificationResult.SuccessRehashNeeded`, the system automatically rehashes the password with modern algorithm parameters and updates `PasswordHash` in SQL Server.
5. **Validation Policy**:
   - Minimum length: **12 characters**.
   - Maximum length: **128 characters** (preventing CPU exhaustion DoS attacks).
   - Rejection of common, trivial, or easily guessable passwords via blocklist.
   - Enforced by `FluentValidation` across `LoginRequestValidator`, `ChangePasswordRequestValidator`, `CreateUserRequestValidator`, and `AdminResetPasswordRequestValidator`.
6. **Zero Exposure**: Hashes and salts are never returned in DTOs, API responses, or written to logs.

---

## 4. JWT Access Token Configuration & Claims

### Configuration Structure (`appsettings.json`)

```json
{
  "Jwt": {
    "Secret": "A_CRYPTOGRAPHICALLY_SECURE_SECRET_AT_LEAST_32_BYTES_LONG",
    "Issuer": "LogisticsManagement",
    "Audience": "LogisticsManagement.Client",
    "ExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 7
  }
}
```

### Token Claims Structure

| Claim | Type | Description |
| :--- | :--- | :--- |
| `sub` | `string` (long) | Database surrogate primary key (`UserAccount.Id`). |
| `nameid` | `string` (long) | Standard NameIdentifier claim. |
| `public_id` | `string` (UUID) | External public identifier for client API interaction. |
| `unique_name` | `string` | Unique username. |
| `email` | `string` | User's primary email address. |
| `employee_id`| `string?` | Associated `Employee.Id` when linked. |
| `branch_id` | `string?` | User's default operational branch ID. |
| `branch_access` | `string[]` | Repeated claim: all branch IDs the user is authorized to operate in. |
| `role` | `string[]` | Repeated claim: all active role codes assigned to the user. |
| `security_stamp`| `string` | Random GUID invalidated upon password change, role modification, or logout-all. |
| `jti` | `string` (UUID) | Cryptographically unique token identifier preventing token reuse. |

---

## 5. Refresh Token Lifecycle, Rotation & Reuse Detection

### Database Schema
Stored in `identity.RefreshTokens` table:
- `Id` (BIGINT PK)
- `UserId` (FK -> `identity.UserAccounts.Id`)
- `TokenHash` (VARCHAR(256), SHA-256 hex string, Indexed)
- `ExpiresAt` (DATETIME2)
- `IsRevoked` (BIT)
- `RevokedAt` (DATETIME2, NULL)
- `ReplacedByToken` (VARCHAR(256), NULL)
- `CreatedAt` (DATETIME2)
- `CreatedByIp` (VARCHAR(50), NULL)

### Rotation Strategy
1. The client sends raw `RefreshToken` to `POST /api/v1/auth/refresh`.
2. The server hashes the incoming token using SHA-256 and retrieves the entity from `identity.RefreshTokens`.
3. If the token is valid and unrevoked:
   - A new cryptographically random 64-byte refresh token is generated and hashed.
   - The old token is marked `IsRevoked = true`, `RevokedAt = DateTime.UtcNow`, `ReplacedByToken = newRefreshTokenHash`.
   - The new refresh token is saved.
   - A new JWT access token is issued.
   - Both tokens are returned in the response.

### Automated Reuse Detection (Anti-Theft)
If an incoming refresh token is found in the database but `IsRevoked == true`:
1. This indicates an unauthorized party or compromised client is attempting to replay a token that was already rotated.
2. The server immediately **invalidates all active refresh tokens** for that entire user account.
3. The server immediately **terminates all active user sessions** in `identity.UserSessions`.
4. The server generates a new `SecurityStamp` for `UserAccount`.
5. An audit security log is generated.
6. The request is rejected with `HTTP 401 Unauthorized`: `"Refresh token reuse detected. All active sessions have been terminated for security."`

---

## 6. Logout and Session Management

### Endpoints
1. `POST /api/v1/auth/logout`:
   - Requires authenticated user.
   - Revokes the supplied refresh token (if provided).
   - Marks the user's current session as `IsTerminated = true`.
2. `POST /api/v1/auth/logout-all`:
   - Requires authenticated user.
   - Atomically revokes all refresh tokens for the user (`UPDATE RefreshTokens SET IsRevoked = 1`).
   - Terminates all active sessions (`UPDATE UserSessions SET IsTerminated = 1`).
   - Resets `UserAccount.SecurityStamp`, instantly invalidating existing JWT tokens.
3. `GET /api/v1/auth/sessions`:
   - Returns a sanitized list of the authenticated user's active login sessions (`Id`, `DeviceInfo`, `IpAddress`, `StartedAt`, `LastActivityAt`, `ExpiresAt`, `IsCurrentSession`).
4. `DELETE /api/v1/auth/sessions/{sessionId}`:
   - Terminates a specific session belonging to the authenticated user.

---

## 7. Authorization Architecture (Three-Tier Enforcement)

```text
┌─────────────────────────────────────────────────────────────┐
│ 1. Authentication Layer                                     │
│    Validate JWT signature, expiration, issuer, audience    │
└──────────────────────────────┬──────────────────────────────┘
                               │
                               v
┌─────────────────────────────────────────────────────────────┐
│ 2. Role & Permission Layer (RBAC + Atomic Permissions)     │
│    PermissionRequirement & Dynamic Policy Provider         │
│    Request-Scoped PermissionService (Immediate Freshness)   │
└──────────────────────────────┬──────────────────────────────┘
                               │
                               v
┌─────────────────────────────────────────────────────────────┐
│ 3. Branch-Level Data Authorization Layer                    │
│    Evaluate X-Branch-Id against UserBranchAccess            │
│    Filter server-side SQL queries by authorized branches    │
└─────────────────────────────────────────────────────────────┘
```

### 7.1 Role-Based Access Control (RBAC)
- Users are assigned one or more `Roles` via `identity.UserRoles`.
- Predefined system roles: `SUPER_ADMIN`, `BRANCH_MANAGER`, `OPERATIONS_MANAGER`, `WAREHOUSE_STAFF`, `CUSTOMER_SERVICE`, `FINANCE_STAFF`, `READ_ONLY`.
- Roles are not hardcoded into business controller logic; instead, roles map to fine-grained permissions.

### 7.2 Permission-Based Authorization
- Permissions represent atomic actions formatted as `{module}.{resource}.{action}` (e.g. `identity.users.create`, `shipping.shipments.assign`).
- `PermissionPolicyProvider` synthesizes authorization policies on the fly for any dot-separated permission code.
- Controllers and endpoints are decorated with `[HasPermission(AppPermissions.Module.Action)]`.
- `PermissionAuthorizationHandler` evaluates permissions against `IPermissionService`.

### 7.3 Permission Freshness Strategy
- Rather than baking permissions into static JWT claims (which bloat the token and cannot be revoked without waiting for token expiration), permissions are evaluated on the server.
- `IPermissionService` is registered as a **Scoped service**:
  - Queries user permissions from `identity.RolePermissions` once per HTTP request.
  - Caches the permission set in-memory for the lifetime of that request.
  - Immediate revocation: any role or permission modification in the database takes effect on the very next HTTP request.
  - If a user's role or account is modified by an administrator, the user's `SecurityStamp` is updated and sessions terminated.

---

## 8. Branch-Level Access Control

1. **Resolution**: `CurrentBranchContext` inspects the incoming `X-Branch-Id` HTTP header. If absent, it defaults to the user's configured default branch (`UserAccount -> UserBranchAccess.IsDefault == true`).
2. **Validation**: `ICurrentUserService.HasBranchAccess(branchId)` checks:
   - If user is in `SUPER_ADMIN`, access is granted to all branches.
   - Otherwise, verifies that `branchId` exists in the user's authorized `UserBranchAccess` records.
3. **Data Access Level Enforcement**: Server queries for branch-scoped operational tables (`Orders`, `Shipments`, `Warehouses`, `InventoryBalances`) enforce filtering against the validated `ActiveBranchId`. Clients cannot access another branch's records by tampering with request parameters.

---

## 9. Administrative User Management

Administrative endpoints under `/api/v1/admin/users` allow full user lifecycle governance:
- **List Users**: Paginated search, role filter, branch filter, active status filter.
- **Get User**: Detailed profile with assigned roles, branch permissions, and linked employee.
- **Create User**: Sets username, email, temporary password, employee linkage, roles, and branch assignments.
- **Update User**: Updates email and employee linkage with uniqueness validation.
- **Status Toggle**: Activates, deactivates, or unlocks locked accounts. Deactivation revokes all active tokens and sessions. Includes self-deactivation prevention for administrators.
- **Assign Roles**: Replaces user roles and invalidates `SecurityStamp`. Prevents self-revocation of `SUPER_ADMIN`.
- **Assign Branches**: Replaces user branch authorizations.
- **Reset Password**: Administratively resets user password and invalidates all existing sessions.
- **Audit Logging**: All administrative mutations are recorded in `audit.AuditLogs` with actor identity, timestamp, and JSON before/after snapshots.

---

## 10. Rate Limiting & Security Middleware

1. **Rate Limiting**: Configured with ASP.NET Core 9 Rate Limiter using fixed window partitioning per client IP:
   - Policy: `"auth-rate-limit"`
   - Limit: **10 requests per minute**.
   - Applied to `/api/v1/auth/login` and `/api/v1/auth/refresh`.
   - Rejection status: `HTTP 429 Too Many Requests`.
2. **CORS Policy**: Configured to restrict allowed origins to explicit trusted client origins (e.g. desktop client endpoints). `AllowAnyOrigin` with credentials is explicitly prohibited.
3. **Correlation ID**: Propagated via `CorrelationIdMiddleware` (`X-Correlation-Id`) to correlate security logs across distributed services.
