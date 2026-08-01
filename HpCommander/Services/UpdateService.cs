using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;

namespace HpCommander.Services;

/// <summary>
/// Checks GitHub for a newer release and, if the install is writable, applies it in place
/// (issue #8).
/// </summary>
/// <remarks>
/// The app ships as a self-contained single file plus a loose <c>Data</c> folder, and
/// <see cref="Data.GameData.ExpectedSchemaVersion"/> makes the two a matched pair - replacing one
/// without the other produces an error dialog and an empty app on next launch. So the unit of
/// update is the whole folder, and the swap is ordered to be rollback-able: the old exe is renamed
/// rather than deleted, and nothing is moved into place until the download has been verified.
/// </remarks>
public sealed class UpdateService
{
    public const string RepositoryUrl = "https://github.com/foreverforbidden/house-party-command-builder";
    public const string ReleasesUrl = RepositoryUrl + "/releases";

    private const string LatestReleaseApi =
        "https://api.github.com/repos/foreverforbidden/house-party-command-builder/releases/latest";

    /// <summary>Renamed, not deleted: Windows lets a running exe be renamed but not removed, and
    /// keeping it is what makes step 6 of the swap reversible.</summary>
    private const string BackupSuffix = ".old";

    private const string StagingFolder = ".update-staging";

