using System;
using System.ServiceProcess;
using System.Threading.Tasks;

namespace WSO
{
    /// <summary>
    /// Windows service data model.
    /// </summary>
    public class ServiceModel
    {
        private readonly ServiceController _service;

        public ServiceModel(ServiceController service, string account)
        {
            _service = service;
            Account = account;
        }

        public string Name => _service.ServiceName;

        public string DisplayName => _service.DisplayName;

        public ServiceControllerStatus Status => _service.Status;

        public string Account { get; }

        public Task ToggleService()
        {
            try
            {
                Task task = Task.Run(() =>
                {
                    try
                    {
                        if (_service.Status == ServiceControllerStatus.Running)
                        {
                            _service.Stop();
                            _service.WaitForStatus(ServiceControllerStatus.Stopped);
                        }
                        else
                        {
                            _service.Start();
                            _service.WaitForStatus(ServiceControllerStatus.Running);
                        }
                    }
                    catch (InvalidOperationException ex)
                    {
                        Logger.ShowException(ex, "Run the application in administrative mode.");
                    }
                    catch (Exception ex)
                    {
                        Logger.ShowException(ex, "Could not toggle the service.");
                    }
                });

                return task;
            }
            catch (Exception ex)
            {
                Logger.ShowException(ex, "Could not toggle the service.");
            }

            return null;
        }
    }
}
