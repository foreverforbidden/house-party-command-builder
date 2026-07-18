using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using HpCommander.Builders;
using HpCommander.Data;

namespace HpCommander.Views;

public partial class CutsceneView : UserControl, ICommandCategoryView
{
    private const string NoneNpc = "(none)";

    // Decorated dropdown label -> raw scene name.
    private readonly Dictionary<string, string> _sceneByLabel = new(StringComparer.OrdinalIgnoreCase);

    public event EventHandler? CommandChanged;

    public bool NeedsGlobalTargets => false;

    public CutsceneView(GameData data)
    {
        InitializeComponent();

        foreach (var cs in data.Cutscenes)
        {
            var label = cs.ToString();
            _sceneByLabel[label] = cs.Name;
            _sceneByLabel[cs.Name] = cs.Name;
            PlaySceneCombo.Items.Add(label);
            EndSceneCombo.Items.Add(label);
        }

        foreach (var c in data.Characters) StarCombo.Items.Add(c);
        if (StarCombo.Items.Count > 0) StarCombo.SelectedIndex = 0;

        foreach (var combo in new[] { Npc1, Npc2, Npc3, Npc4 })
        {
            combo.Items.Add(NoneNpc);
            foreach (var c in data.Characters) combo.Items.Add(c);
            combo.SelectedIndex = 0;
        }

        foreach (var z in data.Cutscenes.Select(c => c.Zone).Where(z => !string.IsNullOrEmpty(z)).Distinct().OrderBy(z => z))
            RandomZoneCombo.Items.Add(z);
        if (RandomZoneCombo.Items.Count > 0) RandomZoneCombo.SelectedIndex = 0;

        foreach (var c in data.Characters) RandomOtherCombo.Items.Add(c);
        if (RandomOtherCombo.Items.Count > 0) RandomOtherCombo.SelectedIndex = 0;

        PlaySceneCombo.AddHandler(TextBoxBase.TextChangedEvent, new TextChangedEventHandler(Field_Changed));
        EndSceneCombo.AddHandler(TextBoxBase.TextChangedEvent, new TextChangedEventHandler(Field_Changed));
        RandomZoneCombo.AddHandler(TextBoxBase.TextChangedEvent, new TextChangedEventHandler(Field_Changed));
    }

    private void ModeTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // SelectionChanged bubbles; only react to the tab strip itself.
        if (!ReferenceEquals(e.OriginalSource, ModeTabs)) return;
        CommandChanged?.Invoke(this, EventArgs.Empty);
    }
    private void Selector_Changed(object sender, SelectionChangedEventArgs e) => CommandChanged?.Invoke(this, EventArgs.Empty);
    private void Field_Changed(object sender, System.Windows.RoutedEventArgs e) => CommandChanged?.Invoke(this, EventArgs.Empty);

    private string SceneName(string typed) =>
        _sceneByLabel.TryGetValue(typed.Trim(), out var name) ? name : typed.Trim();

    public string BuildCommand() => ModeTabs.SelectedIndex switch
    {
        0 => BuildPlay(),
        1 => string.IsNullOrWhiteSpace(EndSceneCombo.Text)
            ? "(pick a scene)"
            : CutsceneCommandBuilder.EndScene(SceneName(EndSceneCombo.Text)),
        2 => CutsceneCommandBuilder.EndAnyWithPlayer,
        3 => RandomOtherCombo.SelectedItem is string other && !string.IsNullOrWhiteSpace(RandomZoneCombo.Text)
            ? CutsceneCommandBuilder.RandomFromLocation(RandomZoneCombo.Text.Trim(), other)
            : "(pick a zone and character)",
        _ => "",
    };

    private string BuildPlay()
    {
        if (string.IsNullOrWhiteSpace(PlaySceneCombo.Text))
            return "(pick a scene)";
        if (StarCombo.SelectedItem is not string star)
            return "(pick a star)";
        var npcs = new[] { Npc1, Npc2, Npc3, Npc4 }
            .Select(c => c.SelectedItem as string)
            .Where(s => s != null && s != NoneNpc)
            .Cast<string>();
        return CutsceneCommandBuilder.PlayScene(SceneName(PlaySceneCombo.Text), star, npcs);
    }
}
