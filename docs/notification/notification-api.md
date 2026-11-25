# Notification & Template REST API Reference

## 1. Notifications (`/api/v1/notifications`)

| Method & Route | Permission | Description |
| :--- | :--- | :--- |
| `GET /api/v1/notifications` | `notification.notifications.read-own` | List current user's notifications with filtering (`isRead`, `notificationType`, `channel`, `status`, `fromDate`, `toDate`, `includeExpired`, `sortBy`, `sortDescending`, `page`, `pageSize`). |
| `GET /api/v1/notifications/all` | `notification.notifications.manage` | Administrative query across recipients and accessible branches (`recipientUserId`, `branchId`, filters). |
| `GET /api/v1/notifications/unread-count` | `notification.notifications.read-own` | Returns `{ "unreadCount": 5 }` for current user. |
| `GET /api/v1/notifications/{id}` | `notification.notifications.read-own` | Get notification details and delivery attempts (`403` if owned by another user without `manage` permission). |
| `POST /api/v1/notifications` | `notification.notifications.manage` | Create/dispatch notification to single user, multiple users, branch, system-wide broadcast, or email address. |
| `PATCH /api/v1/notifications/{id}/read` | `notification.notifications.read-own` | Mark notification as read (`IsRead = true`, `ReadAt = UtcNow`). |
| `POST /api/v1/notifications/mark-all-read` | `notification.notifications.read-own` | Mark all unread notifications for the authenticated user as read. |

---

## 2. Notification Templates (`/api/v1/notification-templates`)

| Method & Route | Permission | Description |
| :--- | :--- | :--- |
| `GET /api/v1/notification-templates` | `notification.templates.read` | Paginated search by `search`, `channel`, `isActive`. |
| `GET /api/v1/notification-templates/{identifier}` | `notification.templates.read` | Get template by numeric ID or uppercase `Code`. |
| `POST /api/v1/notification-templates` | `notification.templates.manage` | Create new template with approved parameter whitelist (`201 Created`). |
| `PUT /api/v1/notification-templates/{id}` | `notification.templates.manage` | Update template subject, body, channel, and whitelist. |
| `PATCH /api/v1/notification-templates/{id}/status` | `notification.templates.manage` | Activate or deactivate template (`{ "isActive": true/false }`). |
| `POST /api/v1/notification-templates/{id}/preview` | `notification.templates.read` | Render preview safely using supplied test parameters and optional HTML escaping. |
