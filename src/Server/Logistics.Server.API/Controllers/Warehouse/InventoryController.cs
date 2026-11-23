using FluentValidation;
using Logistics.Server.API.Authorization;
using Logistics.Server.Application.Common.Exceptions;
using Logistics.Server.Application.Common.Security;
using Logistics.Server.Application.Modules.Warehouse.DTOs;
using Logistics.Server.Application.Modules.Warehouse.Services;
using Logistics.Shared.Contracts;
using Logistics.Shared.Contracts.Pagination;
using Microsoft.AspNetCore.Mvc;
using ValidationException = Logistics.Server.Application.Common.Exceptions.ValidationException;

namespace Logistics.Server.API.Controllers.Warehouse;

/// <summary>
/// Physical inventory operations: items, balances, transactions, receipts, adjustments, transfers, reservations, lots, and serial numbers.
/// </summary>
[Route("api/v1/inventory")]
public class InventoryController : BaseApiController
{
    private readonly IInventoryService _inventoryService;
    private readonly IValidator<CreateInventoryItemRequest> _createItemValidator;
    private readonly IValidator<CreateWarehouseReceiptRequest> _createReceiptValidator;
    private readonly IValidator<CreateStockAdjustmentRequest> _createAdjustmentValidator;
    private readonly IValidator<CreateStockTransferRequest> _createTransferValidator;
    private readonly IValidator<CreateInventoryReservationRequest> _createReservationValidator;
    private readonly IValidator<CreateInventoryLotRequest> _createLotValidator;
    private readonly IValidator<CreateInventorySerialNumberRequest> _createSerialValidator;

    public InventoryController(
        IInventoryService inventoryService,
        IValidator<CreateInventoryItemRequest> createItemValidator,
        IValidator<CreateWarehouseReceiptRequest> createReceiptValidator,
        IValidator<CreateStockAdjustmentRequest> createAdjustmentValidator,
        IValidator<CreateStockTransferRequest> createTransferValidator,
        IValidator<CreateInventoryReservationRequest> createReservationValidator,
        IValidator<CreateInventoryLotRequest> createLotValidator,
        IValidator<CreateInventorySerialNumberRequest> createSerialValidator)
    {
        _inventoryService = inventoryService;
        _createItemValidator = createItemValidator;
        _createReceiptValidator = createReceiptValidator;
        _createAdjustmentValidator = createAdjustmentValidator;
        _createTransferValidator = createTransferValidator;
        _createReservationValidator = createReservationValidator;
        _createLotValidator = createLotValidator;
        _createSerialValidator = createSerialValidator;
    }

    // ───────────── Inventory Items ─────────────

