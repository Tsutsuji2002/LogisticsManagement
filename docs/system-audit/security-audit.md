# Security Audit

## 1. Authentication & JWT Lifecycle

- **Password & Session Design**: Passwords hashed via `PasswordHasherService`; refresh tokens use 64-byte CSPRNG tokens stored hashed (SHA-256) with reuse detection in `AuthenticationService`.
- **Rate Limiting**: `auth-rate-limit` (`10 req/min` IP-partitioned) protects login and refresh endpoints in `Program.cs`.
- **SEC-01 Fixed (Jwt:Secret Startup Guard)**: Previously `Program.cs` allowed falling back to `"CHANGE_THIS_TO_A_SECURE_SECRET_KEY_AT_LEAST_32_CHARS"` in any environment. Non-Development startup now throws `InvalidOperationException` if `Jwt:Secret` is missing, shorter than 32 bytes, or starts with `"CHANGE_THIS"`.
- **SEC-02 Fixed (Live Token Revocation & Account Status Check)**: Previously JWT validation trusted stateless signature/expiry only and `PermissionService` ignored `UserAccounts.IsActive` / `IsLockedOut`. Now:
  - `JwtBearerEvents.OnTokenValidated` queries `UserAccounts` (`AsNoTracking`) on every authenticated API call to reject inactive users, currently locked-out users, or tokens whose `security_stamp` claim does not match the database `SecurityStamp`.
  - `PermissionService.GetUserPermissionsAsync` and `GetUserBranchIdsAsync` immediately return empty sets if the user is inactive or locked out.
- **Session Termination Note**: `UserSessionService.TerminateSessionAsync` marks `UserSessions.IsTerminated = true`, while `RefreshTokens` has no foreign key to `UserSessions`; terminating a single other device session intentionally avoids rotating `SecurityStamp` (which would log out the caller's own session), whereas `LogoutAll` and `AdminResetPasswordAsync` revoke all `RefreshTokens` and rotate `SecurityStamp`.

## 2. Authorization & Cross-Branch Isolation (IDOR Protection)

- **SEC-03 Fixed (Inverted Branch Filter in Finance)**: `FinancialLedgerService.GetLedgerEntriesAsync`, `ShipmentChargeService.GetShipmentChargesAsync`, `InvoiceService.GetInvoicesAsync`, `PaymentService.GetPaymentsAsync`, and `CustomerBalanceService.GetBalancesAsync` previously gated filtering on `if (_currentUser.AccessibleBranchIds.Count > 0)` without checking `!IsSuperAdmin`. Changed consistently to filter whenever `!IsInRole(AppRoles.SuperAdmin) && !IsInRole("Admin")`.
- **SEC-04 Fixed (Missing Branch Checks in Shipping, Pickups, Returns & Carrier Ops)**:
  - Added branch verification to `ShipmentService.GetByOrderIdAsync`, `ShipmentService.CreateFromOrderAsync`, and internal `GetShipmentAsync` helpers across `ShipmentPackageService`, `ShipmentAddressService`, `ShipmentAssignmentService`, `DeliveryAttemptService`, `ProofOfDeliveryService`, and `ShipmentRouteService`.
  - Added `Customer.BranchId` branch scoping to `PickupRequestService` (`FindAsync`, `GetPickupRequestsAsync`, `CreateAsync`).
  - Added `OriginalShipment` branch scoping to `ReturnRequestService` (`FindAsync`, `GetReturnRequestsAsync`, `CreateAsync`).
  - Added branch access checks to `CarrierIntegrationServiceImpl.BookShipmentAsync` and `CancelShipmentAsync`.
- **SEC-05 Fixed (COD Reconciliation & Notification Write Branch Gaps)**:
  - `CodReconciliationService.GetSummaryAsync` now scopes `CODRemittances` queries to `AccessibleBranchIds` when `branchId == null` for non-admin users.
  - `NotificationService.EnsureWriteAccessAsync` now enforces branch checks matching `EnsureReadAccessAsync`.

## 3. Webhook Security & Secret Storage

- **SEC-06 Fixed (Webhook Signature Enforcement & Replay Deduplication)**:
  - `CarrierWebhookServiceImpl.ProcessWebhookAsync` now enforces HMAC-SHA256 verification (`X-Webhook-Signature` header via `CryptographicOperations.FixedTimeEquals`) whenever an active `CarrierConfiguration.WebhookSecret` exists, rejects unsigned requests in non-Development environments when no secret is configured, parses tracking/status/event IDs from JSON, and ignores replayed `ExternalEventId` values.
- **SEC-07 Documented Remaining Secret-at-Rest Risk**: `CarrierConfigurationServiceImpl` persists `ApiKey`, `ApiSecret`, `EncryptedPassword`, and `WebhookSecret` in database columns without symmetric encryption/DPAPI wrapping, though DTO responses mask secrets (`HasApiKey`, `HasWebhookSecret`, etc.). Adding `IDataProtector` envelope encryption is recommended before production multi-tenant deployment.

## 4. Export & File Surface

- `ReportExportService` exports CSV only (`CsvHelpers`: prefixes formula characters `=`, `+`, `-`, `@`, `\t`, `\r` with `'`, sanitizes download filenames, caps exports at `10,000` rows).
- No multipart `IFormFile` upload controller endpoints exist.
- `appsettings.json` retains a Development/placeholder `Jwt:Secret` and local SQL Express connection string; Production secrets must be supplied via environment variables or secret manager (enforced by startup validation).
