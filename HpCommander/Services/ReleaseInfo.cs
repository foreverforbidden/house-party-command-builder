using System.Text.Json;

namespace HpCommander.Services;

/// <summary>
/// The parts of a GitHub release the updater cares about. Kept separate from the download and swap
/// so the parsing and the version comparison - the bits with actual branching in them - can be
/// tested without a network or a filesystem.
/// </summary>
public sealed record ReleaseInfo(
    Version Version,
    string TagName,
    string Notes,
    string? DownloadUrl,
    long DownloadSize,
    string? Sha256)
{
    /// <summary>The asset the release workflow attaches. Anything else on the release - source
    /// zipballs, a loose exe someone uploaded by hand - is not something we know how to apply.</summary>
    public const string AssetPrefix = "HpCommander-";
    public const string AssetSuffix = "-win-x64.zip";

    public bool IsNewerThan(Version other) => Version > other;

    /// <summary>An update we can actually apply, as opposed to one we can only point at.</summary>
    public bool IsInstallable => DownloadUrl is not null;

    /// <summary>
    /// Reads the payload of <c>/releases/latest</c>. Returns null rather than throwing on anything
    /// unexpected: a malformed response is a reason to say nothing, not to interrupt the user.
    /// </summary>
    public static ReleaseInfo? Parse(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
                return null;

            if (!root.TryGetProperty("tag_name", out var tagElement))
                return null;

            var tag = tagElement.GetString();
            if (string.IsNullOrWhiteSpace(tag))
                return null;

            if (!TryParseTag(tag, out var version))
                return null;

            var notes = root.TryGetProperty("body", out var body) ? body.GetString() ?? "" : "";

            FindAsset(root, out var url, out var size, out var sha);

            return new ReleaseInfo(version, tag, notes.Trim(), url, size, sha);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>Tags are written "v1.7.0"; System.Version does not want the v.</summary>
    public static bool TryParseTag(string tag, out Version version)
    {
        version = new Version(0, 0, 0);

        var trimmed = tag.Trim();
        if (trimmed.StartsWith('v') || trimmed.StartsWith('V'))
            trimmed = trimmed[1..];

        // Version.TryParse accepts a bare "1", which reads more like a typo than a release and
        // would be treated as 1.0.0 - potentially offering a downgrade as an upgrade.
        if (!trimmed.Contains('.') || !Version.TryParse(trimmed, out var parsed))
            return false;

        version = parsed;
        return true;
    }

    private static void FindAsset(JsonElement root, out string? url, out long size, out string? sha)
    {
        url = null;
        size = 0;
        sha = null;

        if (!root.TryGetProperty("assets", out var assets) || assets.ValueKind != JsonValueKind.Array)
            return;

        foreach (var asset in assets.EnumerateArray())
        {
            var name = asset.TryGetProperty("name", out var n) ? n.GetString() : null;
            if (name is null ||
                !name.StartsWith(AssetPrefix, StringComparison.OrdinalIgnoreCase) ||
                !name.EndsWith(AssetSuffix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            url = asset.TryGetProperty("browser_download_url", out var u) ? u.GetString() : null;
            size = asset.TryGetProperty("size", out var s) && s.TryGetInt64(out var parsed) ? parsed : 0;

            // GitHub reports this as "sha256:<hex>". Absent on older releases, in which case the
            // download is verified by size alone.
            var digest = asset.TryGetProperty("digest", out var d) ? d.GetString() : null;
            if (digest is not null && digest.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase))
                sha = digest["sha256:".Length..];

            return;
        }
    }
}
