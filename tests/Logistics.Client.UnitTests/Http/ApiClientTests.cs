using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Logistics.Client.Infrastructure.Exceptions;
using Logistics.Client.Infrastructure.Http;
using Logistics.Shared.Contracts;
using Xunit;

namespace Logistics.Client.UnitTests.Http;

public sealed class ApiClientTests
{
    private sealed record SamplePayload(long Id, string Name);

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => responder(request, cancellationToken);
    }

    private static ApiClient CreateClient(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder, int timeoutSeconds = 30)
    {
        var handler = new StubHttpMessageHandler(responder);
        var opts = new ApiClientOptions { BaseUrl = "http://localhost:5289", TimeoutSeconds = timeoutSeconds };
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5289/") };
        return new ApiClient(http, opts);
    }

    [Fact]
    public async Task GetAsync_ShouldUnwrapApiResponsePayload()
    {
        var client = CreateClient((_, _) =>
        {
            var body = JsonSerializer.Serialize(new ApiResponse<SamplePayload>
            {
                Success = true,
                Message = "OK",
                Data = new SamplePayload(42, "Demo")
            });
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            });
        });

        var envelope = await client.GetAsync<ApiResponse<SamplePayload>>("api/v1/sample/42");

        envelope.Should().NotBeNull();
        envelope!.Data.Should().NotBeNull();
        envelope.Data!.Id.Should().Be(42);
        envelope.Data.Name.Should().Be("Demo");
    }

    [Fact]
    public async Task PostAsync_WhenValidationFails_ShouldThrowStructuredApiException()
    {
        var client = CreateClient((_, _) =>
        {
            var body = JsonSerializer.Serialize(new ApiResponse
            {
                Success = false,
                Message = "Dữ liệu không hợp lệ",
                ErrorCode = "VALIDATION_ERROR",
                TraceId = "trace-123",
                ValidationErrors = new Dictionary<string, string[]>
                {
                    ["Code"] = ["Mã không được để trống."]
                }
            });
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            });
        });

        var act = async () => await client.PostAsync<SamplePayload, SamplePayload>("api/v1/sample", new SamplePayload(0, ""));

        var ex = (await act.Should().ThrowAsync<ApiException>()).Which;
        ex.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ex.ErrorCode.Should().Be("VALIDATION_ERROR");
        ex.TraceId.Should().Be("trace-123");
        ex.ValidationErrors.Should().ContainKey("Code");
    }

    [Fact]
    public async Task GetAsync_WhenForbidden_ShouldExposeIsForbidden()
    {
        var client = CreateClient((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Forbidden)));

        var act = async () => await client.GetAsync<SamplePayload>("api/v1/restricted");

        var ex = (await act.Should().ThrowAsync<ApiException>()).Which;
        ex.IsForbidden.Should().BeTrue();
        ex.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetAsync_WhenNetworkFails_ShouldThrowServiceUnavailableApiException()
    {
        var client = CreateClient((_, _) => throw new HttpRequestException("Connection refused"));

        var act = async () => await client.GetAsync<SamplePayload>("api/v1/health");

        var ex = (await act.Should().ThrowAsync<ApiException>()).Which;
        ex.IsNetworkError.Should().BeTrue();
        ex.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task GetAsync_WhenCancelledByCaller_ShouldPropagateOperationCanceled()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var client = CreateClient((_, ct) =>
        {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });

        var act = async () => await client.GetAsync<SamplePayload>("api/v1/slow", cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task GetFileAsync_ShouldReturnBytesAndFileNameFromContentDisposition()
    {
        var csvBytes = Encoding.UTF8.GetBytes("col1,col2\n1,2\n");
        var client = CreateClient((_, _) =>
        {
            var msg = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(csvBytes)
            };
            msg.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/csv");
            msg.Content.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment")
            {
                FileName = "\"orders-report.csv\""
            };
            return Task.FromResult(msg);
        });

        var (content, contentType, fileName) = await client.GetFileAsync("api/v1/reports/export/orders?format=csv");

        content.Should().Equal(csvBytes);
        contentType.Should().Be("text/csv");
        fileName.Should().Be("orders-report.csv");
    }
}
