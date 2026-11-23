# Shipment Management — Business Rules

## 1. Branch-Level Authorization

- **Operational Branch Boundary**: Users without `SuperAdmin` or `Admin` roles can only view, update, and manage shipments whose `OriginBranchId` or `DestinationBranchId` matches one of the branches in their `AccessibleBranchIds` (assigned via `UserBranchAccess`).
- **Branch Scope Validation**: When creating shipments from orders, the operator's accessible branches must include either the source branch or destination branch.
- **Assignment Scope**: Employees assigned as couriers or drivers must have active status and belong to an operating branch compatible with the shipment route.

---

## 2. Order-to-Shipment Eligibility

1. **Order Exists**: Target `OrderId` must exist in `sales.Orders`.
2. **Order Status**: Order must not be `Cancelled` or `Draft`. It must be at least `Confirmed` or `Processing`.
3. **No Duplicate Package Assignment**: Order packages already assigned to an active (non-cancelled) shipment cannot be assigned to another shipment.
4. **Order Status Side Effect**: When the first shipment is created from a `Confirmed` order, the order's status is automatically transitioned to `Processing`.

---

## 3. Package Management Rules

1. **Editable States**: Packages can only be added, updated, or deleted while the shipment is in `Draft` or `ReadyForPickup`.
2. **Aggregate Recalculation**:
   - `TotalPackages` is recalculated on every package addition or removal.
   - `ActualWeightKg` is incremented/decremented by package weight.
   - `ChargeableWeightKg` is computed as $\max(\text{ActualWeight}, \text{VolumetricWeight})$.
   - Volumetric weight formula:
     $$\text{VolumetricWeightKg} = \frac{\text{LengthCm} \times \text{WidthCm} \times \text{HeightCm}}{5000}$$

---

## 4. Delivery Attempt Rules

1. **Operational Requirement**: Delivery attempts can only be recorded when the shipment is in `OutForDelivery` status.
2. **Failure Codes**: If an attempt status is `Failed`, `FailureReasonCode` is mandatory and must be one of:
   - `RecipientUnavailable`
   - `WrongAddress`
   - `RefusedDelivery`
   - `CustomerUnreachable`
   - `CodDispute`
3. **Status Side Effects**:
   - If attempt is `Delivered`, shipment status automatically transitions to `Delivered`, and `ActualDeliveryDate` is recorded.
   - If attempt is `Failed`, shipment status automatically transitions to `DeliveryFailed`.

---

## 5. Proof of Delivery (POD) Rules

1. **Linkage**: POD must reference an existing delivery attempt on the shipment.
2. **Uniqueness**: Only 1 Proof of Delivery record is permitted per shipment.
3. **Recipient Validation**: Recipient relation must be one of `Self`, `FamilyMember`, `Receptionist`, `SecurityGuard`, or `Colleague`.
4. **Attachment Verification**: If `SignatureAttachmentId` or `PhotoAttachmentId` are specified, the corresponding `FileAttachment` records must exist in the database.
5. **State Transition**: Recording a POD ensures the shipment is in `Delivered` status.

---

## 6. Return Request Rules

1. **Eligibility**: Return requests can only be initiated for shipments in `Delivered`, `DeliveryFailed`, or `Cancelled` status.
2. **Single Active Return**: Only one active return request (not in `Rejected` or `ReturnedToSender` status) is permitted per shipment.
3. **Approval Transition**: Approving a return request sets the return status to `Approved` and transitions the parent shipment to `Returning`.
4. **Completion Transition**: Completing a return request transitions the return status to `ReturnedToSender` and the parent shipment to `Returned`.
