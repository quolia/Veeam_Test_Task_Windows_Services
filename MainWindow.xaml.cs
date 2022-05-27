using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.ComponentModel;

namespace WSO
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml.
    /// </summary>
    public partial class MainWindow : Window
    {
        private ServiceDataThread _servicesDataThread;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Main_Loaded(object sender, RoutedEventArgs e)
        {
            Main.DataContext = new ViewModel();
            
            StartGettingServicesData();
        }

        private void Main_Closing(object sender, CancelEventArgs e)
        {
            StopGettingServicesData();
        }

        private void StartGettingServicesData()
        {
            StopGettingServicesData();

            _servicesDataThread = new();
            _servicesDataThread.Start(OnServicesDataRecieved, 1000, OnServiceDataThreadException);
        }

        private void StopGettingServicesData()
        {
            if (_servicesDataThread != null)
            {
                _servicesDataThread.Stop();
                _servicesDataThread = null;
            }
        }

        private void OnServiceDataThreadException(Exception exception, out bool handled)
        {
            if (exception is ServiceDataThreadException ex)
            {
                ex.Skip = Logger.ShowExceptionAndAskForSkip(ex, null);
            }
            else
            {
                Logger.ShowException(exception, "Cannnot obtain services data.");
            }

            handled = true;
        }

        private async void OnServicesDataRecieved(IList<ServiceModel> services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            ViewModel viewModel = null;

            await Dispatcher.InvokeAsync(() =>
            {
                viewModel = Main.DataContext as ViewModel;
            });

            if (viewModel == null)
            {
                throw new ArgumentException("Main window data context is not initialized.");
            }

            if (viewModel.Services == null)
            {
                List<ServiceViewModel> servicesVM = new();

                foreach (ServiceModel serviceModel in services)
                {
                    servicesVM.Add(new ServiceViewModel(serviceModel));
                }

                viewModel.Services = servicesVM;
            }
            else
            {
                List<ServiceViewModel> addedServices = new();

                int i = -1;
                foreach (ServiceModel serviceModel in services)
                {
                    ++i;

                    ServiceViewModel serviceVM = viewModel.Services.ElementAtOrDefault(i);
                    if (serviceVM == null)
                    {
                        addedServices.Add(new ServiceViewModel(serviceModel));
                    }
                    else
                    {
                        serviceVM.SetModel(serviceModel);
                    }
                }

                if (addedServices.Any())
                {
                    viewModel.Services = new List<ServiceViewModel>(viewModel.Services.Concat(addedServices));
                }
                else if (i + 1 < viewModel.Services.Count)
                {
                    viewModel.Services = new List<ServiceViewModel>(viewModel.Services.Take(i + 1));
                }
            }
        }
    }
}
