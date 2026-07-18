using System.Windows.Controls;
using HpCommander.Builders;
using HpCommander.Data;

namespace HpCommander.Views;

public partial class CutsceneView : CommandCategoryViewBase
{
    private const string NoneNpc = "(none)";

    // Decorated dropdown label -> raw scene name.
    private readonly Dictionary<string, string> _sceneByLabel = new(StringComparer.OrdinalIgnoreCase);

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
                PlaySceneCombo.Items.Add(label);
                EndSceneCombo.Items.Add(label);
            }

            FillChars(StarCombo, data);

            foreach (var combo in new[] { Npc1, Npc2, Npc3, Npc4 })
                FillChars(combo, data, allTarget: NoneNpc);

            var zones = data.Cutscenes.Select(c => c.Zone)
                .Where(z => !string.IsNullOrEmpty(z))
                .Distinct()
                .OrderBy(z => z);
            Fill(RandomZoneCombo, zones);

            FillChars(RandomOtherCombo, data);
        }
    }

    private void ModeTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // SelectionChanged bubbles; only react to the tab strip itself.
        if (!ReferenceEquals(e.OriginalSource, ModeTabs)) return;
        Recompute();
    }

    private string SceneName(string typed) =>
        _sceneByLabel.TryGetValue(typed.Trim(), out var name) ? name : typed.Trim();

    public override CommandResult BuildCommand() => ModeTabs.SelectedIndex switch
    {
        0 => BuildPlay(),
        1 => string.IsNullOrWhiteSpace(EndSceneCombo.Text)
            ? CommandResult.NeedsInput("Pick a scene")
            : CommandResult.Ok(CutsceneCommandBuilder.EndScene(SceneName(EndSceneCombo.Text))),
        2 => CommandResult.Ok(CutsceneCommandBuilder.EndAnyWithPlayer),
        3 => RandomOtherCombo.SelectedItem is string other && !string.IsNullOrWhiteSpace(RandomZoneCombo.Text)
            ? CommandResult.Ok(CutsceneCommandBuilder.RandomFromLocation(RandomZoneCombo.Text.Trim(), other))
            : CommandResult.NeedsInput("Pick a zone and a character"),
        _ => CommandResult.Error($"Unhandled tab index {ModeTabs.SelectedIndex}"),
    };

    private CommandResult BuildPlay()
    {
        if (string.IsNullOrWhiteSpace(PlaySceneCombo.Text))
            return CommandResult.NeedsInput("Pick a scene");
        if (StarCombo.SelectedItem is not string star)
            return CommandResult.NeedsInput("Pick a star");
        var npcs = new[] { Npc1, Npc2, Npc3, Npc4 }
            .Select(c => c.SelectedItem as string)
            .Where(s => s != null && s != NoneNpc)
            .Cast<string>();
        return CommandResult.Ok(CutsceneCommandBuilder.PlayScene(SceneName(PlaySceneCombo.Text), star, npcs));
    }
}