    [HttpGet("items")]
    [HasPermission(AppPermissions.Inventory.InventoryRead)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<InventoryItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetItems([FromQuery] string? search, [FromQuery] long? warehouseId, [FromQuery] bool? isActive, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await _inventoryService.GetInventoryItemsAsync(search, warehouseId, isActive, new PagedRequest { Page = page, PageSize = pageSize }, cancellationToken);
        return Ok(ApiResponse<PagedResponse<InventoryItemDto>>.Ok(result.ToPagedResponse()));
    }

    [HttpGet("items/{id:long}")]
    [HasPermission(AppPermissions.Inventory.InventoryRead)]
    [ProducesResponseType(typeof(ApiResponse<InventoryItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetItem(long id, CancellationToken cancellationToken)
    {
        var item = await _inventoryService.GetInventoryItemByIdAsync(id, cancellationToken);
        if (item == null) throw new NotFoundException("InventoryItem", id);
        return Ok(ApiResponse<InventoryItemDto>.Ok(item));
    }

    [HttpPost("items")]
    [HasPermission(AppPermissions.Inventory.InventoryManage)]
    [ProducesResponseType(typeof(ApiResponse<InventoryItemDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateItem([FromBody] CreateInventoryItemRequest request, CancellationToken cancellationToken)
    {
        var val = await _createItemValidator.ValidateAsync(request, cancellationToken);
        if (!val.IsValid) throw new ValidationException(val.Errors.GroupBy(e => e.PropertyName).ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));

        var item = await _inventoryService.CreateInventoryItemAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetItem), new { id = item.Id }, ApiResponse<InventoryItemDto>.Ok(item));
    }

    [HttpPut("items/{id:long}")]
    [HasPermission(AppPermissions.Inventory.InventoryManage)]
    [ProducesResponseType(typeof(ApiResponse<InventoryItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateItem(long id, [FromBody] UpdateInventoryItemRequest request, CancellationToken cancellationToken)
    {
        var item = await _inventoryService.UpdateInventoryItemAsync(id, request, cancellationToken);
        return Ok(ApiResponse<InventoryItemDto>.Ok(item));
    }

    // ───────────── Balances ─────────────

    [HttpGet("balances")]
    [HasPermission(AppPermissions.Inventory.InventoryRead)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<InventoryBalanceDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBalances([FromQuery] long? warehouseId, [FromQuery] long? locationId, [FromQuery] long? productId, [FromQuery] long? lotId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await _inventoryService.GetBalancesAsync(warehouseId, locationId, productId, lotId, new PagedRequest { Page = page, PageSize = pageSize }, cancellationToken);
        return Ok(ApiResponse<PagedResponse<InventoryBalanceDto>>.Ok(result.ToPagedResponse()));
    }

    [HttpGet("balances/{id:long}")]
    [HasPermission(AppPermissions.Inventory.InventoryRead)]
    [ProducesResponseType(typeof(ApiResponse<InventoryBalanceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBalance(long id, CancellationToken cancellationToken)
    {
        var balance = await _inventoryService.GetBalanceByIdAsync(id, cancellationToken);
        if (balance == null) throw new NotFoundException("InventoryBalance", id);
        return Ok(ApiResponse<InventoryBalanceDto>.Ok(balance));
    }

    // ───────────── Transactions ─────────────

    [HttpGet("transactions")]
    [HasPermission(AppPermissions.Inventory.InventoryRead)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<InventoryTransactionDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTransactions([FromQuery] long? warehouseId, [FromQuery] string? transactionType, [FromQuery] string? referenceType, [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await _inventoryService.GetTransactionsAsync(warehouseId, transactionType, referenceType, from, to, new PagedRequest { Page = page, PageSize = pageSize }, cancellationToken);
        return Ok(ApiResponse<PagedResponse<InventoryTransactionDto>>.Ok(result.ToPagedResponse()));
    }

    [HttpGet("transactions/{id:long}")]
    [HasPermission(AppPermissions.Inventory.InventoryRead)]
    [ProducesResponseType(typeof(ApiResponse<InventoryTransactionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTransaction(long id, CancellationToken cancellationToken)
    {
        var txn = await _inventoryService.GetTransactionByIdAsync(id, cancellationToken);
        if (txn == null) throw new NotFoundException("InventoryTransaction", id);
        return Ok(ApiResponse<InventoryTransactionDto>.Ok(txn));
    }

    // ───────────── Receipts ─────────────

    [HttpGet("receipts")]
    [HasPermission(AppPermissions.Inventory.InventoryRead)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<WarehouseReceiptDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReceipts([FromQuery] long? warehouseId, [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await _inventoryService.GetReceiptsAsync(warehouseId, status, new PagedRequest { Page = page, PageSize = pageSize }, cancellationToken);
        return Ok(ApiResponse<PagedResponse<WarehouseReceiptDto>>.Ok(result.ToPagedResponse()));
    }

    [HttpGet("receipts/{id:long}")]
    [HasPermission(AppPermissions.Inventory.InventoryRead)]
    [ProducesResponseType(typeof(ApiResponse<WarehouseReceiptDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReceipt(long id, CancellationToken cancellationToken)
    {
        var receipt = await _inventoryService.GetReceiptByIdAsync(id, cancellationToken);
        if (receipt == null) throw new NotFoundException("WarehouseReceipt", id);
        return Ok(ApiResponse<WarehouseReceiptDto>.Ok(receipt));
    }

    [HttpPost("receipts")]
    [HasPermission(AppPermissions.Warehouse.ReceiptsManage)]
    [ProducesResponseType(typeof(ApiResponse<WarehouseReceiptDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateReceipt([FromBody] CreateWarehouseReceiptRequest request, CancellationToken cancellationToken)
    {
        var val = await _createReceiptValidator.ValidateAsync(request, cancellationToken);
        if (!val.IsValid) throw new ValidationException(val.Errors.GroupBy(e => e.PropertyName).ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));

        var receipt = await _inventoryService.CreateReceiptAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetReceipt), new { id = receipt.Id }, ApiResponse<WarehouseReceiptDto>.Ok(receipt));
    }

    [HttpPost("receipts/{id:long}/confirm")]
    [HasPermission(AppPermissions.Warehouse.ReceiptsManage)]
    [ProducesResponseType(typeof(ApiResponse<WarehouseReceiptDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ConfirmReceipt(long id, [FromBody] ConfirmReceiptRequest request, CancellationToken cancellationToken)
    {
        var receipt = await _inventoryService.ConfirmReceiptAsync(id, request, cancellationToken);
        return Ok(ApiResponse<WarehouseReceiptDto>.Ok(receipt));
    }

    // ───────────── Adjustments ─────────────

    [HttpGet("adjustments")]
    [HasPermission(AppPermissions.Inventory.InventoryRead)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<StockAdjustmentDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAdjustments([FromQuery] long? warehouseId, [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await _inventoryService.GetAdjustmentsAsync(warehouseId, status, new PagedRequest { Page = page, PageSize = pageSize }, cancellationToken);
        return Ok(ApiResponse<PagedResponse<StockAdjustmentDto>>.Ok(result.ToPagedResponse()));
    }

    [HttpGet("adjustments/{id:long}")]
    [HasPermission(AppPermissions.Inventory.InventoryRead)]
    [ProducesResponseType(typeof(ApiResponse<StockAdjustmentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAdjustment(long id, CancellationToken cancellationToken)
    {
        var adj = await _inventoryService.GetAdjustmentByIdAsync(id, cancellationToken);
        if (adj == null) throw new NotFoundException("StockAdjustment", id);
        return Ok(ApiResponse<StockAdjustmentDto>.Ok(adj));
    }

    [HttpPost("adjustments")]
    [HasPermission(AppPermissions.Inventory.AdjustmentsManage)]
    [ProducesResponseType(typeof(ApiResponse<StockAdjustmentDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateAdjustment([FromBody] CreateStockAdjustmentRequest request, CancellationToken cancellationToken)
    {
        var val = await _createAdjustmentValidator.ValidateAsync(request, cancellationToken);
        if (!val.IsValid) throw new ValidationException(val.Errors.GroupBy(e => e.PropertyName).ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));

        var adj = await _inventoryService.CreateAdjustmentAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetAdjustment), new { id = adj.Id }, ApiResponse<StockAdjustmentDto>.Ok(adj));
    }

    [HttpPost("adjustments/{id:long}/approve")]
    [HasPermission(AppPermissions.Inventory.AdjustmentsManage)]
    [ProducesResponseType(typeof(ApiResponse<StockAdjustmentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ApproveAdjustment(long id, [FromQuery] long employeeId, CancellationToken cancellationToken)
    {
        var adj = await _inventoryService.ApproveAdjustmentAsync(id, employeeId, cancellationToken);
        return Ok(ApiResponse<StockAdjustmentDto>.Ok(adj));
    }

    // ───────────── Transfers ─────────────

    [HttpGet("transfers")]
    [HasPermission(AppPermissions.Inventory.InventoryRead)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<StockTransferDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTransfers([FromQuery] long? sourceWarehouseId, [FromQuery] long? destinationWarehouseId, [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await _inventoryService.GetTransfersAsync(sourceWarehouseId, destinationWarehouseId, status, new PagedRequest { Page = page, PageSize = pageSize }, cancellationToken);
        return Ok(ApiResponse<PagedResponse<StockTransferDto>>.Ok(result.ToPagedResponse()));
    }

    [HttpGet("transfers/{id:long}")]
    [HasPermission(AppPermissions.Inventory.InventoryRead)]
    [ProducesResponseType(typeof(ApiResponse<StockTransferDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTransfer(long id, CancellationToken cancellationToken)
    {
        var transfer = await _inventoryService.GetTransferByIdAsync(id, cancellationToken);
        if (transfer == null) throw new NotFoundException("StockTransfer", id);
        return Ok(ApiResponse<StockTransferDto>.Ok(transfer));
    }

    [HttpPost("transfers")]
    [HasPermission(AppPermissions.Inventory.TransfersManage)]
    [ProducesResponseType(typeof(ApiResponse<StockTransferDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateTransfer([FromBody] CreateStockTransferRequest request, CancellationToken cancellationToken)
    {
        var val = await _createTransferValidator.ValidateAsync(request, cancellationToken);
        if (!val.IsValid) throw new ValidationException(val.Errors.GroupBy(e => e.PropertyName).ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));

        var transfer = await _inventoryService.CreateTransferAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetTransfer), new { id = transfer.Id }, ApiResponse<StockTransferDto>.Ok(transfer));
    }

    [HttpPost("transfers/{id:long}/confirm")]
    [HasPermission(AppPermissions.Inventory.TransfersManage)]
    [ProducesResponseType(typeof(ApiResponse<StockTransferDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ConfirmTransfer(long id, [FromQuery] long employeeId, CancellationToken cancellationToken)
    {
        var transfer = await _inventoryService.ConfirmTransferAsync(id, employeeId, cancellationToken);
        return Ok(ApiResponse<StockTransferDto>.Ok(transfer));
    }

    [HttpPost("transfers/{id:long}/receive")]
    [HasPermission(AppPermissions.Inventory.TransfersManage)]
    [ProducesResponseType(typeof(ApiResponse<StockTransferDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ReceiveTransfer(long id, [FromBody] ReceiveStockTransferRequest request, [FromQuery] long employeeId, CancellationToken cancellationToken)
    {
        var transfer = await _inventoryService.ReceiveTransferAsync(id, request, employeeId, cancellationToken);
        return Ok(ApiResponse<StockTransferDto>.Ok(transfer));
    }

    // ───────────── Reservations ─────────────

    [HttpGet("reservations")]
    [HasPermission(AppPermissions.Inventory.InventoryRead)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<InventoryReservationDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReservations([FromQuery] long? warehouseId, [FromQuery] long? productId, [FromQuery] long? orderId, [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await _inventoryService.GetReservationsAsync(warehouseId, productId, orderId, status, new PagedRequest { Page = page, PageSize = pageSize }, cancellationToken);
        return Ok(ApiResponse<PagedResponse<InventoryReservationDto>>.Ok(result.ToPagedResponse()));
    }

    [HttpPost("reservations")]
    [HasPermission(AppPermissions.Inventory.ReservationsManage)]
    [ProducesResponseType(typeof(ApiResponse<InventoryReservationDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateReservation([FromBody] CreateInventoryReservationRequest request, CancellationToken cancellationToken)
    {
        var val = await _createReservationValidator.ValidateAsync(request, cancellationToken);
        if (!val.IsValid) throw new ValidationException(val.Errors.GroupBy(e => e.PropertyName).ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));

        var res = await _inventoryService.CreateReservationAsync(request, cancellationToken);
        return Created(string.Empty, ApiResponse<InventoryReservationDto>.Ok(res));
    }

    [HttpPost("reservations/{id:long}/release")]
    [HasPermission(AppPermissions.Inventory.ReservationsManage)]
    [ProducesResponseType(typeof(ApiResponse<InventoryReservationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ReleaseReservation(long id, CancellationToken cancellationToken)
    {
        var res = await _inventoryService.ReleaseReservationAsync(id, cancellationToken);
        return Ok(ApiResponse<InventoryReservationDto>.Ok(res));
    }

    [HttpPost("reservations/{id:long}/consume")]
    [HasPermission(AppPermissions.Inventory.ReservationsManage)]
    [ProducesResponseType(typeof(ApiResponse<InventoryReservationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ConsumeReservation(long id, CancellationToken cancellationToken)
    {
        var res = await _inventoryService.ConsumeReservationAsync(id, cancellationToken);
        return Ok(ApiResponse<InventoryReservationDto>.Ok(res));
    }

    // ───────────── Lots & Serial Numbers ─────────────

    [HttpGet("lots")]
    [HasPermission(AppPermissions.Inventory.InventoryRead)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<InventoryLotDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLots([FromQuery] long? productId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await _inventoryService.GetLotsAsync(productId, new PagedRequest { Page = page, PageSize = pageSize }, cancellationToken);
        return Ok(ApiResponse<PagedResponse<InventoryLotDto>>.Ok(result.ToPagedResponse()));
    }

    [HttpPost("lots")]
    [HasPermission(AppPermissions.Inventory.LotsManage)]
    [ProducesResponseType(typeof(ApiResponse<InventoryLotDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateLot([FromBody] CreateInventoryLotRequest request, CancellationToken cancellationToken)
    {
        var val = await _createLotValidator.ValidateAsync(request, cancellationToken);
        if (!val.IsValid) throw new ValidationException(val.Errors.GroupBy(e => e.PropertyName).ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));

        var lot = await _inventoryService.CreateLotAsync(request, cancellationToken);
        return Created(string.Empty, ApiResponse<InventoryLotDto>.Ok(lot));
    }

    [HttpGet("serial-numbers")]
    [HasPermission(AppPermissions.Inventory.InventoryRead)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<InventorySerialNumberDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSerialNumbers([FromQuery] long? productId, [FromQuery] long? warehouseId, [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await _inventoryService.GetSerialNumbersAsync(productId, warehouseId, status, new PagedRequest { Page = page, PageSize = pageSize }, cancellationToken);
        return Ok(ApiResponse<PagedResponse<InventorySerialNumberDto>>.Ok(result.ToPagedResponse()));
    }

    [HttpPost("serial-numbers")]
    [HasPermission(AppPermissions.Inventory.SerialNumbersManage)]
    [ProducesResponseType(typeof(ApiResponse<InventorySerialNumberDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateSerialNumber([FromBody] CreateInventorySerialNumberRequest request, CancellationToken cancellationToken)
    {
        var val = await _createSerialValidator.ValidateAsync(request, cancellationToken);
        if (!val.IsValid) throw new ValidationException(val.Errors.GroupBy(e => e.PropertyName).ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));

        var sn = await _inventoryService.CreateSerialNumberAsync(request, cancellationToken);
        return Created(string.Empty, ApiResponse<InventorySerialNumberDto>.Ok(sn));
    }
}
