# Report Export Specification

## 1. Supported Formats & Honest Rejection

- **Supported**: `csv` (`text/csv; charset=utf-8` with UTF-8 BOM `0xEF, 0xBB, 0xBF` for clean Excel UTF-8 rendering).
- **Unsupported**: `pdf`, `xlsx`, and `excel` are rejected with HTTP `400 Bad Request` (`ValidationException` on `Format`), because no third-party PDF or Excel rendering dependency is installed in the solution. Synthetic or placeholder files are never generated.

## 2. Export Endpoint & Dual Authorization

- Endpoint: `GET /api/v1/reports/export/{reportType}?format=csv`
- Requires **both**:
  1. `reports.export`, AND
  2. The domain-specific read permission corresponding to `{reportType}` (e.g., `reports.finance.read` for `invoices`, `payments`, `receivables`, `shipping-charges`; `reports.cod.read` for `cod-collections`, `cod-remittances`, `cod-reconciliation`).
- Enforces branch access scoping and caps exports at `ReportingQueryHelpers.MaxExportRows` (`10,000` rows per export).

## 3. CSV Security & Audit Controls

1. **Formula Injection Neutralization**: Any cell value beginning with `=`, `+`, `-`, `@`, `\t`, or `\r` is prefixed with a single quote (`'`) before RFC 4180 CSV quoting (`ReportingQueryHelpers.BuildCsv`).
2. **Filename Sanitization**: Exported download names strip directory traversal and invalid filesystem characters (`ReportingQueryHelpers.SanitizeFileName`).
3. **Audit Trail**: Every export writes an `Export` record via `IAuditService.RecordAsync` (`SchemaName = "report"`, `TableName = reportType`, capturing format, row count, branch filter, date window, and timezone).
