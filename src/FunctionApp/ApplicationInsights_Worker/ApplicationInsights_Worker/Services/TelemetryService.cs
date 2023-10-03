using ApplicationInsights_Worker.Repositories;
using System;
using System.Text;
using System.Threading.Tasks;

namespace ApplicationInsights_Worker.Services
{
    /// <summary>ITelemetryService </summary>
    public interface ITelemetryService : IDisposable
    {
        /// <summary>
        /// Alpha
        /// </summary>
        /// <param name="name"></param>
        void AlphaProcess(string name);

        /// <summary>
        /// Bravo
        /// </summary>
        /// <returns></returns>
        Task BravoProcessAsync();
    }

    /// <summary>TelemetryService </summary>
    public class TelemetryService : ITelemetryService
    {
        #region Variable・Const
        /// <summary>Logger</summary>
        private static MetricLogger Logger = MetricLogger.GlobalInstance;
        /// <summary>StorageProvider</summary>
        private readonly IStorageProvider StorageProvider = null;
        /// <summary>ApplicationInsightProvider</summary>
        private readonly IApplicationInsightsProvider ApplicationInsightsProvider;
        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="storageProvider">StorageProvider</param>
        /// <param name="applicationInsightsProvider">ApplicationInsightProvider</param>
        /// <exception cref="ArgumentNullException"></exception>
        public TelemetryService(IStorageProvider storageProvider, IApplicationInsightsProvider applicationInsightsProvider) : base()
        {
            if (storageProvider == null)
                throw new ArgumentNullException(nameof(storageProvider));
            if (applicationInsightsProvider == null)
                throw new ArgumentNullException(nameof(applicationInsightsProvider));

            this.StorageProvider = storageProvider;
            this.ApplicationInsightsProvider = applicationInsightsProvider;
        }

        /// <summary>
        /// Alpha
        /// </summary>
        /// <param name="name"></param>
        public void AlphaProcess(string name)
        {
            for (int i = 0; i < 5; i++)
            {
                Logger.Info($"{BaseLogger.GetCurrentMethod()}_{name}_{i.ToString().PadLeft(4, '0')}");
            }
        }

        /// <summary>
        /// Bravo
        /// </summary>
        /// <returns></returns>
        public async Task BravoProcessAsync()
        {
            var dt = await this.ApplicationInsightsProvider.GetAppInsightDataTableAsync();
            var csv = this.ApplicationInsightsProvider.ConvertDataTableToCsvString(dt);

            var containerName = Environment.GetEnvironmentVariable("BLOB_CONTAINER_NAME_OPE_LOG");
            var csvBytes = Encoding.UTF8.GetBytes(csv);

            var targetTodayDirectory = DateTime.Now.ToString("yyyyMMdd");
            var outFileName = $"Log_{DateTime.Now.ToString("yyyyMMddHHmmss")}.csv";
            var outFilePath = $"{targetTodayDirectory}/{outFileName}";

            await this.StorageProvider.UploadFileToBlobAsync(containerName, outFilePath, csvBytes);
        }

        #region Dispose
        private bool IsDisposed = false;

        /// <summary>
        /// リソースの開放
        /// </summary>
        public void Dispose()
        {
            if (this.IsDisposed) return;

            this.IsDisposed = true;
        }
        #endregion
    }
}
