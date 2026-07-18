using System.Windows.Controls;
using HpCommander.Builders;
using HpCommander.Data;

namespace HpCommander.Views;

public partial class QuestView : CommandCategoryViewBase
{
    private const string DefaultStory = "Original Story";

    private readonly GameData _data;

    public QuestView(GameData data)
    {
        InitializeComponent();
        _data = data;

        using (SuspendRecompute())
        {
            // Quests only work in the story you're currently playing, so the name
            // lists are scoped to the selected story rather than pooled together.
            var stories = _data.QuestsByStory.Keys.OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToList();
            Fill(StoryCombo, stories, selectedIndex: Math.Max(0, stories.IndexOf(DefaultStory)));
            Fill(ListCharacterCombo, _data.Characters);
            RepopulateQuests();
        }
    }

    private ComboBox[] QuestCombos => [StartQuestCombo, CompleteQuestCombo, IncrementQuestCombo, FailQuestCombo];

    private void StoryCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        RepopulateQuests();
        Recompute();
    }

    private void RepopulateQuests()
    {
        var names = StoryCombo.SelectedItem is string story
                    && _data.QuestsByStory.TryGetValue(story, out var byChar)
            ? byChar.SelectMany(kvp => kvp.Value)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                    .ToList()
            : [];

        foreach (var combo in QuestCombos)
            RefillPreservingText(combo, names);
    }

    private void ModeTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // SelectionChanged bubbles; only react to the tab strip itself.
        if (!ReferenceEquals(e.OriginalSource, ModeTabs)) return;
        Recompute();
    }

    private static CommandResult BuildManage(string subcommand, string questName) =>
        string.IsNullOrWhiteSpace(questName)
            ? CommandResult.NeedsInput("Type or pick a quest name")
            : CommandResult.Ok(QuestCommandBuilder.Manage(subcommand, questName.Trim()));

    public override CommandResult BuildCommand() => ModeTabs.SelectedIndex switch
    {
        0 => BuildManage("start", StartQuestCombo.Text),
        1 => BuildManage("complete", CompleteQuestCombo.Text),
        2 => BuildManage("increment", IncrementQuestCombo.Text),
        3 => BuildManage("fail", FailQuestCombo.Text),
        4 => ListCharacterCombo.SelectedItem is string character
            ? CommandResult.Ok(QuestCommandBuilder.List(character))
            : CommandResult.NeedsInput("Pick a character"),
        _ => CommandResult.Error($"Unhandled tab index {ModeTabs.SelectedIndex}"),
    };
}
