# Discovered Issues & Remediation Ledger

## Issue Index Summary

| ID | Module | Severity | Status |
|---|---|---|---|
| SEC-01 | Auth / Config | Critical | Fixed & Verified |
| SEC-02 | Auth / Permissions | Critical | Fixed & Verified |
| SEC-03 | Finance | Critical | Fixed & Verified |
| SEC-04 | Shipping / Carrier | High | Fixed & Verified |
| SEC-05 | COD / Notifications | High | Fixed & Verified |
| SEC-06 | Carrier Webhooks | Critical | Fixed & Verified |
| DI-01 | Finance (Invoices) | High | Fixed & Verified |
| DI-02 | Finance (Payments) | High | Fixed & Verified |
| DI-03 | COD (Collections) | High | Fixed & Verified |
| DI-04 | COD (Remittances) | High | Fixed & Verified |
| DI-05 | Sales / Shipping | Medium | Fixed & Verified |
| DI-06 | Shipping (POD / Returns) | Medium | Fixed & Verified |
| DI-07 | Warehouse (Reservations) | High | Fixed & Verified |
| DI-08 | Warehouse (Parcel / Dispatch) | High | Fixed & Verified |
| INT-04 | Outbox & Background Jobs | High | Fixed & Verified |
| CON-01 | Configuration (Sequences) | High | Fixed & Verified |
| UI-01 | WinForms Desktop | Medium | Fixed & Verified |
| LIM-01 | Carrier Secrets Storage | Medium | Documented Remaining Risk |
| LIM-02 | Inventory Schema (Adj/Trans) | Medium | Documented Schema Limitation |
| LIM-03 | Carrier Adapter Live APIs | Informational | Documented External Simulation |

---

## Detailed Issue Records

### SEC-01 — Placeholder `Jwt:Secret` Fallback Allowed Outside Development
- **Module**: Authentication & API Startup (`Logistics.Server.API`)
- **Severity**: Critical
- **Reproduction steps**: Start API with `ASPNETCORE_ENVIRONMENT=Production` and placeholder or omitted `Jwt:Secret` (`CHANGE_THIS...`). Previously the API booted and signed tokens using the known literal string.
- **Root cause**: `Program.cs` used `jwtSection["Secret"] ?? "CHANGE_THIS_TO_A_SECURE_SECRET_KEY_AT_LEAST_32_CHARS"` without validating environment or secret strength.
- **Affected components**: `src/Server/Logistics.Server.API/Program.cs`, `appsettings.json`.
- **Fix applied**: Added fail-fast startup validation rejecting missing, `< 32`-byte, or `"CHANGE_THIS..."` secrets in any non-Development environment (`InvalidOperationException`).
- **Test added**: `HardeningAndE2EIntegrationTests.ProductionConfig_PlaceholderJwtSecret_FailsStartupValidation`.
- **Verification result**: Verified — startup throws `InvalidOperationException` under `Production` environment.
- **Remaining risk**: Low (`appsettings.json` retains a Development value; Production deployments must inject a secret via environment variables or secret manager).

### SEC-02 — Inactive/Locked Users & Rotated `SecurityStamp` Accepted Until JWT Expiry
- **Module**: Authentication & Authorization (`Logistics.Server.API`, `Logistics.Server.Infrastructure`)
- **Severity**: Critical
- **Reproduction steps**: Issue a JWT to a user; admin updates user roles (rotating `SecurityStamp`) or deactivates the account; replay the unexpired JWT against `/api/v1/auth/me` or permissioned endpoints. Previously returned `200 OK`.
- **Root cause**: `AddJwtBearer` lacked `OnTokenValidated` revocation checks and `PermissionService` did not filter out inactive or locked-out `UserAccounts`.
- **Affected components**: `src/Server/Logistics.Server.API/Program.cs`, `src/Server/Logistics.Server.Infrastructure/Services/PermissionService.cs`.
- **Fix applied**: Added `JwtBearerEvents.OnTokenValidated` validating `IsActive`, `!IsLockedOut`/`LockoutEndAt`, and `security_stamp` against `UserAccounts`; updated `PermissionService.GetUserPermissionsAsync` and `GetUserBranchIdsAsync` to return empty sets for inactive or locked-out users.
- **Test added**: `HardeningAndE2EIntegrationTests.Security_RevokedSecurityStamp_And_DeactivatedUser_RejectExistingJwtImmediately`.
- **Verification result**: Verified — JWT immediately fails with `401 Unauthorized` after role update/stamp rotation.
- **Remaining risk**: Low (single-session termination in `UserSessionService` intentionally preserves other active JWTs for caller continuity until logout-all/password reset).

