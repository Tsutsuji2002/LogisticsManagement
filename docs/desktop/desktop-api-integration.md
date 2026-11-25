# Desktop API Integration Specification

## 1. HTTP Pipeline ([ApiClient.cs](file:///c:/Projects/LogisticsManagement/src/Client/Logistics.Client.Infrastructure/Http/ApiClient.cs))

All HTTP calls route through a single `HttpClient` pipeline configured in [`DependencyInjection.cs`](file:///c:/Projects/LogisticsManagement/src/Client/Logistics.Client.Infrastructure/DependencyInjection.cs):

- **Anonymous Bare Auth Client (`"LogisticsAuth"`)**: Used exclusively by `ClientAuthenticationService` for `POST /api/v1/auth/login`, `POST /api/v1/auth/refresh`, `POST /api/v1/auth/logout`, and `POST /api/v1/auth/logout-all` to prevent delegating-handler recursion.
- **Authenticated `IApiClient`**: Wraps responses in `ApiResponse<T>` unwrapping, extracts `ValidationErrors`, `Errors`, `ErrorCode`, and `TraceId` into `ApiException`, maps network/timeout failures cleanly, and provides `GetFileAsync(...)` for CSV export downloads.
- **Auth & Branch Handler ([AuthenticationDelegatingHandler.cs](file:///c:/Projects/LogisticsManagement/src/Client/Logistics.Client.Infrastructure/Http/AuthenticationDelegatingHandler.cs))**:
  - Injects `Authorization: Bearer <accessToken>` on protected endpoints.
  - Injects `X-Branch-Id: <ActiveBranchId>` whenever an active branch is selected (in addition to passing explicit `?branchId=` query/body fields where backend endpoints read them).
  - On `401 Unauthorized`, attempts a single-flight forced token refresh via `GetValidAccessTokenAsync(forceRefresh: true)` and retries the cloned request at most once before calling `HandleSessionInvalidatedAsync(...)`.

## 2. Typed Domain Clients

15 interfaces in [`ITypedApiClients.cs`](file:///c:/Projects/LogisticsManagement/src/Client/Logistics.Client.Application/Interfaces/ITypedApiClients.cs) map directly to actual verified controller routes in `Logistics.Server.API`:

- `IHealthApiClient` -> `/api/health`
- `IAuthSessionApiClient` -> `/api/v1/auth/me`, `/change-password`, `/sessions`
- `IOrganizationApiClient` -> `/api/v1/company`, `/branches`, `/departments`, `/employees`
- `IIdentityAdminApiClient` -> `/api/v1/admin/users`, `/api/v1/admin/roles`, `/api/v1/admin/permissions`
- `ICrmApiClient` -> `/api/v1/customers/**` (contacts, addresses, contracts, pricing-rules)
- `ICatalogApiClient` -> `/api/v1/catalog/{categories,products,units,services}`
- `ISalesApiClient` -> `/api/v1/orders/**`
- `IShippingApiClient` -> `/api/v1/shipments/**`, `/pickup-requests/**`, `/return-requests/**`
- `ICarrierApiClient` -> `/api/v1/carriers/**` (configurations, operations, logs, webhooks, sync-jobs)
- `IWarehouseApiClient` -> `/api/v1/warehouses/**`, `/parcel-hub/**`, `/warehouse-dispatches/**`
- `IInventoryApiClient` -> `/api/v1/inventory/**`, `/warehouse-receipts/**`
- `IFinanceApiClient` -> `/api/v1/finance/**`, `/shipping-charges/**`, `/invoices/**`, `/payments/**`, `/financial-ledger/**`
- `ICodApiClient` -> `/api/v1/cod/collections/**`, `/remittances/**`, `/reconciliation/**`
- `INotificationApiClient` -> `/api/v1/notifications/**`, `/notification-templates/**`
- `IReportingApiClient` -> `/api/v1/dashboards/**`, `/api/v1/reports/**`, `/api/v1/reports/export/{type}?format=csv`, `/scheduled-reports/**`, `/background-jobs/**`, `/outbox-messages/**`
