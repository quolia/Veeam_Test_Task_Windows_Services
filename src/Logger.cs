using System;
using System.IO;
using System.Text;
using System.Windows;

namespace WSO
{
    internal static class Logger
    {
        private static readonly object _locker = new();

        public static bool OnUnhandledException(Exception ex)
        {
            ShowException(ex, "The application will terminate.");
            return true;
        }

        public static void ShowException(Exception ex, string hint)
        {
            LogException(ex);

            string message = GetExceptionMessage(ex, hint);
            ShowErrorMessageBox(ex.GetType().ToString(), message);
        }

        public static bool ShowExceptionAndAskForSkip(Exception ex, string hint)
        {
            LogException(ex);

            string message = GetExceptionMessage(ex, hint);
            bool result = ShowErrorAndAskForSkip(ex.GetType().ToString(), message);

            return result;
        }

        private static void ShowErrorMessageBox(string caption, string message)
        {
            _ = MessageBox.Show(message, caption, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private static bool ShowErrorAndAskForSkip(string caption, string message)
        {
            StringBuilder builder = new();
            builder.AppendLine()
                .AppendLine("Skip next errors of this kind?");

            return MessageBoxResult.Yes == MessageBox.Show(message + builder.ToString(), caption, MessageBoxButton.YesNo, MessageBoxImage.Error);
        }

        public static void LogException(Exception ex)
        {
            try
            {
                string message = GetLogMessage(ex);

                lock (_locker)
                {
                    File.AppendAllText("log.txt", message);
                }
            }
            catch (Exception ex2)
            {
                ShowErrorMessageBox(ex2.GetType().ToString(), GetExceptionMessage(ex2, "Could not write to log file."));
            }
        }
        private static string GetLogMessage(Exception ex)
        {
            StringBuilder builder = new();
            builder.AppendLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
                .AppendLine(ex.GetType().ToString())
                .AppendLine(ex.Message);


            string additionalInfo = GetAdditionalInfo(ex);
            if (!string.IsNullOrEmpty(additionalInfo))
            {
                builder.AppendLine();
                builder.AppendLine(additionalInfo);
            }

            if (!string.IsNullOrEmpty(ex.StackTrace))
            {
                builder.AppendLine(ex.StackTrace);
            }

            while (ex.InnerException != null)
            {
                builder.AppendLine()
                    .AppendLine(ex.InnerException.GetType().ToString())
                    .AppendLine(ex.InnerException.Message);

                additionalInfo = GetAdditionalInfo(ex);
                if (!string.IsNullOrEmpty(additionalInfo))
                {
                    builder.AppendLine();
                    builder.AppendLine(additionalInfo);
                }

                if (!string.IsNullOrEmpty(ex.InnerException.StackTrace))
                {
                    builder.AppendLine(ex.InnerException.StackTrace);
                }

                ex = ex.InnerException;
            }

            builder.AppendLine();

            return builder.ToString();
        }

        private static string GetExceptionMessage(Exception ex, string hint)
        {
            StringBuilder builder = new();
            builder.AppendLine(ex.Message);

            string additionalInfo = GetAdditionalInfo(ex);
            if (!string.IsNullOrEmpty(additionalInfo))
            {
                builder.AppendLine();
                builder.AppendLine(additionalInfo);
            }

            while (ex.InnerException != null)
            {
                builder.AppendLine(ex.InnerException.Message);

                additionalInfo = GetAdditionalInfo(ex.InnerException);
                if (!string.IsNullOrEmpty(additionalInfo))
                {
                    builder.AppendLine(additionalInfo);
                }

                ex = ex.InnerException;
            }

            if (!string.IsNullOrEmpty(hint))
            {
                builder.AppendLine();
                builder.AppendLine(hint);
            }

            builder.AppendLine();
            builder.AppendLine("See log.txt for more detailed information.");

            return builder.ToString();
        }

        private static string GetAdditionalInfo(Exception ex)
        {
            if (ex.Data.Count == 0)
            {
                return string.Empty;
            }

            StringBuilder builder = new();

            foreach (object key in ex.Data.Keys)
            {
                if (ex.Data[key] != null)
                {
                    if (builder.Length == 0)
                    {
                        builder.AppendLine("Additional info:");
                    }

                    builder.AppendLine($"{key}: {ex.Data[key]}");
                }
            }

            return builder.ToString();
        }
    }
}