### SEC-03 — Inverted Branch Filter Logic in Finance Services
- **Module**: Finance (`Logistics.Server.Infrastructure`)
- **Severity**: Critical
- **Reproduction steps**: Query `/api/v1/finance/invoices`, `/api/v1/finance/ledger/entries`, `/api/v1/finance/payments`, `/api/v1/finance/shipping-fees/charges`, or `/api/v1/finance/balances` as non-admin with 0 branch assignments vs `SuperAdmin` with seeded branch claims.
- **Root cause**: All five query methods checked `if (_currentUser.AccessibleBranchIds.Count > 0)` instead of checking `!IsInRole(AppRoles.SuperAdmin) && !IsInRole("Admin")`. A non-admin user with zero branches bypassed filtering and received all branches' financial records.
- **Affected components**: `src/Server/Logistics.Server.Infrastructure/Services/FinanceServices.cs`.
- **Fix applied**: Replaced all 5 checks with `if (!_currentUser.IsInRole(AppRoles.SuperAdmin) && !_currentUser.IsInRole("Admin"))`.
- **Test added**: `HardeningAndE2EIntegrationTests.Security_CrossBranchIsolation_DeniesCrossBranchFinanceCodAndShippingAccess`.
- **Verification result**: Verified — HAN-scoped user does not see SGN invoices or ledger entries and receives `403 Forbidden` on direct SGN invoice ID access.
- **Remaining risk**: None identified.

### SEC-04 — Missing Branch Isolation in Shipping Helpers, Pickups, Returns & Carrier Ops
- **Module**: Shipping & Carrier Integration
- **Severity**: High
- **Reproduction steps**: Authenticated user scoped to branch HAN accesses `/api/v1/shipments/{sgnShipmentId}/packages`, `/routes`, `/pickup-requests`, `/returns`, `/api/v1/shipments/order/{orderId}`, or calls `/api/v1/carrier-integration/book` for an SGN shipment.
- **Root cause**: Internal `GetShipmentAsync`, `GetByOrderIdAsync`, `CreateFromOrderAsync`, `PickupRequestService`, `ReturnRequestService`, and `CarrierIntegrationServiceImpl` omitted `HasBranchAccess` / `ApplyBranchFilter`.
- **Affected components**: `src/Server/Logistics.Server.Infrastructure/Services/ShippingServices.cs`, `CarrierServices.cs`.
- **Fix applied**: Enforced branch ownership on all shipment sub-services, pickup requests (via `Customer.BranchId`), return requests (via `OriginalShipment`), order-to-shipment lookups/creation, and carrier booking/cancellation.
- **Test added**: `HardeningAndE2EIntegrationTests.Security_CrossBranchIsolation_DeniesCrossBranchFinanceCodAndShippingAccess`.
- **Verification result**: Verified — HAN-scoped user receives `403 Forbidden` on SGN shipment sub-endpoints.
- **Remaining risk**: None identified.

