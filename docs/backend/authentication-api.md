# Authentication & Authorization API Reference

## 1. Authentication Endpoints (`/api/v1/auth`)

### 1.1 User Login
- **Endpoint**: `POST /api/v1/auth/login`
- **Rate Limit**: 10 requests / minute per IP
- **Anonymous Access**: Yes
- **Request Body**:
```json
{
  "usernameOrEmail": "admin_user",
  "password": "SecurePassword2026!",
  "deviceInfo": "WinForms Desktop / Windows 11"
}
```
- **Response** (`200 OK`):
```json
{
  "success": true,
  "data": {
    "accessToken": "eyJhbGciOi...",
    "accessTokenExpiresAt": "2026-11-19T08:15:00Z",
    "refreshToken": "w91K_v8X2...",
    "refreshTokenExpiresAt": "2026-11-26T08:00:00Z",
    "user": {
      "id": 1,
      "publicId": "e3b0c442-98fc-1c14-9afb-4c8996fb9242",
      "username": "admin_user",
      "email": "admin@logistics.vn",
      "displayName": "System Administrator",
      "employeeId": 1,
      "employeeCode": "EMP-001",
      "jobTitle": "Lead System Architect",
      "defaultBranchId": 10,
      "defaultBranchName": "Hanoi Central Hub",
      "authorizedBranches": [
        {
          "branchId": 10,
          "branchPublicId": "550e8400-e29b-41d4-a716-446655440000",
          "code": "BR-HN-01",
          "name": "Hanoi Central Hub",
          "isDefault": true
        }
      ],
      "roles": ["SUPER_ADMIN"],
      "permissions": ["identity.users.read", "identity.users.create", "..."],
      "isActive": true
    }
  },
  "message": "Authentication successful.",
  "errorCode": null,
  "traceId": null
}
```
- **Error Responses**:
  - `400 Bad Request`: Validation failure (empty username or password).
  - `401 Unauthorized`: Invalid credentials, inactive account, or inactive employee profile.
  - `403 Forbidden`: Account temporarily locked out.
  - `429 Too Many Requests`: Rate limit exceeded.

---

### 1.2 Token Refresh & Rotation
- **Endpoint**: `POST /api/v1/auth/refresh`
- **Rate Limit**: 10 requests / minute per IP
- **Anonymous Access**: Yes
- **Request Body**:
```json
{
  "refreshToken": "w91K_v8X2...",
  "deviceInfo": "WinForms Desktop / Windows 11"
}
```
- **Response** (`200 OK`): Returns new `LoginResponse` containing a fresh `accessToken` and rotated `refreshToken`.
- **Error Responses**:
  - `401 Unauthorized`: Invalid, expired, or previously revoked token.

---

### 1.3 User Logout
- **Endpoint**: `POST /api/v1/auth/logout`
- **Authorization**: Bearer Token required
- **Request Body** (optional):
```json
{
  "refreshToken": "w91K_v8X2..."
}
```
- **Response** (`200 OK`):
```json
{
  "success": true,
  "message": "Logged out successfully."
}
```

---

### 1.4 Terminate All Sessions (Logout All)
- **Endpoint**: `POST /api/v1/auth/logout-all`
- **Authorization**: Bearer Token required
- **Response** (`200 OK`): Revokes all active refresh tokens and user sessions, and rotates the security stamp.

---

### 1.5 Get Current Profile
- **Endpoint**: `GET /api/v1/auth/me`
- **Authorization**: Bearer Token required
- **Response** (`200 OK`): Returns current user profile with fresh effective permissions and authorized branches.

---

### 1.6 Change Password
- **Endpoint**: `POST /api/v1/auth/change-password`
- **Authorization**: Bearer Token required
- **Request Body**:
```json
{
  "currentPassword": "OldPassword123!",
  "newPassword": "NewUniqueStrongPassword2026!"
}
```
- **Response** (`200 OK`): Updates password hash, invalidates security stamp, and terminates all active sessions.

---

### 1.7 List Active Sessions
- **Endpoint**: `GET /api/v1/auth/sessions`
- **Authorization**: Bearer Token required
- **Response** (`200 OK`):
```json
{
  "success": true,
  "data": [
    {
      "id": 105,
      "deviceInfo": "WinForms Desktop / Windows 11",
      "ipAddress": "192.168.1.50",
      "startedAt": "2026-11-19T08:00:00Z",
      "lastActivityAt": "2026-11-19T08:14:00Z",
      "expiresAt": "2026-11-26T08:00:00Z",
      "isCurrentSession": true
    }
  ]
}
```

