# Notification Delivery & Email Provider Specification

## 1. Delivery Architecture (`INotificationDeliveryService`)

| Channel | Initial Status | Delivery Mechanism | Attempt Record |
| :--- | :--- | :--- | :--- |
| **`InApp`** | `Delivered` | Persisted directly inside `notification.Notifications` upon creation. | 1 initial `NotificationDelivery` (`Status = "Success"`, `GatewayResponse = "InAppPersisted"`). |
| **`Email`** | `Pending` | Processed asynchronously by `NotificationDeliveryWorkerJob` via `IEmailProvider`. | 1 `NotificationDelivery` row appended per attempt (`AttemptNumber = 1..5`). |
| **`SMS` / `Webhook`** | `Pending` | Explicitly marked `Failed` (no outbound SMS/Webhook gateway integrated). | 1 `NotificationDelivery` row noting unsupported channel. |

---

## 2. Email Provider Integration (`ConfigurableEmailProvider : IEmailProvider`)

Configuration is bound from the `Email` configuration section (with sensitive credentials supplied via User Secrets / environment variables, never hardcoded in `appsettings.json`):

1. **`SendGrid`**:
   - Uses named `HttpClient("SendGrid")` from `IHttpClientFactory` with strict timeout (`Email:TimeoutSeconds`, clamped `3-120s`).
   - Sends `Authorization: Bearer <SendGrid:ApiKey>` and `custom_args.idempotency_key` (`notif-{PublicId}-att-{AttemptNumber}`).
   - Reads `X-Message-Id` header on `2xx` responses and `Retry-After` header on `429` / `5xx` responses.
2. **`Smtp`**:
   - Uses `System.Net.Mail.SmtpClient` with `EnableSsl`, port, timeout, and optional credentials, passing `CancellationToken` to `SendMailAsync`.
3. **`None` (Default Unconfigured)**:
   - Returns `EmailFailureClassification.ConfigurationMissing` (`Succeeded = false`) — **never fakes delivery success**.

---

## 3. Failure Classification, Retries & Redaction

- **Failure Classification**:
  - `Transient`: HTTP `429`, HTTP `5xx`, `HttpRequestException`, timeout, or transient SMTP codes (`MailboxBusy`, `InsufficientStorage`, `ServiceNotAvailable`, `TransactionFailed`). Scheduled for retry (`Status = "Queued"`).
  - `Permanent`: HTTP `400/401/403/404`, malformed email address (`FormatException`), or non-retryable SMTP status codes. Immediately marked `Status = "Failed"` so invalid addresses are never retried indefinitely.
  - `ConfigurationMissing`: Immediately marked `Status = "Failed"`.
- **Bounded Exponential Backoff + Retry-After**:
  - Maximum `5` delivery attempts (`NotificationDeliveryService.MaxDeliveryAttempts`).
  - Honors provider `Retry-After` seconds when supplied; otherwise computes `2^attempt * 15` seconds before the next attempt is eligible.
- **No Long DB Locks During Provider I/O**:
  - `NotificationDeliveryService` reads eligible notifications `AsNoTracking()`, performs network I/O outside any open database transaction, and persists the delivery outcome afterward.
- **Idempotency & Duplicate Prevention**:
  - Refuses to dispatch if `Notification.Status` is already `Sent` / `Delivered` or any `NotificationDelivery` already has `Status == "Success"`.
- **Sensitive Data Redaction**:
  - Logs mask email local parts (`a***@company.com`), and gateway error strings redact `Bearer ...` and `SG.*` API keys before persistence or logging.