    private static readonly TimeSpan CheckTimeout = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Returns the latest release, or null if there is no answer worth acting on - offline, rate
    /// limited, malformed payload. Never throws: a failed check on startup must be invisible.
    /// </summary>
    public async Task<ReleaseInfo?> CheckAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = new HttpClient { Timeout = CheckTimeout };
            // GitHub rejects API requests that do not identify themselves.
            client.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue("HpCommander", AppVersion.CurrentDisplay));
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

            var response = await client.GetAsync(LatestReleaseApi, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return ReleaseInfo.Parse(json);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or IOException)
        {
            return null;
        }
    }

    /// <summary>
    /// Whether the exe can be replaced where it stands. Checked <em>before</em> downloading, so an
    /// install under Program Files does not pull a hundred-odd megabytes only to fail at the last
    /// step.
    /// </summary>
    public static bool CanUpdateInPlace() =>
        AppVersion.InstallDirectory is { } directory && IsWritable(directory);

    /// <summary>Actually writes a file rather than reading the ACL: the question is whether this
    /// process can write here, which permissions alone do not answer (read-only media, a
    /// virtualised Program Files, a folder locked by security software).</summary>
    public static bool IsWritable(string directory)
    {
        try
        {
            var probe = Path.Combine(directory, $".write-probe-{Guid.NewGuid():N}");
            File.WriteAllBytes(probe, []);
            File.Delete(probe);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    /// <summary>Downloads the release asset to a scratch folder under LOCALAPPDATA, reporting
    /// progress as a fraction where the total size is known.</summary>
    public async Task<string> DownloadAsync(
        ReleaseInfo release,
        IProgress<double>? progress,
        CancellationToken cancellationToken)
    {
        if (release.DownloadUrl is null)
            throw new InvalidOperationException("This release has no installable asset.");

        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "HpCommander", "update", release.Version.ToString());
        Directory.CreateDirectory(folder);

        var target = Path.Combine(folder, $"{ReleaseInfo.AssetPrefix}{release.TagName}{ReleaseInfo.AssetSuffix}");

        using (var client = new HttpClient { Timeout = Timeout.InfiniteTimeSpan })
        {
            client.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue("HpCommander", AppVersion.CurrentDisplay));

            using var response = await client
                .GetAsync(release.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var total = response.Content.Headers.ContentLength ?? release.DownloadSize;

            await using var source = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            await using var destination = File.Create(target);

            var buffer = new byte[81920];
            long copied = 0;
            int read;
            while ((read = await source.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
            {
                await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
                copied += read;
                if (total > 0)
                    progress?.Report((double)copied / total);
            }
        }

        Verify(target, release);
        return target;
    }

    /// <summary>Refuses anything that is not byte-for-byte what the release advertised. A partial
    /// download that got a 200 would otherwise be extracted over a working install.</summary>
    private static void Verify(string archivePath, ReleaseInfo release)
    {
        var actualSize = new FileInfo(archivePath).Length;
        if (release.DownloadSize > 0 && actualSize != release.DownloadSize)
            throw new InvalidDataException(
                $"Downloaded {actualSize:N0} bytes but the release lists {release.DownloadSize:N0}.");

        if (release.Sha256 is { } expected)
        {
            using var stream = File.OpenRead(archivePath);
            var actual = Convert.ToHexString(SHA256.HashData(stream));
            if (!actual.Equals(expected, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("The download does not match the checksum on the release.");
        }

        using var archive = ZipFile.OpenRead(archivePath);
        var hasExe = archive.Entries.Any(e =>
            e.FullName.Equals("HpCommander.exe", StringComparison.OrdinalIgnoreCase));
        var hasData = archive.Entries.Any(e =>
            e.FullName.StartsWith("Data/", StringComparison.OrdinalIgnoreCase) ||
            e.FullName.StartsWith("Data\\", StringComparison.OrdinalIgnoreCase));

        if (!hasExe || !hasData)
            throw new InvalidDataException("The download is missing HpCommander.exe or the Data folder.");
    }

    /// <summary>
    /// Puts the downloaded build in place and returns the exe to relaunch. The exe and Data move
    /// together; if the second move fails the first is undone, so a failure leaves the previous
    /// version running rather than a half-updated folder.
    /// </summary>
    public static string Apply(string archivePath)
    {
        var installDirectory = AppVersion.InstallDirectory
                               ?? throw new InvalidOperationException("Cannot locate the install directory.");
        var exePath = AppVersion.ExecutablePath!;

        var staging = Path.Combine(installDirectory, StagingFolder);
        DeleteDirectory(staging);
        ZipFile.ExtractToDirectory(archivePath, staging);

        var stagedExe = Path.Combine(staging, "HpCommander.exe");
        var stagedData = Path.Combine(staging, "Data");
        if (!File.Exists(stagedExe) || !Directory.Exists(stagedData))
            throw new InvalidDataException("The extracted update is missing HpCommander.exe or Data.");

        var backupExe = exePath + BackupSuffix;
        var backupData = Path.Combine(installDirectory, "Data" + BackupSuffix);
        var dataPath = Path.Combine(installDirectory, "Data");

        // Renaming a running exe is allowed; deleting it is not.
        File.Delete(backupExe);
        File.Move(exePath, backupExe);

        try
        {
            DeleteDirectory(backupData);
            if (Directory.Exists(dataPath))
                Directory.Move(dataPath, backupData);

            try
            {
                File.Move(stagedExe, exePath);
                Directory.Move(stagedData, dataPath);
            }
            catch
            {
                // Put Data back before rethrowing, so the rollback below restores a matched pair.
                DeleteDirectory(dataPath);
                if (Directory.Exists(backupData))
                    Directory.Move(backupData, dataPath);
                throw;
            }
        }
        catch
        {
            File.Delete(exePath);
            File.Move(backupExe, exePath);
            throw;
        }

        DeleteDirectory(staging);
        DeleteDirectory(backupData);
        return exePath;
    }

    /// <summary>
    /// Clears what the previous run left behind. The backup exe usually cannot be deleted on the
    /// first launch after an update - the process that was renamed is still exiting - so this is
    /// best-effort and simply runs again next time.
    /// </summary>
    public static void CleanUpAfterUpdate()
    {
        var directory = AppVersion.InstallDirectory;
        if (directory is null)
            return;

        try
        {
            foreach (var stale in Directory.EnumerateFiles(directory, "*" + BackupSuffix))
                TryDelete(stale);

            DeleteDirectory(Path.Combine(directory, StagingFolder));
            DeleteDirectory(Path.Combine(directory, "Data" + BackupSuffix));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Nothing here is worth telling the user about.
        }
    }

    public static void Relaunch(string exePath)
    {
        Process.Start(new ProcessStartInfo(exePath)
        {
            UseShellExecute = true,
            WorkingDirectory = Path.GetDirectoryName(exePath)!,
        });
    }

    public static void OpenReleasesPage()
    {
        try
        {
            Process.Start(new ProcessStartInfo(ReleasesUrl) { UseShellExecute = true });
        }
        catch
        {
            // Opening the browser is best-effort; nothing to recover if it fails.
        }
    }

    private static void TryDelete(string path)
    {
        try { File.Delete(path); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
    }

    private static void DeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
    }
}