### SEC-05 — Missing Branch Scoping in COD Reconciliation Summary & Notification Write Access
- **Module**: COD & Notifications
- **Severity**: High
- **Reproduction steps**: Call `/api/v1/cod/reconciliation/summary` without `branchId` as non-admin (`remQuery` previously included all branches' remittances) or modify another branch's notification with `NotificationsManage`.
- **Root cause**: `CodReconciliationService.GetSummaryAsync` filtered `colQuery` but omitted branch filtering on `remQuery`; `NotificationService.EnsureWriteAccessAsync` omitted branch check present in `EnsureReadAccessAsync`.
- **Affected components**: `src/Server/Logistics.Server.Infrastructure/Services/CodServices.cs`, `NotificationServices.cs`.
- **Fix applied**: Added non-admin `AccessibleBranchIds` and date range filters to `remQuery`; added branch access validation to `EnsureWriteAccessAsync`.
- **Test added**: Verified alongside existing `CodIntegrationTests` and `NotificationAndBackgroundIntegrationTests`.
- **Verification result**: Passed full integration suite (`118/118`).
- **Remaining risk**: None identified.

### SEC-06 — Unauthenticated/Unsigned Carrier Webhooks Accepted Without Signature Verification, Parsing, or Replay Check
- **Module**: Carrier Webhooks (`Logistics.Server.Infrastructure`)
- **Severity**: Critical
- **Reproduction steps**: `POST /api/v1/carrier-webhooks/{carrier}` with arbitrary un-signed JSON payload or replay identical payload twice. Previously returned `200 OK`, bypassed `WebhookSecret`, stored `"PENDING_PARSE"`, and created duplicate `CarrierTrackingEvents`.
- **Root cause**: `CarrierWebhookServiceImpl.ProcessWebhookAsync` ignored `signature` parameter, hardcoded placeholder event data, and lacked idempotency lookup on `ExternalEventId`.
- **Affected components**: `src/Server/Logistics.Server.Infrastructure/Services/CarrierServices.cs`.
- **Fix applied**: Added constant-time HMAC-SHA256 validation (`X-Webhook-Signature`) when `CarrierConfiguration.WebhookSecret` is configured (rejecting with `403` when missing/invalid and rejecting unconfigured secrets outside Development), parsed JSON tracking/status/event IDs, deduplicated replays on `(CarrierId, ExternalEventId)`, and applied `ShipmentStateHelper` + `ShipmentStatusHistory` + outbox events.
- **Test added**: `HardeningAndE2EIntegrationTests.Security_Webhook_SignatureVerification_And_ReplayDeduplication`.
- **Verification result**: Verified — unsigned/forged requests return `403 Forbidden`, valid signed webhook transitions shipment to `Delivered`, and replay produces only 1 tracking event.
- **Remaining risk**: Carrier `GHTK/GHN/VTPOST` header format normalization may need carrier-specific header extractors if live webhook schemas differ from standard `X-Webhook-Signature`.

### DI-01 — Double-Invoicing & Cross-Customer `ShipmentCharge` Linking
- **Module**: Finance (`InvoiceService`)
- **Severity**: High
- **Reproduction steps**: Submit two invoices referencing the same `ShipmentChargeId`, or reference a `ShipmentChargeId` belonging to another customer.
- **Root cause**: `CreateDraftInvoiceAsync` and `AddInvoiceItemAsync` only set `charge.IsInvoiced = true` without verifying ownership, existence, or prior `IsInvoiced`/`InvoiceId` state.
- **Affected components**: `src/Server/Logistics.Server.Infrastructure/Services/FinanceServices.cs`.
- **Fix applied**: Loaded `Shipment` on `ShipmentCharge`, rejected missing charges (`404`), cross-customer charges (`409`), and already-invoiced charges (`409`), and set `charge.IsInvoiced = true` + `Invoice`/`InvoiceId`.
- **Test added**: `HardeningAndE2EIntegrationTests.Idempotency_DoubleInvoicing_DoubleCarrierBook_And_UnverifiedCodRemittance_Rejected`.
- **Verification result**: Verified — second invoice referencing same charge returns `409 Conflict`.
- **Remaining risk**: None identified.

### DI-02 — Payment Ledger `ReferenceId = 0` & Arbitrary User Fallback
- **Module**: Finance (`PaymentService`)
- **Severity**: High
- **Reproduction steps**: Call `POST /api/v1/finance/payments` and inspect `FinancialLedgerEntries` (`ReferenceId` was `0`).
- **Root cause**: `RegisterPaymentAsync` posted the ledger entry before saving the `Payment` entity (`referenceId: 0`), lacked a relational transaction, and fell back to `UserAccounts.FirstAsync()` if username lookup missed.
- **Affected components**: `src/Server/Logistics.Server.Infrastructure/Services/FinanceServices.cs`.
- **Fix applied**: Persisted `Payment` first inside an explicit relational transaction before posting `FinancialLedgerEntry` with `referenceId: payment.Id`, and removed arbitrary `.FirstAsync()` impersonation.
- **Test added**: `HardeningAndE2EIntegrationTests.EndToEnd_Workflow_Order_Shipping_ParcelHub_Delivery_Pod_Finance_Cod_And_Dashboard_Succeeds`.
- **Verification result**: Verified — `payLedger.ReferenceId == payment.Id`.
- **Remaining risk**: None identified.

### DI-03 — Premature `TotalCodHoldingAmount` Mutation & Uncapped Collection Update
- **Module**: COD (`CodCollectionService`)
- **Severity**: High
- **Reproduction steps**: Update a `CollectedByDriver` COD collection amount via `RecordCollectionAsync`, then call `VerifyCollectionAsync`; `CustomerBalance.TotalCodHoldingAmount` was double-counted.
- **Root cause**: `RecordCollectionAsync` adjusted `TotalCodHoldingAmount` regardless of whether the collection had reached `VerifiedByCashier`, and omitted the `<= Shipment.CodAmount` upper-bound check.
- **Affected components**: `src/Server/Logistics.Server.Infrastructure/Services/CodServices.cs`.
- **Fix applied**: Enforced `0 <= CollectedAmount <= Shipment.CodAmount` and restricted `TotalCodHoldingAmount` delta adjustments to collections already in `VerifiedByCashier`.
- **Test added**: Covered by `CodIntegrationTests` lifecycle assertions (`TotalCodHoldingAmount` verification after cashier verify).
- **Verification result**: All COD integration tests pass (`118/118`).
- **Remaining risk**: None identified.

### DI-04 — Unverified COD Collections Remittable & Promoted on Cancellation/Removal
- **Module**: COD (`CodRemittanceService`)
- **Severity**: High
- **Reproduction steps**: Include a `CollectedByDriver` collection ID in `CreateCodRemittanceRequest` or cancel/remove an item from a remittance whose collection wasn't verified.
- **Root cause**: `CreateRemittanceAsync` and `AddItemAsync` did not check `Status == "VerifiedByCashier"`; `RemoveItemAsync` and `CancelRemittanceAsync` unconditionally set `Status = "VerifiedByCashier"`.
- **Affected components**: `src/Server/Logistics.Server.Infrastructure/Services/CodServices.cs`.
- **Fix applied**: Enforced `Status == "VerifiedByCashier"` on remittance create/add and restored `"VerifiedByCashier"` on cancel/remove only when `Status == "RemittedToCustomer"`.
- **Test added**: `HardeningAndE2EIntegrationTests.Idempotency_DoubleInvoicing_DoubleCarrierBook_And_UnverifiedCodRemittance_Rejected`.
- **Verification result**: Verified — unverified collection remittance attempt returns `409 Conflict`.
- **Remaining risk**: None identified.

### DI-05 — Order Cancelled After Shipment PickedUp / OutForDelivery
- **Module**: Sales (`OrderService`)
- **Severity**: Medium
- **Reproduction steps**: Confirm order, create shipment, transition shipment to `PickedUp` or `InSortingHub`, then call `POST /api/v1/orders/{id}/cancel`.
- **Root cause**: `OrderService.CancelAsync` only checked `ShipmentStatus == "InTransit" || ShipmentStatus == "Delivered"`.
- **Affected components**: `src/Server/Logistics.Server.Infrastructure/Services/SalesServices.cs`.
- **Fix applied**: Blocked cancellation whenever any linked shipment is in `PickedUp`, `InSortingHub`, `InTransit`, `OutForDelivery`, `Delivered`, `Returning`, or `Returned`.
- **Test added**: Asserted directly in `EndToEnd_Workflow_Order_Shipping_ParcelHub_Delivery_Pod_Finance_Cod_And_Dashboard_Succeeds` (`409 Conflict`).
- **Verification result**: Verified.
- **Remaining risk**: None identified.

### DI-06 — POD Allowed Against Failed Attempts & Unchecked Return State Transition
- **Module**: Shipping (`ProofOfDeliveryService`, `ReturnRequestService`)
- **Severity**: Medium
- **Reproduction steps**: Create POD referencing a `Failed` `DeliveryAttempt`, or approve a `ReturnRequest` for a `Delivered` shipment.
- **Root cause**: `ProofOfDeliveryService.CreateAsync` did not check `attempt.Status == "Delivered"` or `ShipmentStateHelper`; `ReturnRequestService.ProcessAsync` directly assigned `ShipmentStatus = "Returning"` bypassing `ShipmentStateHelper`.
- **Affected components**: `src/Server/Logistics.Server.Infrastructure/Services/ShippingServices.cs`.
- **Fix applied**: Required `attempt.Status == "Delivered"` and valid transition for POD; gated `"Returning"` transition in `ReturnRequestService.ProcessAsync` behind `ShipmentStateHelper.IsValidTransition`. Also enforced duplicate booking & valid state checks in `CarrierServices.cs`.
- **Test added**: Asserted failed-attempt POD rejection (`409`) in `EndToEnd_Workflow...` and return approval compatibility.
- **Verification result**: Verified.
- **Remaining risk**: None identified.

### DI-07 — `ConsumeReservationAsync` Hardcoded Employee ID & Negative Clamping
- **Module**: Warehouse & Inventory (`InventoryService`)
- **Severity**: High
- **Reproduction steps**: Call `ConsumeReservationAsync` when `Employee` #1 does not exist or when physical balance was adjusted below `ReservedQuantity`.
- **Root cause**: Hardcoded `PerformedByEmployeeId = 1` and clamped `Math.Max(0, ...)` without checking `balance == null` or `QuantityOnHand < ReservedQuantity`.
- **Affected components**: `src/Server/Logistics.Server.Infrastructure/Services/WarehouseServices.cs`.
- **Fix applied**: Wrapped in relational transaction, validated balance existence and stock/allocation sufficiency (`ConflictException`), and resolved `_currentUser.EmployeeId` -> `UserAccount.EmployeeId` -> active employee fallback cleanly.
- **Test added**: Covered by `WarehouseIntegrationTests`.
- **Verification result**: Passed.
- **Remaining risk**: None identified.

### DI-08 — Parcel/Dispatch Exception Swallowing & Missing `DispatchItems` Stock Deduction
- **Module**: Warehouse (`ParcelHandlingService`)
- **Severity**: High
- **Reproduction steps**: Create `WarehouseDispatch` with physical `Items` (`DispatchItems`), confirm dispatch, and observe `InventoryBalances` unchanged; or trigger invalid shipment status transition during `ReceiveParcelAsync`/`ConfirmDispatchAsync` and observe split state persisted.
- **Root cause**: `ReceiveParcelAsync` and `ConfirmDispatchAsync` caught and logged warnings on `TransitionStatusAsync` without rolling back, and `ConfirmDispatchAsync` omitted iterating `dispatch.DispatchItems`.
- **Affected components**: `src/Server/Logistics.Server.Infrastructure/Services/WarehouseServices.cs`.
- **Fix applied**: Wrapped both operations in relational transactions, removed silent exception swallowing, and added atomic stock deduction + `InventoryTransaction (Dispatch)` creation for `DispatchItems`. Also blocked same-warehouse / non-positive `CreateTransferAsync`.
- **Test added**: `HardeningAndE2EIntegrationTests.EndToEnd_Warehouse_DispatchItems_DeductsInventoryAtomically`.
- **Verification result**: Verified (`50 -> 38` on hand).
- **Remaining risk**: None identified.

### INT-04 — Outbox & Background Job Shared `ChangeTracker` Poisoning on Handler Failure
- **Module**: Outbox & Background Processing
- **Severity**: High
- **Reproduction steps**: Process a batch where message 1's handler stages an invalid entity onto `_context` and throws, followed by message 2 in the same batch.
- **Root cause**: Catch blocks saved retry updates while leaving dirty entities from failed handlers inside `_context.ChangeTracker`. (Initial detachment fix also detached sibling batch `OutboxMessage` entities, fixed by tracking `claimedSet`.)
- **Affected components**: `src/Server/Logistics.Server.Infrastructure/Services/BackgroundProcessingServices.cs`.
- **Fix applied**: Detach only entries not in `claimedSet` before saving retry metadata in both `OutboxProcessorService` and `BackgroundJobService`.
- **Test added**: `SqlServerHardeningIntegrationTests.OutboxProcessorService_FailingHandlerWithDirtyTracker_IsolatesFailureAndCompletesBatch`.
- **Verification result**: Verified on SQL Server LocalDB (`msg1` -> `Pending` with `RetryCount=1`, `msg2` -> `Published`).
- **Remaining risk**: None identified.

### CON-01 — In-Process Only `NumberSequenceService` Locking
- **Module**: Configuration (`NumberSequenceService`)
- **Severity**: High
- **Reproduction steps**: Run concurrent number allocations across multiple DbContext instances/processes on SQL Server.
- **Root cause**: `SemaphoreSlim` only serialized callers inside one process.
- **Affected components**: `src/Server/Logistics.Server.Infrastructure/Services/NumberSequenceService.cs`.
- **Fix applied**: Added explicit transaction wrapping and SQL Server `FROM [configuration].[NumberSequences] WITH (UPDLOCK, ROWLOCK)` locking when running on `Microsoft.EntityFrameworkCore.SqlServer`.
- **Test added**: `SqlServerHardeningIntegrationTests.NumberSequenceService_SqlServerConcurrentCalls_GenerateUniqueSequentialNumbers`.
- **Verification result**: Verified across 12 concurrent tasks on SQL Server LocalDB with 12 unique sequential numbers generated.
- **Remaining risk**: First-time unseeded initialization of a brand-new `Scope` across concurrent instances remains subject to unique index race (all standard scopes are pre-seeded).

### UI-01 — Unchecked Workspace Tabs on WinForms Warehouse, Inventory & Carrier Controls
- **Module**: WinForms Desktop Client (`Logistics.Client.WinForms`)
- **Severity**: Medium
- **Reproduction steps**: Log into desktop client without Warehouse/Inventory/Carrier permissions; navigate to workspace or invoke `SelectSection`.
- **Root cause**: `WarehouseWorkspaceControl`, `InventoryWorkspaceControl`, and `CarrierWorkspaceControl` added all `TabPage`s unconditionally and set `_tabs.SelectedTab` without verifying tab containment.
- **Affected components**: `WarehouseWorkspaceControl.cs`, `InventoryWorkspaceControl.cs`, `CarrierWorkspaceControl.cs`.
- **Fix applied**: Gated tab registration by `ClientPermissions` and guarded `SelectSection` with `_tabs.TabPages.Contains(...)`.
- **Test added**: Verified full `net9.0-windows` WinForms compilation (`0` errors).
- **Verification result**: Compiled cleanly; behavioral runtime execution requires manual Windows desktop UI session.
- **Remaining risk**: Automated UI click/render tests are not present for WinForms controls.

### LIM-01 / LIM-02 / LIM-03 — Documented Architectural & Schema Limitations (No Schema Migration Applied)
- **LIM-01 (Medium)**: `CarrierConfigurations` secrets are stored in database columns without symmetric envelope encryption (`IDataProtector`), though masked in API responses (`HasApiKey`, `HasWebhookSecret`).
- **LIM-02 (Medium)**: `StockAdjustments` and `StockTransfers` lack item detail child tables in the EF Core schema; per-item balance postings upon adjustment approval/transfer receipt require a schema migration adding item detail tables.
- **LIM-03 (Informational)**: `GhnCarrierAdapter`, `GhtkCarrierAdapter`, and `ViettelPostCarrierAdapter` provide simulated integration adapters and do not connect to external carrier production endpoints.
