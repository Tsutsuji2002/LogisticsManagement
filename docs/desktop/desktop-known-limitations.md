# Desktop Client Known Limitations & Verified Backend Constraints

Every behavior below reflects verified contracts of the existing `Logistics.Server.API` backend. The desktop client intentionally does NOT invent fake endpoints or client-side workarounds.

1. **Report Export Supports CSV Only**
   - `GET /api/v1/reports/export/{reportType}?format=csv` is the only supported format on the server (`pdf` / `xlsx` return `400 Bad Request`).
   - Accordingly, the Reports Explorer exposes **Xuất CSV** only.

2. **No Binary Attachment Upload / Download Endpoint**
   - Proof of Delivery (`CreateProofOfDeliveryRequest`) and Customer Contracts (`CreateCustomerContractRequest`) accept reference metadata / IDs only; the backend exposes no `IFormFile` upload or file download controller endpoint for attachments.

3. **Catalog Shipping Services Are Read-Only**
   - `GET /api/v1/catalog/services` is implemented; create/update/delete endpoints for `ShippingServices` do not exist in `CatalogController`, so the Services tab is strictly read-only.

4. **Master Geographic / Currency Reference & Audit Log Views Not Exposed by API**
   - No API controllers exist for `Currencies`, `Countries`, `Provinces/Districts/Wards`, or `AuditLogs` (despite `audit.logs.read` permission constant).
   - Screens needing currency selection default safely to `CurrencyId = 1` (or accept numeric ID) with explicit clear labeling.

5. **Branch Context Query/DTO Forwarding**
   - `CurrentUserService` on the server enforces access from JWT `branch_id` / `branch_access` claims and individual endpoints inspect `?branchId=` / body properties rather than a global `X-Branch-Id` middleware header.
   - To be both forward-compatible and immediately accurate, WinForms sends **both** `X-Branch-Id` headers and explicit `branchId` query/body fields.

6. **Near Real-Time Notification Polling Instead of SignalR**
   - No SignalR notification hub exists on `Logistics.Server.API`; `MainForm` polls `/api/v1/notifications/unread-count` politely every 60 seconds and immediately refreshes whenever notifications are read/sent in `NotificationsWorkspaceControl`.

7. **Authenticated Password Reset Only**
   - Unauthenticated "Forgot Password" email recovery does not exist on `/api/v1/auth`. Users change passwords while signed in (`POST /api/v1/auth/change-password`) or ask an administrator with `identity.users.reset-password` to reset via `Organization -> Tài khoản & Phân quyền`.

8. **Scheduled Report Asynchronous File Storage Execution**
   - Triggering a scheduled report via `POST /api/v1/scheduled-reports/{id}/run` executes the backend scheduling handler and returns `ScheduledReportRunResultDto` (`IsReportGenerationSupported` and status message verbatim).
