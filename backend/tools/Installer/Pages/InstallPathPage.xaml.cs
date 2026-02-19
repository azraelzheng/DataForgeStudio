using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace Installer.Pages;

public partial class InstallPathPage : UserControl
{
    public InstallPathPage() { InitializeComponent(); }

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog { Title = "选择安装目录", InitialDirectory = @"C:\Program Files" };
        if (dialog.ShowDialog() == true && DataContext is ViewModels.MainViewModel vm)
            vm.Config.InstallPath = dialog.FolderName;
    }
}
