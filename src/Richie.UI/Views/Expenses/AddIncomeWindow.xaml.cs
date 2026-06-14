using Richie.UI.ViewModels;
using Wpf.Ui.Controls;

namespace Richie.UI.Views.Expenses;

public partial class AddIncomeWindow : FluentWindow
{
    public AddIncomeViewModel Editor { get; }

    public AddIncomeWindow(AddIncomeViewModel editor)
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
