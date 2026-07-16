using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using HpCommander.Builders;
using HpCommander.Data;

namespace HpCommander.Views;

public partial class QuestView : UserControl, ICommandCategoryView
{
    public event EventHandler? CommandChanged;

    public bool NeedsGlobalTargets => false;

    public QuestView(GameData data)
    {
        InitializeComponent();

        // The quest name alone identifies a quest - the character isn't part of the command,
        // so every known name goes into one flat list.
        var questNames = data.QuestsByCharacter
            .SelectMany(kvp => kvp.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var combo in new[] { StartQuestCombo, CompleteQuestCombo, IncrementQuestCombo, FailQuestCombo })
        {
            foreach (var name in questNames)
                combo.Items.Add(name);
            combo.AddHandler(TextBoxBase.TextChangedEvent, new TextChangedEventHandler(Field_Changed));
        }

        foreach (var c in data.Characters)
            ListCharacterCombo.Items.Add(c);
        if (ListCharacterCombo.Items.Count > 0)
            ListCharacterCombo.SelectedIndex = 0;
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
