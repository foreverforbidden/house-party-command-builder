using System.Windows;
using System.Windows.Controls;
using HpCommander.Builders;
using HpCommander.Data;

namespace HpCommander.Views;

public partial class CutsceneView : CommandCategoryViewBase
{
    private const string NoneNpc = "(none)";

    // Decorated dropdown label -> raw scene name.
    private readonly Dictionary<string, string> _sceneByLabel = new(StringComparer.OrdinalIgnoreCase);

    // Raw scene name -> its metadata, for validating the cast before the game silently refuses it.
    private readonly Dictionary<string, Cutscene> _sceneByName = new(StringComparer.OrdinalIgnoreCase);

    private enum Mode { Play, End, EndAny, Random }

    public CutsceneView(GameData data)
    {
        InitializeComponent();

        using (SuspendRecompute())
        {
            foreach (var cs in data.Cutscenes)
            {
                var label = cs.ToString();
                _sceneByLabel[label] = cs.Name;
                _sceneByLabel[cs.Name] = cs.Name;
                _sceneByName[cs.Name] = cs;
                SceneCombo.Items.Add(label);
            }

            // One character for both the star and the random-scene partner, so switching tabs
            // no longer discards who you picked.
            FillChars(CharCombo, data);

            foreach (var combo in new[] { Npc1, Npc2, Npc3, Npc4 })
                FillChars(combo, data, allTarget: NoneNpc);

            var zones = data.Cutscenes.Select(c => c.Zone)
                .Where(z => !string.IsNullOrEmpty(z))
                .Distinct()
                .OrderBy(z => z);
            Fill(RandomZoneCombo, zones);

            ApplyTabContext();
        }
    }

    private Mode Current => (Mode)Math.Max(0, ModeTabs.SelectedIndex);

    private void ModeTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // SelectionChanged bubbles; only react to the tab strip itself.
        if (!ReferenceEquals(e.OriginalSource, ModeTabs)) return;
        ApplyTabContext();
        Recompute();
    }

    private void ApplyTabContext()
    {
        SceneLabel.Text = Current == Mode.End
            ? "Scene to end"
            : "Scene (the label shows how many NPCs it needs)";
        CharLabel.Text = Current == Mode.Random
            ? "Other character (paired with the Player)"
            : "Star";

        ScenePanel.Visibility = Current is Mode.Play or Mode.End ? Visibility.Visible : Visibility.Collapsed;
        CharPanel.Visibility = Current is Mode.Play or Mode.Random ? Visibility.Visible : Visibility.Collapsed;
    }

    private string SceneName(string typed) =>
        _sceneByLabel.TryGetValue(typed.Trim(), out var name) ? name : typed.Trim();

    public override CommandResult BuildCommand() => Current switch
    {
        Mode.Play => BuildPlay(),
        Mode.End => string.IsNullOrWhiteSpace(SceneCombo.Text)
            ? CommandResult.NeedsInput("Pick a scene")
            : CommandResult.Ok(CutsceneCommandBuilder.EndScene(SceneName(SceneCombo.Text))),
        Mode.EndAny => CommandResult.Ok(CutsceneCommandBuilder.EndAnyWithPlayer),
        Mode.Random => string.IsNullOrWhiteSpace(CharCombo.EffectiveValue)
            ? CommandResult.NeedsInput("Pick a character")
            : string.IsNullOrWhiteSpace(RandomZoneCombo.Text)
                ? CommandResult.NeedsInput("Pick a zone")
                : CommandResult.Ok(CutsceneCommandBuilder.RandomFromLocation(
                    RandomZoneCombo.Text.Trim(), CharCombo.EffectiveValue)),
        _ => CommandResult.Error($"Unhandled tab index {ModeTabs.SelectedIndex}"),
    };

    private CommandResult BuildPlay()
    {
        if (string.IsNullOrWhiteSpace(SceneCombo.Text))
            return CommandResult.NeedsInput("Pick a scene");
        var star = CharCombo.EffectiveValue;
        if (string.IsNullOrWhiteSpace(star))
            return CommandResult.NeedsInput("Pick a star");

        var name = SceneName(SceneCombo.Text);
        var npcs = new[] { Npc1, Npc2, Npc3, Npc4 }
            .Select(c => c.SelectedItem as string)
            .Where(s => s != null && s != NoneNpc)
            .Cast<string>()
            .ToList();

        // A wrong cast count just fails silently in-game, so say so here instead.
        if (_sceneByName.TryGetValue(name, out var scene))
        {
            if (npcs.Count != scene.Npcs)
                return CommandResult.NeedsInput(
                    $"'{scene.Name}' needs exactly {scene.Npcs} NPC{(scene.Npcs == 1 ? "" : "s")} - you have {npcs.Count}");

            if (scene.StarGender.Length > 0 && !string.Equals(star, "player", StringComparison.OrdinalIgnoreCase))
                CastNote.Text = $"This scene expects a {scene.StarGender.ToLowerInvariant()} star.";
            else
                CastNote.Text = "";
        }
        else
        {
            CastNote.Text = "";
        }

        return CommandResult.Ok(CutsceneCommandBuilder.PlayScene(name, star, npcs));
    }
}
