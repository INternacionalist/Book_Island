using System;
using System.Windows;
using System.Windows.Threading;

namespace книжный_остров
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            DispatcherUnhandledException += OnDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

            try
            {
                WpfAppBookStore.DatabaseService.EnsureInfrastructure();
            }
            catch (Exception ex)
            {
                WpfAppBookStore.DbLogger.LogError("App.OnStartup", ex);
            }
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            WpfAppBookStore.DbLogger.LogError("UI", e.Exception);
            e.Handled = true;
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                WpfAppBookStore.DbLogger.LogError("AppDomain", ex);
            }
        }
    }
}
