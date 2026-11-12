using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;
using TiempoApp.Desktop.ViewModels;

namespace TiempoApp.Desktop.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = _viewModel = viewModel;
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        // Cancel any in-flight request when the window closes.
        _viewModel.SearchCancelCommand.Execute(null);
        base.OnClosing(e);
    }

    private void OnHyperlinkNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        e.Handled = true;
    }
}
