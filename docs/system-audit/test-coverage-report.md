# Test Coverage Report

## Final Verified Test Execution Summary

Executed on solution `LogisticsManagement.sln` after all code hardening edits:

- **Solution Build**: `dotnet build LogisticsManagement.sln --no-restore` — **Succeeded** (`0 Warning(s)`, `0 Error(s)`, all 13 projects including `Logistics.Client.WinForms` `net9.0-windows` and `Logistics.Server.Worker`).
- **Test Execution** (`dotnet test LogisticsManagement.sln --no-restore --no-build`):
  - `Logistics.Client.UnitTests.dll`: **17 Passed**, 0 Failed, 0 Skipped (Total: 17, Duration: 1 s)
  - `Logistics.Server.UnitTests.dll`: **253 Passed**, 0 Failed, 0 Skipped (Total: 253, Duration: 6 s)
  - `Logistics.Server.IntegrationTests.dll`: **118 Passed**, 0 Failed, 0 Skipped (Total: 118, Duration: 1 m 40 s)
  - **Combined Total**: **388 Passed**, 0 Failed, 0 Skipped

## New Hardening & End-to-End Integration Tests Added

### 1. Cross-Module E2E, Security, Webhook & Idempotency Suite
File: `tests/Logistics.Server.IntegrationTests/HardeningAndE2EIntegrationTests.cs`

1. `EndToEnd_Workflow_Order_Shipping_ParcelHub_Delivery_Pod_Finance_Cod_And_Dashboard_Succeeds`: Tests Customer -> Sales Order Create & Confirm -> Create Shipment -> Add Freight Charge -> PickUp -> Parcel Hub Receive -> Order Cancellation Blocked (`409`) -> InTransit -> OutForDelivery -> Failed Delivery Attempt -> POD Rejected (`409`) -> Delivered Attempt -> POD Created -> COD Collection -> Deposit -> Cashier Verify -> Remittance Create & Confirm -> Invoice with `ShipmentChargeId` -> Issue -> Register Payment (`FinancialLedgerEntry.ReferenceId == payment.Id`) -> Dashboard Overview.
2. `EndToEnd_Warehouse_DispatchItems_DeductsInventoryAtomically`: Tests Inventory Warehouse Receipt Confirmation (`+50`) -> `WarehouseDispatch` with `DispatchItems` Confirmation (`-12`) -> Verified `InventoryBalance.QuantityOnHand == 38`.
3. `Security_CrossBranchIsolation_DeniesCrossBranchFinanceCodAndShippingAccess`: Verifies a HAN-scoped user with `BranchManager` + `FinanceStaff` roles cannot view SGN invoices or ledger entries and receives `403 Forbidden` on direct SGN shipment packages, routes, and invoice endpoints.
4. `Security_RevokedSecurityStamp_And_DeactivatedUser_RejectExistingJwtImmediately`: Verifies admin role change rotates `SecurityStamp` and immediately rejects the still-unexpired JWT with `401 Unauthorized`.
5. `Security_Webhook_SignatureVerification_And_ReplayDeduplication`: Verifies `403 Forbidden` on missing or invalid HMAC signatures, `200 OK` + shipment transition to `Delivered` on valid `X-Webhook-Signature` (`sha256=...`), and idempotent ignore on replay (`1` `CarrierTrackingEvents` row).
6. `Idempotency_DoubleInvoicing_DoubleCarrierBook_And_UnverifiedCodRemittance_Rejected`: Verifies `409 Conflict` on duplicate carrier booking, double-invoicing the same `ShipmentChargeId`, and creating COD remittances from unverified (`CollectedByDriver`) collections.
7. `ProductionConfig_PlaceholderJwtSecret_FailsStartupValidation`: Verifies `Production` environment throws `InvalidOperationException` when configured with placeholder `Jwt:Secret`.

### 2. SQL Server LocalDB Concurrency & Outbox Isolation Suite
File: `tests/Logistics.Server.IntegrationTests/SqlServerHardeningIntegrationTests.cs`

1. `NumberSequenceService_SqlServerConcurrentCalls_GenerateUniqueSequentialNumbers`: Executes 12 parallel `NumberSequenceService.GetNextNumberAsync("WAYBILL")` calls against disposable SQL Server LocalDB (`(localdb)\mssqllocaldb`) with EF Core migrations applied, asserting 12 distinct sequential codes and `NextNumber == 13`.
2. `OutboxProcessorService_FailingHandlerWithDirtyTracker_IsolatesFailureAndCompletesBatch`: Processes an `OutboxMessages` batch where message 1 stages an invalid `Notification` onto the shared `ApplicationDbContext` and throws before message 2 succeeds, asserting message 1 updates cleanly (`RetryCount = 1`, `Status = "Pending"`, `NextRetryAt != null`) while message 2 finishes as `Published`.

## Coverage Gaps Still Remaining

- No automated UI automation / Playwright / WinForms test harness exercises live rendered WinForms grids and modal dialogs interactively.
- External carrier integration tests use in-process simulated carrier adapters rather than sandbox HTTP endpoints of GHN, GHTK, or ViettelPost.
