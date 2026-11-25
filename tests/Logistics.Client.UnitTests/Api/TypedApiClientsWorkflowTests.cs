using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Logistics.Client.Application.Contracts.Catalog;
using Logistics.Client.Application.Contracts.Cod;
using Logistics.Client.Application.Contracts.Crm;
using Logistics.Client.Application.Contracts.Finance;
using Logistics.Client.Application.Contracts.Notification;
using Logistics.Client.Application.Contracts.Reporting;
using Logistics.Client.Application.Contracts.Sales;
using Logistics.Client.Application.Contracts.Shipping;
using Logistics.Client.Application.Contracts.Warehouse;
using Logistics.Client.Application.Interfaces;
using Logistics.Client.Infrastructure.Api;
using Logistics.Client.Infrastructure.Http;
using Logistics.Client.Infrastructure.Services;
using Logistics.Shared.Contracts;
using Logistics.Shared.Contracts.Pagination;
using Moq;
using Xunit;

namespace Logistics.Client.UnitTests.Api;

public sealed class TypedApiClientsWorkflowTests
{
    private sealed record RecordedRequest(HttpMethod Method, string PathAndQuery, string? Body, string? BranchHeader, string? AuthHeader);

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public List<RecordedRequest> Requests { get; } = [];
        public Func<HttpRequestMessage, HttpResponseMessage> Responder { get; set; } = _ => JsonEnvelope(new { });

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content != null ? await request.Content.ReadAsStringAsync(cancellationToken) : null;
            var branch = request.Headers.TryGetValues("X-Branch-Id", out var bVals) ? bVals.FirstOrDefault() : null;
            var auth = request.Headers.Authorization?.ToString();
            Requests.Add(new RecordedRequest(request.Method, request.RequestUri!.PathAndQuery, body, branch, auth));
            return Responder(request);
        }
    }

    private static HttpResponseMessage JsonEnvelope<T>(T payload) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(
            JsonSerializer.Serialize(new ApiResponse<T> { Success = true, Message = "OK", Data = payload }),
            Encoding.UTF8,
            "application/json")
    };

    private static (IApiClient Client, RecordingHandler Recorder) CreateApiClientWithAuthAndBranch(long branchId = 7)
    {
        var recorder = new RecordingHandler();
        var authMock = new Mock<IAuthenticationService>();
        authMock.SetupGet(a => a.IsAuthenticated).Returns(true);
        authMock.Setup(a => a.GetValidAccessTokenAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("jwt-test-token");

        var branch = new BranchContextService();
        branch.InitializeFromProfile(new Application.Contracts.Auth.UserProfileDto(
            1, Guid.NewGuid(), "user", "u@test", "User", null, null, null, branchId, "HCM",
            [new(branchId, Guid.NewGuid(), "HCM", "HCM Hub", true)],
            ["SuperAdmin"], [], true));

        var authHandler = new AuthenticationDelegatingHandler(authMock.Object, branch)
        {
            InnerHandler = recorder
        };
        var opts = new ApiClientOptions { BaseUrl = "http://localhost:5289", TimeoutSeconds = 30 };
        var http = new HttpClient(authHandler) { BaseAddress = new Uri("http://localhost:5289/") };
        return (new ApiClient(http, opts), recorder);
    }

    [Fact]
    public async Task EndToEndDomainWorkflowEndpoints_ShouldMatchBackendRoutesAndAttachAuthBranchHeaders()
    {
        var (api, recorder) = CreateApiClientWithAuthAndBranch(branchId: 7);

        var crm = new CrmApiClient(api);
        var catalog = new CatalogApiClient(api);
        var sales = new SalesApiClient(api);
        var shipping = new ShippingApiClient(api);
        var warehouse = new WarehouseApiClient(api);
        var inventory = new InventoryApiClient(api);
        var finance = new FinanceApiClient(api);
        var cod = new CodApiClient(api);
        var notifications = new NotificationApiClient(api);
        var reporting = new ReportingApiClient(api);

        // 1. Customer create
        recorder.Responder = _ => JsonEnvelope(new CustomerDto(10, Guid.NewGuid(), 7, "HCM", "HCM Hub", "C001", "Corporate", "ACME", null, null, "a@acme.vn", "0909", 0, 30, "Postpaid", null, null, true, DateTime.UtcNow, null));
        await crm.CreateCustomerAsync(new CreateCustomerRequest(7, "C001", "Corporate", "ACME", null, null, "a@acme.vn", "0909"));

        // 2. Product create
        recorder.Responder = _ => JsonEnvelope(new ProductDto { Id = 20, Sku = "SKU-1", Name = "Box" });
        await catalog.CreateProductAsync(new CreateProductRequest { CategoryId = 1, Sku = "SKU-1", Name = "Box", UnitOfMeasureId = 1 });

        // 3. Order create + confirm
        recorder.Responder = _ => JsonEnvelope(new OrderDetailDto { Id = 30, OrderNumber = "ORD-30" });
        await sales.CreateOrderAsync(new CreateOrderRequest { CustomerId = 10, BranchId = 7 });
        await sales.ConfirmOrderAsync("30");

        // 4. Shipment from order + assign
        recorder.Responder = _ => JsonEnvelope(new ShipmentDetailDto { Id = 40, TrackingNumber = "TRK-40" });
        await shipping.CreateFromOrderAsync(new CreateShipmentFromOrderRequest { OrderId = 30, DestinationBranchId = 7 });
        recorder.Responder = _ => JsonEnvelope(new ShipmentAssignmentDto());
        await shipping.AssignAsync("40", new CreateShipmentAssignmentRequest { AssignedToEmployeeId = 99, AssignmentType = "LastMileDelivery" });

        // 5. Warehouse parcel scan + receipt confirm
        recorder.Responder = _ => JsonEnvelope(new ScanPackageResultDto { PackageId = 1, ShipmentTrackingNumber = "TRK-40" });
        await warehouse.ScanParcelAsync(new ScanPackageRequest { TrackingNumberOrBarcode = "TRK-40", WarehouseId = 2, EmployeeId = 99 });
        recorder.Responder = _ => JsonEnvelope(new WarehouseReceiptDto { Id = 50, ReceiptNumber = "REC-50" });
        await inventory.ConfirmReceiptAsync(50, new ConfirmReceiptRequest());

        // 6. Inventory adjustment
        recorder.Responder = _ => JsonEnvelope(new StockAdjustmentDto { Id = 60, AdjustmentNumber = "ADJ-60" });
        await inventory.CreateAdjustmentAsync(new CreateStockAdjustmentRequest { WarehouseId = 2, ProductId = 20, LocationId = 3, QuantityChange = 5, ReasonDescription = "Cycle count" });

        // 7. Finance invoice create + issue + payment register
        recorder.Responder = _ => JsonEnvelope(new InvoiceDto { Id = 70, InvoiceNumber = "INV-70" });
        await finance.CreateInvoiceAsync(new CreateInvoiceRequest { CustomerId = 10, InvoiceDate = DateOnly.FromDateTime(DateTime.Today) });
        await finance.IssueInvoiceAsync(70);
        recorder.Responder = _ => JsonEnvelope(new PaymentDto { Id = 71, PaymentNumber = "PAY-71" });
        await finance.RegisterPaymentAsync(new RegisterPaymentRequest { CustomerId = 10, Amount = 100000m, CurrencyId = 1 });

        // 8. COD collect + remittance create & confirm
        recorder.Responder = _ => JsonEnvelope(new CodCollectionDto { Id = 80, ExpectedCodAmount = 500000m });
        await cod.CollectAsync(80, new RecordCodCollectionRequest { CollectedAmount = 500000m });
        recorder.Responder = _ => JsonEnvelope(new CodRemittanceDto { Id = 81, RemittanceNumber = "REM-81" });
        await cod.CreateRemittanceAsync(new CreateCodRemittanceRequest { CustomerId = 10, CurrencyId = 1, BankName = "VCB", BankAccount = "123", AccountHolderName = "ACME", CollectionIds = [80] });
        await cod.ConfirmRemittanceAsync(81, new ConfirmCodRemittanceRequest { PaymentReference = "UNC-1" });

        // 9. Notification mark read
        recorder.Responder = _ => JsonEnvelope(new NotificationDto { Id = 90, IsRead = true });
        await notifications.MarkAsReadAsync(90);

        // 10. Reporting paged query + CSV export
        recorder.Responder = _ => JsonEnvelope(new PagedResponse<OrderReportRowDto>());
        await reporting.GetOrderReportAsync(new ReportFilterRequest { BranchId = 7 }, 1, 20);
        recorder.Responder = _ =>
        {
            var resp = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(Encoding.UTF8.GetBytes("a,b\n1,2")) };
            resp.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/csv");
            resp.Content.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment") { FileName = "orders.csv" };
            return resp;
        };
        var csv = await reporting.ExportCsvAsync("orders", new ReportFilterRequest { BranchId = 7 });
        csv.FileName.Should().Be("orders.csv");

        recorder.Requests.Should().AllSatisfy(r =>
        {
            r.AuthHeader.Should().Be("Bearer jwt-test-token");
            r.BranchHeader.Should().Be("7");
        });

        recorder.Requests.Select(r => (r.Method, r.PathAndQuery.Split('?')[0])).Should().ContainInOrder(
            (HttpMethod.Post, "/api/v1/customers"),
            (HttpMethod.Post, "/api/v1/catalog/products"),
            (HttpMethod.Post, "/api/v1/orders"),
            (HttpMethod.Post, "/api/v1/orders/30/confirm"),
            (HttpMethod.Post, "/api/v1/shipments/from-order"),
            (HttpMethod.Post, "/api/v1/shipments/40/assignments"),
            (HttpMethod.Post, "/api/v1/parcel-handling/scan"),
            (HttpMethod.Post, "/api/v1/inventory/receipts/50/confirm"),
            (HttpMethod.Post, "/api/v1/inventory/adjustments"),
            (HttpMethod.Post, "/api/v1/finance/invoices"),
            (HttpMethod.Post, "/api/v1/finance/invoices/70/issue"),
            (HttpMethod.Post, "/api/v1/finance/payments"),
            (HttpMethod.Post, "/api/v1/cod/collections/80/collect"),
            (HttpMethod.Post, "/api/v1/cod/remittances"),
            (HttpMethod.Post, "/api/v1/cod/remittances/81/confirm"),
            (HttpMethod.Patch, "/api/v1/notifications/90/read"),
            (HttpMethod.Get, "/api/v1/reports/orders"),
            (HttpMethod.Get, "/api/v1/reports/export/orders"));
    }
}
