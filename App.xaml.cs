using System;
using System.Windows;
using System.Windows.Threading;

namespace WSO
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        private void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            try
            {
                Logger.OnUnhandledException(e.Exception);
            }
            finally
            {
                TerminateApplication();
            }
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                Logger.OnUnhandledException((Exception)e.ExceptionObject);
            }
            finally
            {
                TerminateApplication();
            }
        }

        private void TerminateApplication()
        {
            try
            {
                if (Current != null)
                {
                    Current.Dispatcher.Invoke(() =>
                    {
                        try
                        {
                            Current.Shutdown();
                        }
                        catch (Exception ex)
                        {
                            Logger.LogException(ex);
                        }
                    });
                }
            }
            catch (Exception ex2)
            {
                Logger.LogException(ex2);
            }
        }
    }
}
