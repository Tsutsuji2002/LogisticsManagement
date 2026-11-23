# Carrier Security & Credential Management

## 1. Security Principles

1. **No Plaintext Credential Exposure**: Carrier API keys, client secrets, passwords, and webhook secrets are stored encrypted and are NEVER returned over client-facing API responses.
2. **Environment Isolation**: Carrier configurations are segregated by `Environment` (`Sandbox` vs `Production`), allowing branches and testing environments to test against vendor sandbox APIs without risking production freight bookings.
3. **Role-Based Access Control**:
   - `carrier.carriers.read`: View carrier metadata, service catalog, tracking mappings, and monitoring logs.
   - `carrier.carriers.manage`: Configure credentials, create services, map statuses, and book/cancel carrier shipments.
4. **Webhook Security**: Unauthenticated public endpoints do not execute database alterations directly; they record the raw request and payload into `CarrierWebhookLog` with remote IP capture.

## 2. Configuration Schema: `CarrierConfiguration`

| Property | Protection | DTO Representation |
|:---|:---|:---|
| `ApiKey` | Secret / Encrypted | `HasApiKey: bool` |
| `ApiSecret` | Secret / Encrypted | `HasApiSecret: bool` |
| `ClientId` | Public / Identifier | `HasClientId: bool` |
| `Username` | Identifier | `HasUsername: bool` |
| `EncryptedPassword` | Secret / Encrypted | `HasPassword: bool` |
| `WebhookSecret` | Secret / Encrypted | `HasWebhookSecret: bool` |
| `SettingsJson` | Non-sensitive JSON settings | `SettingsJson: string?` |

## 3. Masked Response Pattern
When clients fetch configurations via `GET /api/v1/carriers/{id}/configurations`, the returned DTO returns boolean presence indicators:
```json
{
  "id": 1,
  "carrierId": 1,
  "carrierCode": "GHN",
  "environment": "Production",
  "hasApiKey": true,
  "hasApiSecret": false,
  "hasClientId": true,
  "hasUsername": true,
  "hasPassword": true,
  "hasWebhookSecret": true,
  "settingsJson": "{\"shop_id\": 123456}",
  "isActive": true
}
```
Client users can verify whether credentials are configured without the API exposing secrets.