---

### 1.8 Terminate Specific Session
- **Endpoint**: `DELETE /api/v1/auth/sessions/{sessionId}`
- **Authorization**: Bearer Token required
- **Response** (`200 OK`): Terminate the specified user session.

---

## 2. Administrative User Management (`/api/v1/admin/users`)

All endpoints in this section require explicit permissions.

### 2.1 List Users
- **Endpoint**: `GET /api/v1/admin/users`
- **Permission**: `identity.users.read`
- **Query Parameters**:
  - `search` (string?): Filters by username, email, or employee name.
  - `role` (string?): Filters by assigned role code.
  - `branchId` (long?): Filters by authorized branch ID.
  - `isActive` (bool?): Filters by active status.
  - `page` (int, default: 1): Page number.
  - `pageSize` (int, default: 20): Items per page.
- **Response** (`200 OK`): Paginated collection of `UserAccountDto`.

---

### 2.2 Get User by PublicId
- **Endpoint**: `GET /api/v1/admin/users/{publicId}`
- **Permission**: `identity.users.read`
- **Response** (`200 OK`): Single `UserAccountDto`.

---

### 2.3 Create User Account
- **Endpoint**: `POST /api/v1/admin/users`
- **Permission**: `identity.users.create`
- **Request Body**:
```json
{
  "username": "dispatcher_hn",
  "email": "dispatcher.hn@logistics.vn",
  "password": "InitialTempPassword2026!",
  "employeeId": 12,
  "roles": ["OPERATIONS_MANAGER"],
  "branchIds": [10, 11],
  "defaultBranchId": 10
}
```
- **Response** (`201 Created`): Returns newly created `UserAccountDto`.

---

### 2.4 Update User Account
- **Endpoint**: `PUT /api/v1/admin/users/{publicId}`
- **Permission**: `identity.users.update`
- **Request Body**:
```json
{
  "email": "new.email@logistics.vn",
  "employeeId": 15
}
```

---

### 2.5 Update User Status (Activate/Deactivate/Unlock)
- **Endpoint**: `PATCH /api/v1/admin/users/{publicId}/status`
- **Permission**: `identity.users.update`
- **Request Body**:
```json
{
  "isActive": false,
  "unlockAccount": true
}
```
*Note*: Self-deactivation by the active administrator is prohibited and returns `403 Forbidden`. Deactivation immediately revokes all active sessions and refresh tokens.

---

### 2.6 Assign Roles
- **Endpoint**: `PUT /api/v1/admin/users/{publicId}/roles`
- **Permission**: `identity.users.roles.manage`
- **Request Body**:
```json
{
  "roles": ["BRANCH_MANAGER", "WAREHOUSE_STAFF"]
}
```
*Note*: Updates user roles and invalidates `SecurityStamp`. Self-revocation of `SUPER_ADMIN` role is blocked.

---

### 2.7 Assign Authorized Branches
- **Endpoint**: `PUT /api/v1/admin/users/{publicId}/branches`
- **Permission**: `identity.users.branches.manage`
- **Request Body**:
```json
{
  "branchIds": [10, 20, 30],
  "defaultBranchId": 10
}
```

---

### 2.8 Administrative Password Reset
- **Endpoint**: `POST /api/v1/admin/users/{publicId}/reset-password`
- **Permission**: `identity.users.reset-password`
- **Request Body**:
```json
{
  "newPassword": "AdminGeneratedStrongPass2026!"
}
```
*Note*: Updates the password hash, invalidates the security stamp, and terminates all active user sessions.

---

## 3. Roles and Permissions Metadata (`/api/v1/admin`)

### 3.1 Get All Roles
- **Endpoint**: `GET /api/v1/admin/roles`
- **Permission**: `identity.roles.read`
- **Response** (`200 OK`): List of all active system roles and their assigned permissions.

### 3.2 Get All Permissions
- **Endpoint**: `GET /api/v1/admin/permissions`
- **Permission**: `identity.roles.read`
- **Response** (`200 OK`): Complete catalog of atomic system permissions grouped by module.
