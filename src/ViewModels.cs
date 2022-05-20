using System.ComponentModel;
using System.Globalization;
using System.ServiceProcess;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

namespace WSO
{
    /// <summary>
    /// Base class to implement MVVM pattern.
    /// </summary>
    public class Notifier : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        protected void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Main window view model.
    /// </summary>
    public class ViewModel : Notifier
    {
        protected IList<ServiceViewModel> _services;
        public IList<ServiceViewModel> Services
        {
            get => _services;

            set
            {
                _services = value;
                NotifyPropertyChanged(nameof(Services));
            }
        }
    }

    /// <summary>
    /// Windows service view model.
    /// </summary>
    public class ServiceViewModel : Notifier
    {
        private ServiceModel _model;

        public ServiceViewModel(ServiceModel model)
        {
            SetModel(model);
        }

        public void SetModel(ServiceModel model)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));

            NotifyPropertyChanged(nameof(Name));
            NotifyPropertyChanged(nameof(DisplayName));
            NotifyPropertyChanged(nameof(Status));
            NotifyPropertyChanged(nameof(Account));
            NotifyPropertyChanged(nameof(StartStop));
            NotifyPropertyChanged(nameof(IsStartStopEnabled));
        }

        public async void ToggleService()
        {
            IsProcessing = true;

            Task task = _model.ToggleService();
            if (task != null)
            {
                await task;

                if (task.Exception != null)
                {
                    Logger.ShowException(task.Exception, "Could not toggle the service.");
                }
            }

            IsProcessing = false;

            SetModel(_model);
        }

        public string Name => _model.Name;

        public string DisplayName => _model.DisplayName;

        public string Status => _model.Status.ToString();

        public string Account => GetTranslatedAccount();

        public string StartStop => IsProcessing ? "Processing" : _model.Status == ServiceControllerStatus.Running ? "Stop" : "Start";

        public bool IsStartStopEnabled => !IsProcessing;

        private bool _isProcessing;
        protected bool IsProcessing
        {
            get => _isProcessing;

            set
            {
                _isProcessing = value;
                NotifyPropertyChanged(nameof(StartStop));
                NotifyPropertyChanged(nameof(IsStartStopEnabled));
            }
        }

        private string GetTranslatedAccount()
        {
            if (string.IsNullOrEmpty(_model.Account))
            {
                return "N/A";
            }

            string account = _model.Account.ToUpper(CultureInfo.InvariantCulture);

            return account switch
            {
                "NT AUTHORITY\\LOCALSERVICE" => "Local Service",
                "NT AUTHORITY\\NETWORKSERVICE" => "Network Service",
                "LOCALSYSTEM" => "Local System",
                _ => _model.Account,
            };
        }
    }
}
