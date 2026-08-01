using System.Windows;
using HpCommander.Views;

namespace HpCommander.Services;

/// <summary>
/// The user-facing half of the update flow: consent, the "there is a new version" prompt, and the
/// decision between swapping in place and sending them to the releases page. Kept out of
/// <see cref="UpdateService"/> so that class stays free of dialogs.
/// </summary>
public sealed class UpdatePrompt(Window owner, AppSettings settings)
{
    private readonly UpdateService _service = new();

    /// <summary>
    /// The startup path. Silent about everything except an update actually being available -
    /// no consent nag, no "you are up to date", no error if the network is down.
    /// </summary>
    public async Task RunStartupCheckAsync()
    {
        if (!settings.UpdateCheckEnabled)
            return;

        // Once a day is plenty for a tool that releases every few weeks.
        if (settings.LastUpdateCheckUtc is { } last && DateTime.UtcNow - last < TimeSpan.FromDays(1))
            return;

        settings.LastUpdateCheckUtc = DateTime.UtcNow;
        settings.Save();

        var release = await _service.CheckAsync();
        if (release is null || !release.IsNewerThan(AppVersion.Current))
            return;

        if (settings.SkippedVersion == release.Version.ToString())
            return;

        Offer(release);
    }

    /// <summary>The Info button. Says something whatever the answer is, including "you are on the
    /// latest version" - a check that reports nothing looks broken.</summary>
    public async Task<string> RunManualCheckAsync()
    {
        if (!EnsureConsent())
            return "Update checking is off.";

        settings.LastUpdateCheckUtc = DateTime.UtcNow;
        settings.Save();

        var release = await _service.CheckAsync();
        if (release is null)
            return "Could not reach GitHub. Check your connection and try again.";

        if (!release.IsNewerThan(AppVersion.Current))
            return $"You are on the latest version ({AppVersion.CurrentDisplay}).";

        // Asking explicitly means an earlier "skip this version" no longer applies.
        settings.SkippedVersion = null;
        settings.Save();

        Offer(release);
        return $"Version {release.Version} is available.";
    }

    /// <summary>
    /// One-time explanation before the app first talks to the network, modelled on the auto-copy
    /// consent dialog. Returns whether checking is on afterwards.
    /// </summary>
    public bool EnsureConsent()
    {
        if (settings.UpdateCheckConsentGiven)
            return settings.UpdateCheckEnabled;

        var answer = MessageBox.Show(
            owner,
            "Check GitHub for new versions of HP Commander?\n\n" +
            "This asks github.com which release is newest and compares it to the version you are " +
            "running. Nothing about you or what you build is sent - it is a read of a public page.\n\n" +
            "You can change this later from the Info section.",
            "Check for updates",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        settings.UpdateCheckConsentGiven = true;
        settings.UpdateCheckEnabled = answer == MessageBoxResult.Yes;
        settings.Save();

        return settings.UpdateCheckEnabled;
    }

    private void Offer(ReleaseInfo release)
    {
        var canSwap = release.IsInstallable && UpdateService.CanUpdateInPlace();

        var body =
            $"Version {release.Version} is available. You have {AppVersion.CurrentDisplay}.\n\n" +
            Summarise(release.Notes) +
            (canSwap
                // Worth stating plainly: the README invites hand-editing Data/*.json, and this
                // replaces that folder wholesale.
                ? "\n\nUpdating replaces the program and its Data folder, then restarts. Any " +
                  "hand-edits you have made to files in Data will be lost.\n\n" +
                  "Yes to update now, No to skip this version, Cancel to be reminded later."
                : "\n\nThis copy cannot update itself" +
                  (release.IsInstallable ? " because its folder is not writable" : "") +
                  ".\n\nYes to open the releases page, No to skip this version, Cancel to be " +
                  "reminded later.");

        var answer = MessageBox.Show(
            owner, body, "Update available", MessageBoxButton.YesNoCancel, MessageBoxImage.Information);

        switch (answer)
        {
            case MessageBoxResult.Yes when canSwap:
                Install(release);
                break;

            case MessageBoxResult.Yes:
                UpdateService.OpenReleasesPage();
                break;

            case MessageBoxResult.No:
                settings.SkippedVersion = release.Version.ToString();
                settings.Save();
                break;
        }
    }

    private void Install(ReleaseInfo release)
    {
        var window = new UpdateWindow(release, _service) { Owner = owner };
        if (window.ShowDialog() != true || window.RelaunchPath is not { } path)
            return;

        UpdateService.Relaunch(path);
        Application.Current.Shutdown();
    }

    /// <summary>Release notes are free text and can run long; a dialog is not a changelog viewer.</summary>
    private static string Summarise(string notes)
    {
        if (string.IsNullOrWhiteSpace(notes))
            return "";

        var lines = notes.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Take(8)
            .ToList();

        var text = string.Join('\n', lines);
        return text.Length > 600 ? text[..600] + "..." : text;
    }
}
