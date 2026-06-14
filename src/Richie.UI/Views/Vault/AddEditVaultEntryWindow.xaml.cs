using Richie.UI.ViewModels;
using Wpf.Ui.Controls;

namespace Richie.UI.Views.Vault;

public partial class AddEditVaultEntryWindow : FluentWindow
{
    public AddEditVaultEntryViewModel Editor { get; }

    public AddEditVaultEntryWindow(AddEditVaultEntryViewModel editor)
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
