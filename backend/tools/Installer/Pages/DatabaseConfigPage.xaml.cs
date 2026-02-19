using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Installer.ViewModels;

namespace Installer.Pages;

public partial class DatabaseConfigPage : UserControl
{
    public DatabaseConfigPage() { InitializeComponent(); }

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm && sender is PasswordBox pb)
            vm.Config.Database.Password = pb.Password;
    }

    private async void TestConnection_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            ConnectionResult.Text = "正在测试连接...";
            ConnectionResult.Foreground = new SolidColorBrush(Colors.Gray);

            var (success, message) = await vm.TestDatabaseConnectionAsync();
            ConnectionResult.Text = message;
            ConnectionResult.Foreground = new SolidColorBrush(success ? Color.FromRgb(16, 124, 16) : Color.FromRgb(209, 52, 56));
        }
    }
}
