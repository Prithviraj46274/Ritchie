using Wpf.Ui.Controls;

namespace Richie.UI.Views.Liabilities;

public partial class AddEditLoanWindow : FluentWindow
{
    public AddEditLoanViewModel Editor { get; }

    public AddEditLoanWindow(AddEditLoanViewModel editor)
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