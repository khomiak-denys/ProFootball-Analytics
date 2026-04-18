using ProFootball.Infrastructure.Querying;
using Xunit;

namespace ProFootball.Infrastructure.Tests.Features.Common;

public class LikePatternTests
{
    [Fact]
    public void Contains_ShouldWrapAndEscapeSpecialCharacters()
    {
        var pattern = LikePattern.Contains(@" 100%\_player ");

        Assert.Equal(@"% 100\%\\\_player %", pattern);
    }

    [Fact]
    public void Exact_ShouldEscapeSpecialCharactersWithoutWrapping()
    {
        var pattern = LikePattern.Exact(@"right_%\");

        Assert.Equal(@"right\_\%\\", pattern);
    }
}
