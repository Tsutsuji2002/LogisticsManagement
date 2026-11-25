# Notification Templates Specification

## 1. Template Lifecycle & Governance

Templates are persisted in `notification.NotificationTemplates` and managed through `INotificationTemplateService`:
- **Unique Code**: Enforced uppercase alphanumeric + underscore (`^[A-Z0-9_]+$`), e.g., `SHIPMENT_DISPATCHED`, `INVOICE_ISSUED`.
- **Channels**: `InApp`, `Email`, `SMS`, `Webhook`.
- **Activation Toggle**: Templates can be activated or deactivated (`PATCH /api/v1/notification-templates/{id}/status`). Attempting to dispatch a notification using an inactive template fails cleanly with `409 Conflict`.
- **Audit Logging**: Creating, updating, or toggling a template logs an immutable record to `audit.AuditLogs`.

---

## 2. Safe Template Rendering (`INotificationTemplateRenderer`)

The renderer enforces strict security and correctness guarantees:
1. **No Dynamic Compilation or Arbitrary Code**: Uses compiled regular expressions for `{{Placeholder}}` and `{Placeholder}` token substitution only. No Razor, script engines, reflection, or arbitrary expression evaluation (`${...}`, `@(...)`, `<script>`, `javascript:`) is permitted.
2. **Approved Placeholder Whitelist**: If `ParametersJson` defines an allowed placeholder list, any placeholder in `SubjectTemplate` or `BodyTemplate` not present in that whitelist fails validation with `400 Bad Request`.
3. **Required Parameter Validation**: Missing placeholder parameters throw `ValidationException` listing the exact missing keys.
4. **HTML Escaping**: When rendering Email (`htmlEscapeBody: true`) or previewing with HTML escaping enabled, all parameter values are encoded with `System.Net.WebUtility.HtmlEncode` to prevent XSS or HTML injection inside email clients.
5. **Snapshot Preservation**: Because `NotificationTemplates` lacks a version table in the current schema, `NotificationService` renders `Title` and `Content` at creation time and stores the rendered output directly on `notification.Notifications`. Editing a template later never alters historical notifications.
