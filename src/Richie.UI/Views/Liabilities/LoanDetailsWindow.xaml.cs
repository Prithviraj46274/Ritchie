using Richie.UI.ViewModels;
using Wpf.Ui.Controls;

namespace Richie.UI.Views.Liabilities;

public partial class LoanDetailsWindow : FluentWindow
{
    private readonly LoanDetailsViewModel _vm;

    public LoanDetailsWindow(LoanDetailsViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;
    }

    public void Initialize(Guid loanId) => _vm.Initialize(loanId);
}