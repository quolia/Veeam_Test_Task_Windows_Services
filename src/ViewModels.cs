using System;
using System.Reflection;
using System.Windows.Input;
using System.ComponentModel;
using System.Globalization;
using System.ServiceProcess;
using System.Collections.Generic;

namespace WSO
{
    /// <summary>
    /// Base class to implement MVVM pattern.
    /// </summary>
    internal class Notifier : INotifyPropertyChanged
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
    internal class ViewModel : Notifier
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
    internal class ServiceViewModel : Notifier
    {
        private ServiceModel _model;

        public ServiceViewModel(ServiceModel model)
        {
            StartStopCommand = new StartStopCommandHandler(this);
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

        public string Name => _model.Name;

        public string DisplayName => _model.DisplayName;

        public string Status => _model.Status.ToString();

        public string Account => GetTranslatedAccount();

        public string StartStop => IsProcessing ? "Processing" : _model.Status == ServiceControllerStatus.Running ? "Stop" : "Start";

        public bool IsStartStopEnabled => !IsProcessing;

        private bool _isProcessing;
        public bool IsProcessing
        {
            get => _isProcessing;

            set
            {
                _isProcessing = value;
                NotifyPropertyChanged(nameof(StartStop));
                NotifyPropertyChanged(nameof(IsStartStopEnabled));
            }
        }

        public ICommand StartStopCommand { get; private set; }

        public async void ToggleService()
        {
            IsProcessing = true;

            var task = _model.ToggleService();
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

        internal class StartStopCommandHandler : ICommand
        {
            public event EventHandler CanExecuteChanged = delegate { };

            private readonly ServiceViewModel _viewModel;

            public StartStopCommandHandler(ServiceViewModel vm)
            {
                _viewModel = vm;
            }

            public bool CanExecute(object parameter) => true;

            public void Execute(object parameter)
            {
                if (_viewModel != null && CanExecute(parameter))
                {
                    _viewModel.ToggleService();
                }
            }
        }
    }
}
