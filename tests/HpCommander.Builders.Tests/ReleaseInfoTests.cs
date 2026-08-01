using HpCommander.Services;
using Xunit;

namespace HpCommander.Builders.Tests;

/// <summary>
/// The update check reads an API response written by someone else and decides, on that basis, to
/// replace the user's installation. Everything that can go wrong in that decision - a tag that is
/// not a version, a release with no asset attached, a payload that is not what we expect - has to
/// end in "do nothing" rather than an exception or a wrong answer.
/// </summary>
public class ReleaseInfoTests
{
    private const string Asset = "HpCommander-v1.8.0-win-x64.zip";

    private static string Payload(
        string tag = "v1.8.0",
        string? assetName = Asset,
        string body = "Fixed the Run tab.",
        string? digest = "sha256:abc123",
        long size = 70_000_000) =>
        $$"""
        {
          "tag_name": "{{tag}}",
          "body": "{{body}}",
          "assets": [
            {{(assetName is null ? "" : $$"""
            {
              "name": "{{assetName}}",
              "size": {{size}},
              "digest": "{{digest}}",
              "browser_download_url": "https://example.invalid/{{assetName}}"
            }
            """)}}
          ]
        }
        """;

    // ---------------- happy path ----------------

    [Fact]
    public void ReadsTheVersionNotesAndAsset()
    {
        var release = ReleaseInfo.Parse(Payload());

        Assert.NotNull(release);
        Assert.Equal(new Version(1, 8, 0), release.Version);
        Assert.Equal("Fixed the Run tab.", release.Notes);
        Assert.Equal($"https://example.invalid/{Asset}", release.DownloadUrl);
        Assert.Equal(70_000_000, release.DownloadSize);
        Assert.Equal("abc123", release.Sha256);
        Assert.True(release.IsInstallable);
    }

    [Theory]
    [InlineData("v1.8.0", true)]
    [InlineData("v1.7.1", true)]
    [InlineData("v1.7.0", false)]
    [InlineData("v1.6.9", false)]
    [InlineData("v2.0.0", true)]
    public void ComparesAgainstTheRunningVersion(string tag, bool expectedNewer)
    {
        var release = ReleaseInfo.Parse(Payload(tag: tag));

        Assert.NotNull(release);
        Assert.Equal(expectedNewer, release.IsNewerThan(new Version(1, 7, 0)));
    }

    // ---------------- tags ----------------

    [Theory]
    [InlineData("v1.8.0", 1, 8, 0)]
    [InlineData("1.8.0", 1, 8, 0)]
    [InlineData("V1.8.0", 1, 8, 0)]
    [InlineData("  v1.8.0  ", 1, 8, 0)]
    [InlineData("v1.8", 1, 8, -1)]
    public void AcceptsTheTagFormsReleasesActuallyUse(string tag, int major, int minor, int build)
    {
        Assert.True(ReleaseInfo.TryParseTag(tag, out var version));
        Assert.Equal(major, version.Major);
        Assert.Equal(minor, version.Minor);
        Assert.Equal(build, version.Build);
    }

    [Theory]
    [InlineData("")]
    [InlineData("latest")]
    [InlineData("v")]
    [InlineData("release-2024")]
    [InlineData("v1.8.0-beta")]
    // A bare major would parse as 1.0.0 and could present a downgrade as an upgrade.
    [InlineData("v2")]
    public void RejectsTagsThatAreNotVersions(string tag)
    {
        Assert.False(ReleaseInfo.TryParseTag(tag, out _));
    }

    // ---------------- malformed and partial payloads ----------------

    [Theory]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("[]")]
    [InlineData("{}")]
    [InlineData("""{"tag_name": null}""")]
    [InlineData("""{"tag_name": ""}""")]
    [InlineData("""{"tag_name": "nightly"}""")]
    public void ReturnsNullRatherThanThrowingOnAnythingUnusable(string json)
    {
        Assert.Null(ReleaseInfo.Parse(json));
    }

    [Fact]
    public void AReleaseWithNoAssetIsReportedButNotInstallable()
    {
        // Worth surfacing - the user can still be pointed at the releases page - but there is
        // nothing to download and swap.
        var release = ReleaseInfo.Parse(Payload(assetName: null));

        Assert.NotNull(release);
        Assert.Equal(new Version(1, 8, 0), release.Version);
        Assert.False(release.IsInstallable);
        Assert.Null(release.DownloadUrl);
    }

    [Fact]
    public void IgnoresAssetsThatAreNotTheWindowsBuild()
    {
        var release = ReleaseInfo.Parse(Payload(assetName: "HpCommander-v1.8.0-linux-x64.tar.gz"));

        Assert.NotNull(release);
        Assert.False(release.IsInstallable);
    }

    [Fact]
    public void ToleratesAnAssetWithNoChecksum()
    {
        // Releases cut before GitHub started reporting digests still verify by size.
        var release = ReleaseInfo.Parse(Payload(digest: null));

        Assert.NotNull(release);
        Assert.True(release.IsInstallable);
        Assert.Null(release.Sha256);
        Assert.Equal(70_000_000, release.DownloadSize);
    }

    [Fact]
    public void ToleratesAReleaseWithNoNotes()
    {
        var release = ReleaseInfo.Parse(Payload(body: ""));

        Assert.NotNull(release);
        Assert.Equal("", release.Notes);
    }
}
