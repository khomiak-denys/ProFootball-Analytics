using ProFootball.Infrastructure.Querying;
using Xunit;

namespace ProFootball.Infrastructure.Tests.Features.Common;

public class PagingTests
{
    [Fact]
    public void Normalize_ShouldApplyDefaultPageAndSize_WhenInputIsBelowMinimum()
    {
        var (page, pageSize, skip) = Paging.Normalize(0, 0);

        Assert.Equal(1, page);
        Assert.Equal(20, pageSize);
        Assert.Equal(0, skip);
    }

    [Fact]
    public void Normalize_ShouldClampPageSize_WhenInputExceedsMax()
    {
        var (page, pageSize, skip) = Paging.Normalize(page: 2, pageSize: 999, maxPageSize: 200);

        Assert.Equal(2, page);
        Assert.Equal(200, pageSize);
        Assert.Equal(200, skip);
    }

    [Fact]
    public void Normalize_ShouldClampSkipToIntMax_WhenSkipOverflowsInt()
    {
        var (_, _, skip) = Paging.Normalize(page: int.MaxValue, pageSize: 200);

        Assert.Equal(int.MaxValue, skip);
    }
}
