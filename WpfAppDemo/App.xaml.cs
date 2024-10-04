using System.Configuration;
using System.Data;
using System.Windows;

namespace WpfAppDemo
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
          //  Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("no-NO");
            base.OnStartup(e);
        }
    }

}
