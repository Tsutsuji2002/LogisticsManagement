# Background Processing & Scheduled Reports REST API Reference

## 1. Background Jobs & Outbox Operations (`/api/v1/background-jobs`)

| Method & Route | Permission | Description |
| :--- | :--- | :--- |
| `GET /api/v1/background-jobs` | `system.background-jobs.read` | Paginated job logs filtered by `jobName`, `status`, `triggerType`, `fromDate`, `toDate` (raw payloads hidden). |
| `GET /api/v1/background-jobs/failed` | `system.background-jobs.read` | Paginated list of failed jobs. |
| `GET /api/v1/background-jobs/{id}` | `system.background-jobs.read` | Get job telemetry details and sanitized error summary. |
| `POST /api/v1/background-jobs/{id}/retry` | `system.background-jobs.retry` | Queue a `Failed` or `Cancelled` job for immediate retry (audited). |
| `POST /api/v1/background-jobs/{id}/cancel` | `system.background-jobs.manage` | Safely cancel a `Pending` job (`409` if `Running` or `Success`). |
| `GET /api/v1/background-jobs/health` | `system.background-processing.read` | Queue backlog & health summary across outbox, jobs, notifications, and due reports. |
| `GET /api/v1/background-jobs/outbox` | `system.background-processing.read` | Inspect outbox statuses (`Pending`, `InProgress`, `Published`, `Failed`) without exposing raw payloads. |
| `POST /api/v1/background-jobs/outbox/{id}/retry` | `system.background-processing.manage` | Reset a failed/stuck outbox message to `Pending` for immediate dispatch. |

---

## 2. Scheduled Reports (`/api/v1/scheduled-reports`)

| Method & Route | Permission | Description |
| :--- | :--- | :--- |
| `GET /api/v1/scheduled-reports/definitions` | `notification.reports.read` | List available report definitions by `category` and `isActive`. |
| `GET /api/v1/scheduled-reports/definitions/{id}` | `notification.reports.read` | Get report definition details. |
| `GET /api/v1/scheduled-reports` | `notification.reports.read` | List branch-filtered scheduled report configurations. |
| `GET /api/v1/scheduled-reports/{id}` | `notification.reports.read` | Get scheduled report details (enforces branch access). |
| `POST /api/v1/scheduled-reports` | `notification.reports.create` | Create scheduled report with validated 5-field cron, timezone, and email recipients. |
| `PUT /api/v1/scheduled-reports/{id}` | `notification.reports.update` | Update schedule cron, timezone, output format, branch, or recipients. |
| `PATCH /api/v1/scheduled-reports/{id}/status` | `notification.reports.manage` | Activate or deactivate a scheduled report. |
| `POST /api/v1/scheduled-reports/{id}/run` | `notification.reports.manage` | Manually trigger report execution with overlap prevention (`409` if running). |
| `GET /api/v1/scheduled-reports/{id}/history` | `notification.reports.read` | Retrieve execution history for the specified scheduled report. |
