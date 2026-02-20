using System.Windows;
using System.Windows.Controls;

namespace DeployManager.Views
{
    /// <summary>
    /// DatabaseConfigView.xaml 的交互逻辑
    /// </summary>
    public partial class DatabaseConfigView : UserControl
    {
        public DatabaseConfigView()
        {
            InitializeComponent();

            // 同步密码框内容到 ViewModel
            PasswordBox.PasswordChanged += (s, e) =>
            {
                if (DataContext is ViewModels.DatabaseConfigViewModel vm)
                {
                    vm.Password = PasswordBox.Password;
                }
            };

            // 从 ViewModel 加载密码到密码框
            Loaded += (s, e) =>
            {
                if (DataContext is ViewModels.DatabaseConfigViewModel vm)
                {
                    PasswordBox.Password = vm.Password;
                }
            };
        }

        /// <summary>
        /// SQL 认证单选按钮点击事件
        /// </summary>
        private void OnSqlAuthRadioClick(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.DatabaseConfigViewModel vm)
            {
                vm.UseWindowsAuth = false;
            }
        }
    }
}
