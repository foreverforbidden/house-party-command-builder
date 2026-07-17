using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using HpCommander.Builders;
using HpCommander.Data;

namespace HpCommander.Views;

public partial class QuestView : UserControl, ICommandCategoryView
{
    private const string DefaultStory = "Original Story";

    private readonly GameData _data;

    public event EventHandler? CommandChanged;

    public bool NeedsGlobalTargets => false;

    public QuestView(GameData data)
    {
        InitializeComponent();
        _data = data;

        // Quests only work in the story you're currently playing, so the name
        // lists are scoped to the selected story rather than pooled together.
        foreach (var story in _data.QuestsByStory.Keys.OrderBy(s => s, StringComparer.OrdinalIgnoreCase))
            StoryCombo.Items.Add(story);
        var defaultIndex = StoryCombo.Items.IndexOf(DefaultStory);
        StoryCombo.SelectedIndex = defaultIndex >= 0 ? defaultIndex : (StoryCombo.Items.Count > 0 ? 0 : -1);

        foreach (var combo in QuestCombos)
            combo.AddHandler(TextBoxBase.TextChangedEvent, new TextChangedEventHandler(Field_Changed));

        foreach (var c in _data.Characters)
            ListCharacterCombo.Items.Add(c);
        if (ListCharacterCombo.Items.Count > 0)
            ListCharacterCombo.SelectedIndex = 0;

        RepopulateQuests();
    }

    private ComboBox[] QuestCombos => new[] { StartQuestCombo, CompleteQuestCombo, IncrementQuestCombo, FailQuestCombo };

    private void StoryCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        RepopulateQuests();
        CommandChanged?.Invoke(this, EventArgs.Empty);
    }

    private void RepopulateQuests()
    {
        var names = StoryCombo.SelectedItem is string story
                    && _data.QuestsByStory.TryGetValue(story, out var byChar)
            ? byChar.SelectMany(kvp => kvp.Value)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                    .ToList()
            : new List<string>();

        foreach (var combo in QuestCombos)
        {
            var current = combo.Text;
            combo.Items.Clear();
            foreach (var n in names)
                combo.Items.Add(n);
            combo.Text = current;
        }
    }

    private void ModeTabs_SelectionChanged(object sender, SelectionChangedEventArgs e) => CommandChanged?.Invoke(this, EventArgs.Empty);

    private void Selector_Changed(object sender, SelectionChangedEventArgs e) => CommandChanged?.Invoke(this, EventArgs.Empty);

    private void Field_Changed(object sender, RoutedEventArgs e) => CommandChanged?.Invoke(this, EventArgs.Empty);

    private static string BuildManage(string subcommand, string questName) =>
        string.IsNullOrWhiteSpace(questName)
            ? "(type or pick a quest name)"
            : QuestCommandBuilder.Manage(subcommand, questName.Trim());

    public string BuildCommand() => ModeTabs.SelectedIndex switch
    {
        0 => BuildManage("start", StartQuestCombo.Text),
        1 => BuildManage("complete", CompleteQuestCombo.Text),
        2 => BuildManage("increment", IncrementQuestCombo.Text),
        3 => BuildManage("fail", FailQuestCombo.Text),
        4 => ListCharacterCombo.SelectedItem is string character
            ? QuestCommandBuilder.List(character)
            : "(pick a character)",
        _ => "",
    };
}
