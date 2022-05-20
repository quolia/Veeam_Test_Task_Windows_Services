using System;
using System.Collections.Generic;
using System.ServiceProcess;
using System.Threading;
using Microsoft.Win32;

namespace WSO
{
    /// <summary>
    /// Implementation of the thread to obtain Windows services data.
    /// </summary>
    public class ServiceDataThread
    {
        public delegate void ServiceDataThreadExceptionHandler(Exception exception, out bool handled);

        private Thread _servicesDataThread;
        private Action<IList<ServiceModel>> _onServicesDataRecieved;
        private ServiceDataThreadExceptionHandler _onException;
        private int _updatePeriodMilliseconds;
        private volatile bool _isStop;
        private bool _skipRegistryError;

        public void Start(Action<IList<ServiceModel>> onServicesDataRecieved, int updatePeriodMilliseconds, ServiceDataThreadExceptionHandler onException)
        {
            _onException = onException;

            Stop();

            try
            {
                _onServicesDataRecieved = onServicesDataRecieved ?? throw new ArgumentNullException(nameof(onServicesDataRecieved));
                _updatePeriodMilliseconds = updatePeriodMilliseconds;
                _servicesDataThread = new Thread(new ThreadStart(GetServicesDataSafe));
                _servicesDataThread.Start();
            }
            catch (Exception ex)
            {
                if (!HandleException(ex))
                {
                    throw;
                }
            }
        }

        public void Stop()
        {
            if (_servicesDataThread == null)
            {
                return;
            }

            _isStop = true;

            try
            {
                bool isStopped =_servicesDataThread.Join(5000);
                if (!isStopped)
                {
                    _servicesDataThread.Interrupt();
                }
            }
            catch (Exception ex)
            {
                if (!HandleException(ex))
                {
                    throw;
                }
            }
            finally
            {
                _servicesDataThread = null;
            }
        }

        private void GetServicesDataSafe()
        {
            try
            {
                GetServicesData();
            }
            catch (Exception ex)
            {
                if (!HandleException(ex))
                {
                    throw;
                }
            }
        }

        private void GetServicesData()
        {
            _skipRegistryError = false;

            while (!_isStop)
            {
                List<ServiceModel> models = new();
                ServiceController[] services = ServiceController.GetServices();
                RegistryKey localKey = Registry.LocalMachine;

                foreach (ServiceController service in services)
                {
                    string account = null;
                    string keyName = @"SYSTEM\CurrentControlSet\Services\" + service.ServiceName;

                    try
                    {
                        using RegistryKey key = localKey.OpenSubKey(keyName);
                        if (key == null)
                        {
                            HandleServiceDataException(null, keyName);
                        }
                        else
                        {
                            account = (string)key.GetValue("ObjectName");
                        }
                    }
                    catch (Exception ex)
                    {
                        HandleServiceDataException(ex, keyName);
                    }

                    models.Add(new ServiceModel(service, account));
                }

                try
                {
                    _onServicesDataRecieved(models);
                }
                catch (Exception ex)
                {
                    throw new ServiceDataThreadException("Cannot handle services data error.", ex);
                }

                int msec = 0;
                while (msec < _updatePeriodMilliseconds)
                {
                    msec += 100;
                    Thread.Sleep(100);
                    if (_isStop)
                    {
                        return;
                    }
                }
            }
        }

        private bool HandleException(Exception exception)
        {
            if (_onException == null)
            {
                return false;
            }
            
            try
            {
                _onException(exception, out bool handled);

                return handled;
            }
            catch
            {
                return false;
            }
        }

        private void HandleServiceDataException(Exception exception, string keyName)
        {
            if (_skipRegistryError)
            {
                return;
            }

            ServiceDataThreadException ex = new("Cannot obtain service account from registry.", exception);
            ex.Data.Add("Registry key", keyName ?? "null");

            _skipRegistryError = HandleException(ex) ? ex.Skip : throw new ServiceDataThreadException("Cannot handle service data exception.", ex);
        }
    }

    /// <summary>
    /// Services data exception.
    /// </summary>
    public class ServiceDataThreadException : Exception
    {
        public bool Skip { get; set; }

        public ServiceDataThreadException()
        {
        }

        public ServiceDataThreadException(string message)
            : base(message)
        {
        }

        public ServiceDataThreadException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
