using Richie.UI.ViewModels;
using Wpf.Ui.Controls;

namespace Richie.UI.Views.Expenses;

public partial class AddEditRecurringWindow : FluentWindow
{
    public AddEditRecurringViewModel Editor { get; }

    public AddEditRecurringWindow(AddEditRecurringViewModel editor)
    {
        InitializeComponent();
        Editor = editor;
        DataContext = editor;
        editor.CloseRequested += OnCloseRequested;
        Closed += (_, _) => editor.CloseRequested -= OnCloseRequested;
    }

    private void OnCloseRequested(bool success)
    {
        DialogResult = success;
        Close();
    }
}
