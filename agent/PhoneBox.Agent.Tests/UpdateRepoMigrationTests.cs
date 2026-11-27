using PhoneBox.Agent;
using Xunit;

/// <summary>
/// This fork updates from its own repository. A config saved while the original project was the default must not keep
/// pointing there, or a later release of the original would replace this build.
/// </summary>
public sealed class UpdateRepoMigrationTests : IDisposable
{
    private const string ThisFork = "noamgdalyahu/phonebox-unslop";
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "phonebox-repo-" + Guid.NewGuid().ToString("N"));

    public UpdateRepoMigrationTests() => Directory.CreateDirectory(_dir);
    public void Dispose() => Directory.Delete(_dir, recursive: true);

    private string Write(string json)
    {
        var path = Path.Combine(_dir, "config.json");
        File.WriteAllText(path, json);
        return path;
    }

    [Fact]
    public void ANewConfigUpdatesFromThisFork()
    {
        var store = new ConfigStore(Path.Combine(_dir, "none.json"));
        Assert.Equal(ThisFork, store.Get().Updates.Repo);
    }

    [Theory]
    [InlineData("ersinkoc/phonebox")]
    [InlineData("ERSINKOC/PhoneBox")]
    [InlineData("  ersinkoc/phonebox  ")]
    [InlineData("")]
    public void ASavedOriginalOrEmptyRepositoryMovesToThisFork(string saved)
    {
        var store = new ConfigStore(Write("{\"updates\":{\"auto\":true,\"repo\":\"" + saved + "\"}}"));
        Assert.Equal(ThisFork, store.Get().Updates.Repo);
    }

    [Fact]
    public void SavingTheOriginalRepositoryAlsoMovesIt()
    {
        var store = new ConfigStore(Path.Combine(_dir, "config.json"));
        Assert.Null(store.Save("{\"updates\":{\"repo\":\"ersinkoc/phonebox\"}}"));
        Assert.Equal(ThisFork, store.Get().Updates.Repo);
    }

    [Fact]
    public void ARepositoryChosenOnPurposeIsKept()
    {
        var store = new ConfigStore(Write("{\"updates\":{\"repo\":\"someone/else\"}}"));
        Assert.Equal("someone/else", store.Get().Updates.Repo);
    }
}
