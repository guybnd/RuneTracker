using RuneshapePriceChecker.Startup;
using Xunit;

namespace RuneshapePriceChecker.Tests.Startup;

/// <summary>
/// Which repository the updater compares against (RUNE-27).
///
/// RuneTracker is a fork, and the updater shipped pointing at the repository it was forked from.
/// That is not a cosmetic mistake: this fork's 0.1.0 against upstream's 1.0.10 told every user
/// they were nine versions behind, and the update button would have installed the other
/// application over this one. The defaults are worth a test precisely because nothing else would
/// have caught it — the code was correct, it was just aimed at the wrong repository.
/// </summary>
public class UpdateCheckerRepoTests
{
    [Fact]
    public void DefaultsToThisForkNotTheOneItWasForkedFrom()
    {
        Assert.Equal("guybnd", UpdateChecker.DefaultRepoOwner);
        Assert.Equal("RuneTracker", UpdateChecker.DefaultRepoName);
        Assert.NotEqual("Barragek0", UpdateChecker.DefaultRepoOwner);
    }

    [Fact]
    public void UsesTheDefaultsWhenNothingIsConfigured()
    {
        var options = new UpdateOptions();

        Assert.Equal(UpdateChecker.DefaultRepoOwner, UpdateChecker.RepoOwner(options));
        Assert.Equal(UpdateChecker.DefaultRepoName, UpdateChecker.RepoName(options));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void FallsBackToTheDefaultsRatherThanAskingGitHubForNothing(string? configured)
    {
        var options = new UpdateOptions { RepoOwner = configured!, RepoName = configured! };

        Assert.Equal(UpdateChecker.DefaultRepoOwner, UpdateChecker.RepoOwner(options));
        Assert.Equal(UpdateChecker.DefaultRepoName, UpdateChecker.RepoName(options));
    }

    [Fact]
    public void LetsAnotherForkPointSomewhereElseWithoutTouchingTheCode()
    {
        var options = new UpdateOptions { RepoOwner = " someone ", RepoName = " TheirFork " };

        Assert.Equal("someone", UpdateChecker.RepoOwner(options));
        Assert.Equal("TheirFork", UpdateChecker.RepoName(options));
    }
}
