using CompanioNc8;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Automation;

namespace iMonitor2
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        public IServiceProvider? ServiceProvider { get; private set; }
        public static IConfiguration? _configuration { get; private set; }
        protected override void OnStartup(StartupEventArgs e)
        {
            AutomationElement desktop = AutomationElement.RootElement;
            AutomationElement frmMain = desktop.FindFirst(TreeScope.Children,
                                new PropertyCondition(AutomationElement.AutomationIdProperty, "iMonitor2"));
            if (frmMain != null)
            {
                System.Windows.MessageBox.Show("此程式已經開啟");
                // 20220731 將程式復原
                var p = (WindowPattern)frmMain.GetCurrentPattern(WindowPattern.Pattern);
                p.SetWindowVisualState(WindowVisualState.Normal);
                frmMain.SetFocus();
                // 關閉程式
                System.Windows.Application.Current.Shutdown();
                return;
            }

            IConfigurationBuilder builder = new ConfigurationBuilder()
             .SetBasePath(Directory.GetCurrentDirectory())
             .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            //_configuration = builder.Build();

            // 20220731
            // 使用BIG5的宣告
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            LogHelper.Instance.Info("************************************************************************************************************");
            LogHelper.Instance.Info("Logging in!");
        }
        protected override void OnExit(ExitEventArgs e)
        {
            LogHelper.Instance.Info("Logging out!");
            LogHelper.Instance.Info("************************************************************************************************************");
            LogHelper.Instance.Info(" ");
            base.OnExit(e);
        }

    }
}
