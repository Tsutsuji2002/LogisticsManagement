# Notification Management Specification

## 1. Notification Creation Patterns (`INotificationService`)

The `INotificationService` abstraction supports four creation patterns plus unified `CreateAsync(CreateNotificationRequest)`:

1. **Single User** (`CreateForUserAsync`): Targets a specific `RecipientUserId` (validated against active `UserAccounts`).
2. **Multiple Users** (`CreateForUsersAsync`): Deduplicates `userIds` and issues one `Notification` row per active recipient.
3. **Branch-Scoped Broadcast** (`CreateForBranchAsync`): Resolves all active `UserAccounts` having an active `UserBranchAccess` grant for `BranchId` and attaches `BranchId` for administrative visibility scoping.
4. **System Broadcast** (`CreateSystemNotificationAsync`): Targets all active users in the enterprise (`Priority = "High"`).
5. **Direct Email** (`CreateNotificationRequest.RecipientEmail`): Dispatches an email notification even if an external recipient (e.g., scheduled report subscriber) does not have a `UserAccount`.

---

## 2. Data Model & Incremental Schema Additions

To properly separate delivery lifecycle status (`Pending`, `Queued`, `Sent`, `Delivered`, `Failed`, `Cancelled`) from user read state, incremental migration `AddNotificationReadAndOutboxClaimColumns` added the following columns to `notification.Notifications`:

- `IsRead` (`bit`, default `false`) & `ReadAt` (`datetime2(7)`, nullable)
- `NotificationType` (`nvarchar(100)`, nullable) for semantic filtering (`OrderConfirmed`, `DeliveryFailed`, `InvoiceIssued`, `CodDiscrepancy`, etc.)
- `BranchId` (`bigint`, nullable, FK -> `organization.Branches.Id` with `DeleteBehavior.Restrict`)
- `ExpiresAt` (`datetime2(7)`, nullable) for notification retention & expiration
- `EventKey` (`nvarchar(200)`, nullable, filtered unique index `WHERE [EventKey] IS NOT NULL`) for outbox deduplication

---

## 3. Read State, Filtering & User Isolation

- **Ownership Isolation**:
  - `GET /api/v1/notifications` and `GET /api/v1/notifications/unread-count` strictly bind queries to `_currentUser.UserId` derived from the verified JWT token—any client-supplied user ID is ignored.
  - `GET /api/v1/notifications/{id}` and `PATCH /api/v1/notifications/{id}/read` verify `notification.RecipientUserId == _currentUser.UserId`. Accessing another user's notification returns `403 Forbidden` unless the caller possesses `notification.notifications.manage` (or `SUPER_ADMIN`) within authorized branch scope.
- **Filtering & Pagination**:
  - Supports `isRead`, `notificationType`, `channel`, `status`, `fromDate`, `toDate`, `includeExpired`, `sortBy` (`createdAt`, `priority`, `status`, `isRead`), and `sortDescending`.
  - Expired notifications (`ExpiresAt <= UtcNow`) are excluded from unread counts and standard listings unless `includeExpired=true`.
- **Bulk Mark Read**:
  - `POST /api/v1/notifications/mark-all-read` updates all unread notifications owned by the authenticated user and records `ReadAt = UtcNow`.
