using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace ApplicationInsights_Worker.Repositories
{
    #region IMetricLogger
    /// <summary>
    /// ApplicationInsights base interface
    /// </summary>
    public interface IMetricLogger
    {
        void Error(int code, Exception exception);

        void Error(int code, string message, params object[] parameters);

        void Warning(int code, Exception exception);

        void Warning(int code, string message, params object[] parameters);

        void Info(string message, params object[] parameters);

        void Verbose(string message, params object[] parameters);

        void Metric(string metricName, object value);

        void Metric(string metricName, string L1MetricName, object value);

        void Metric(string metricName, string L1MetricName, string L2MetricName, object value);

        void Flush();

        void Event(string message);
    }
    #endregion

    /// <summary>
    /// ApplicationInsights base
    /// </summary>
    public class MetricLogger : BaseLogger, IMetricLogger, IDisposable
    {
        private readonly string _appInsightsInstrumentationKey = null;
        private readonly int _maxTelemetryBufferCapacity = 1000;
        private readonly int _sendingIntervalS = 10;
        private readonly int _flushBufferTimeoutMS = 1000;

        private readonly Guid _instanceId = Guid.NewGuid();

        private List<Level> _levels = new List<Level>() { Level.ERROR, Level.WARNING, Level.INFO, Level.VERBOSE };

        private TelemetryClient telemetryClient;

        private static readonly object stLock = new object();
        static private MetricLogger _gInstance = null;

        public static MetricLogger GlobalInstance
        {
            get
            {
                Debug.WriteLine("MetricLogger GlobalInstance GET");
                if (_gInstance == null)
                {
                    lock (stLock)
                    {
                        if (_gInstance == null)
                        {
                            Debug.WriteLine("MetricLogger GlobalInstance Init");
                            _gInstance = new MetricLogger(); // Self init
                        }
                    }
                }
                return _gInstance;
            }
        }

        static MetricLogger()
        {
            Debug.WriteLine("MetricLogger_Ctor");
            AppDomain.CurrentDomain.ProcessExit += MetricLogger_Dtor;
        }

        static void MetricLogger_Dtor(object sender, EventArgs e)
        {
            Debug.WriteLine("MetricLogger_Dtor");
            if (_gInstance != null)
            {
                lock (stLock)
                {
                    if (_gInstance != null)
                    {
                        Debug.WriteLine("MetricLogger_Dtor Flushing");
                        _gInstance.Flush();
                    }
                }
            }
        }

        public MetricLogger()
        {
            _appInsightsInstrumentationKey = Environment.GetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY");
            InitLogAnalytics();
        }

        public MetricLogger(string instrumentationKey)
        {
            if (string.IsNullOrEmpty(instrumentationKey)) throw new ArgumentNullException(nameof(instrumentationKey));
            _appInsightsInstrumentationKey = instrumentationKey;
            InitLogAnalytics();
        }

        private void InitLogAnalytics()
        {
            if (string.IsNullOrEmpty(_appInsightsInstrumentationKey)) throw new ArgumentNullException(nameof(_appInsightsInstrumentationKey));

            // initialize memoryChannel
            var configuration = new TelemetryConfiguration(_appInsightsInstrumentationKey);
            var inMemCh = configuration.TelemetryChannel as Microsoft.ApplicationInsights.Channel.InMemoryChannel;
            inMemCh.MaxTelemetryBufferCapacity = _maxTelemetryBufferCapacity;
            inMemCh.SendingInterval = TimeSpan.FromSeconds(_sendingIntervalS);

            // initialize ApplicationInsights
            telemetryClient = new TelemetryClient(configuration);
            var entryAssembly = Assembly.GetEntryAssembly();
            telemetryClient.Context.Component.Version = entryAssembly.FullName;
            telemetryClient.Context.Cloud.RoleInstance = Environment.MachineName;

            // additinal propaties
            Verbose("Metric Logger initialized.");
        }

        /// <summary>
        /// exception handle
        /// </summary>
        /// <param name="msgType"></param>
        /// <param name="msgCode"></param>
        /// <param name="e"></param>
        protected override void ExceptionHandler(Level msgType, int msgCode, Exception e)
        {
            if (_levels.Contains(msgType))
                telemetryClient.TrackException(e);
        }

        /// <summary>
        /// Trace
        /// </summary>
        /// <param name="msgType">message types</param>
        /// <param name="msgCode">message codes</param>
        /// <param name="message">messafe</param>
        /// <param name="parameters">param</param>
        protected override void WriteLine(Level msgType, int msgCode, string message, params object[] parameters)
        {
            if (string.IsNullOrEmpty(message)) return;
            base.WriteLine(msgType, msgCode, message, parameters);

            // Write to log analytics trace
            if (_levels.Contains(msgType))
            {
                string formattedMessage = parameters == null || !parameters.Any() ? message : string.Format(message, parameters);
                telemetryClient.TrackTrace(string.Format(LOG_FORMAT, msgType.ToString(), msgCode, formattedMessage, NowAsString));
            }
        }

        /// <summary>
        /// Get new instance
        /// </summary>
        /// <returns>インスタンス</returns>
        internal static new MetricLogger GetNew()
        {
            return new MetricLogger();
        }

        /// <summary>
        /// Disposing of resources
        /// </summary>
        public void Dispose()
        {
            Flush();
        }

        /// <summary>
        /// Telemetry transmission
        /// </summary>
        /// <param name="wait">送信実行後に待機する場合はTrueを設定する</param>
        public void SendTelemetry(bool wait = false)
        {
            if (telemetryClient != null)
            {
                telemetryClient.Flush();
                if (wait)
                    System.Threading.Thread.Sleep(_flushBufferTimeoutMS);
            }
        }

        // -------- Metrics ----------------

        /// <summary>
        /// Register metrics
        /// </summary>
        /// <param name="metricName">メトリクス名</param>
        /// <param name="value">値</param>
        public void Metric(string metricName, object value)
        {
            if (string.IsNullOrEmpty(metricName)) throw new ArgumentNullException(nameof(metricName));
            if (value == null) throw new ArgumentNullException(nameof(value));

            telemetryClient.GetMetric(metricName).TrackValue(value);
        }

        /// <summary>
        /// Register metrics
        /// </summary>
        /// <param name="metricName">メトリクス名</param>
        /// <param name="L1MetricName">L1メトリクス名</param>
        /// <param name="value">値</param>
        public void Metric(string metricName, string L1MetricName, object value)
        {
            if (string.IsNullOrEmpty(metricName)) throw new ArgumentNullException(nameof(metricName));
            if (string.IsNullOrEmpty(L1MetricName)) throw new ArgumentNullException(nameof(L1MetricName));
            if (value == null) throw new ArgumentNullException(nameof(value));

            telemetryClient.GetMetric(metricName, L1MetricName).TrackValue(value, value.ToString());
        }

        /// <summary>
        /// Register metrics
        /// </summary>
        /// <param name="metricName">メトリクス名</param>
        /// <param name="L1MetricName">L1メトリクス名</param>
        /// <param name="L2MetricName">L2メトリクス名</param>
        /// <param name="value">値</param>
        public void Metric(string metricName, string L1MetricName, string L2MetricName, object value)
        {
            if (string.IsNullOrEmpty(metricName)) throw new ArgumentNullException(nameof(metricName));
            if (string.IsNullOrEmpty(L1MetricName)) throw new ArgumentNullException(nameof(L1MetricName));
            if (string.IsNullOrEmpty(L2MetricName)) throw new ArgumentNullException(nameof(L2MetricName));
            if (value == null) throw new ArgumentNullException(nameof(value));

            telemetryClient.GetMetric(metricName, L1MetricName, L2MetricName).TrackValue(value, value.ToString(), value.ToString());
        }

        /// <summary>
        /// Flush
        /// </summary>
        public void Flush()
        {
            SendTelemetry(true);
        }

        /// <summary>
        /// Event registration
        /// </summary>
        /// <param name="message">メッセージ</param>
        public void Event(string message)
        {
            if (string.IsNullOrEmpty(message)) throw new ArgumentNullException(nameof(message));

            var eventTelemetry = new Microsoft.ApplicationInsights.DataContracts.EventTelemetry(message)
            {
                Timestamp = DateTime.UtcNow
            };
            telemetryClient.TrackEvent(eventTelemetry);
        }
    }

}
