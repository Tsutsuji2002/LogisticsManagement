using FluentAssertions;
using Logistics.Server.Application.Common.Models;
using Xunit;

namespace Logistics.Server.UnitTests.Common;

public class PagedResultTests
{
    [Fact]
    public void PagedResult_ShouldCalculateTotalPages_Correctly()
    {
        var items = new List<string> { "item1", "item2" };
        var pagedResult = new PagedResult<string>(items, totalCount: 25, page: 2, pageSize: 10);

        pagedResult.TotalPages.Should().Be(3);
        pagedResult.HasPreviousPage.Should().BeTrue();
        pagedResult.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public void PagedResult_OnFirstPage_ShouldNotHavePreviousPage()
    {
        var items = new List<int> { 1, 2, 3 };
        var pagedResult = new PagedResult<int>(items, totalCount: 30, page: 1, pageSize: 10);

        pagedResult.HasPreviousPage.Should().BeFalse();
        pagedResult.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public void PagedResult_OnLastPage_ShouldNotHaveNextPage()
    {
        var items = new List<int> { 21, 22 };
        var pagedResult = new PagedResult<int>(items, totalCount: 22, page: 3, pageSize: 10);

        pagedResult.HasPreviousPage.Should().BeTrue();
        pagedResult.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public void ToPagedResponse_ShouldMapAllProperties_Accurately()
    {
        var items = new List<string> { "Alpha", "Beta", "Gamma" };
        var result = new PagedResult<string>(items, totalCount: 15, page: 2, pageSize: 3);

        var response = result.ToPagedResponse();

        response.Items.Should().BeEquivalentTo(items);
        response.TotalCount.Should().Be(15);
        response.Page.Should().Be(2);
        response.PageSize.Should().Be(3);
        response.TotalPages.Should().Be(5);
        response.HasPreviousPage.Should().BeTrue();
        response.HasNextPage.Should().BeTrue();
    }
}
