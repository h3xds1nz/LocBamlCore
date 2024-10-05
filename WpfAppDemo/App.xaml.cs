// Created 4th Oct 2024
// Amended 6th Oct 2024
// by h3xds1nz

using System.Globalization;
using System.Threading;
using System.Windows;

namespace WpfAppDemo
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // This allows you to test different translations

            // Thread.CurrentThread.CurrentUICulture = new CultureInfo("no-NO");

            base.OnStartup(e);
        }
    }

}
