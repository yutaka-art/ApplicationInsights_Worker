using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Data;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ApplicationInsights_Worker.Repositories
{
    #region IApplicationInsightsProvider
    /// <summary>IApplicationInsightsProvider</summary>
    public interface IApplicationInsightsProvider : IDisposable
    {
        /// <summary>Obtain logs from Application Insight and return them in DataTable format</summary>
        /// <returns></returns>
        Task<DataTable> GetAppInsightDataTableAsync();

        /// <summary>Data table to CSV</summary>
        /// <param name="dt"></param>
        /// <param name="writeHeader"></param>
        /// <returns></returns>
        string ConvertDataTableToCsvString(DataTable dt, bool writeHeader = true);
    }
    #endregion

    #region ApplicationInsightsProvider
    /// <summary>ApplicationInsightsProvider</summary>
    public class ApplicationInsightsProvider : IApplicationInsightsProvider
    {
        #region Variable・Const
        /// <summary>Logger</summary>
        private static MetricLogger Logger = MetricLogger.GlobalInstance;
        /// <summary>HttpClient</summary>
        private readonly HttpClient HttpClient;
        #endregion

        #region Constructor
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="httpClient">HttpClient</param>
        /// <exception cref="ArgumentNullException"></exception>
        public ApplicationInsightsProvider(HttpClient httpClient) : base()
        {
            if (httpClient == null)
                throw new ArgumentNullException(nameof(httpClient));

            HttpClient = httpClient;
        }
        #endregion

        #region Method
        /// <summary>Obtain logs from Application Insight and return them in DataTable format</summary>
        /// <returns></returns>
        public async Task<DataTable> GetAppInsightDataTableAsync()
        {
            return await GetTargetDataTableAsync();
        }

        /// <summary>
        /// Fetches data via Kusto Query Language (KQL) and stores it in a DataTable.
        /// </summary>
        /// <returns>Data table with raw data.</returns>
        private async Task<DataTable> GetTargetDataTableAsync()
        {
            var appId = Environment.GetEnvironmentVariable("KQL_APPLICATION_ID");
            var apiKey = Environment.GetEnvironmentVariable("KQL_APPLICATION_API_KEY");
            var query = GetTargetKQL().ToString();

            var url = $"https://api.applicationinsights.io/v1/apps/{appId}/query?query={query}";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("x-api-key", apiKey);

            var response = await HttpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            var responseJson = JsonConvert.DeserializeObject<JToken>(responseContent);
            var layers = responseJson.Value<JArray>("tables");

            var dataTable = new DataTable();

            foreach (var item in layers.Children())
            {
                ExtractColumns(item, dataTable);
                PopulateRows(item, dataTable);
            }

            return dataTable;
        }

        private void ExtractColumns(JToken item, DataTable dataTable)
        {
            var columns = item["columns"];

            foreach (JObject column in columns)
            {
                var colName = column["name"].ToString();
                dataTable.Columns.Add(colName, typeof(string)).AllowDBNull = true;
            }
        }

        private void PopulateRows(JToken item, DataTable dataTable)
        {
            var rows = item["rows"];

            foreach (var row in rows)
            {
                var newRow = dataTable.NewRow();

                for (int idx = 0; idx < dataTable.Columns.Count; idx++)
                {
                    var columnName = dataTable.Columns[idx].ToString();
                    var value = row[idx];

                    if (columnName.Equals("timestamp"))
                    {
                        var jst = TimeZoneInfo.FindSystemTimeZoneById("Tokyo Standard Time");
                        var utcDateTime = DateTime.Parse(value.ToString());
                        newRow[columnName] = TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, jst);
                    }
                    else
                    {
                        newRow[columnName] = value;
                    }
                }

                dataTable.Rows.Add(newRow);
            }
        }

        /// <summary>
        /// KQL Build
        /// </summary>
        /// <returns>KQL</returns>
        private StringBuilder GetTargetKQL()
        {
            var result = new StringBuilder();

            var queryMain = Environment.GetEnvironmentVariable("KQL_MAIN_QUERY_REGION");
            var queryOrder = Environment.GetEnvironmentVariable("KQL_ORDER_BY_REGION");
            var queryWhere = Environment.GetEnvironmentVariable("KQL_WHERE_REGION");

            var targetToday = DateTime.Now;
            var targetFrom = targetToday.AddMonths(int.Parse(Environment.GetEnvironmentVariable("KQL_WHERE_REGION_TIME_SPAN"))).ToString("yyyy-MM-dd 00:00:00");
            var targetTo = targetToday.ToString("yyyy-MM-dd 23:59:59");

            queryWhere = queryWhere.Replace("TARGET_FROM", targetFrom);
            queryWhere = queryWhere.Replace("TARGET_TO", targetTo);

            result.Append(queryMain);
            result.Append(queryOrder);
            result.Append(queryWhere);

            return result;
        }

        /// <summary>Converts a DataTable to a CSV formatted string.</summary>
        /// <param name="dt">The DataTable to convert.</param>
        /// <param name="writeHeader">Whether to include headers in the CSV output.</param>
        /// <returns>The CSV formatted string.</returns>
        public string ConvertDataTableToCsvString(DataTable dt, bool writeHeader = true)
        {
            var result = new StringBuilder();

            int colCount = dt.Columns.Count;
            int lastColIndex = colCount - 1;

            if (writeHeader)
            {
                for (int i = 0; i < colCount; i++)
                {
                    string field = EncloseDoubleQuotesIfNeed(dt.Columns[i].Caption);
                    result.Append(field);
                    if (lastColIndex > i)
                    {
                        result.Append(",");
                    }
                }
                result.AppendLine();
            }

            foreach (DataRow row in dt.Rows)
            {
                for (int i = 0; i < colCount; i++)
                {
                    string field = row[i].ToString();
                    if (!string.IsNullOrEmpty(field))
                    {
                        field = EncloseDoubleQuotes(field);
                    }

                    result.Append(field);
                    if (lastColIndex > i)
                    {
                        result.Append(",");
                    }
                }

                result.AppendLine();
            }

            return result.ToString();
        }

        private string EncloseDoubleQuotesIfNeed(string field)
        {
            if (NeedEncloseDoubleQuotes(field))
            {
                return EncloseDoubleQuotes(field);
            }
            return field;
        }

        private static string EncloseDoubleQuotes(string field)
        {
            if (field.Contains("\""))
            {
                field = field.Replace("\"", "\"\"");
            }
            return $"\"{field}\"";
        }

        private bool NeedEncloseDoubleQuotes(string field)
        {
            return field.Contains("\"") ||
                field.Contains(",") ||
                field.Contains("\r") ||
                field.Contains("\n") ||
                field.StartsWith(" ") ||
                field.StartsWith("\t") ||
                field.EndsWith(" ") ||
                field.EndsWith("\t");
        }
        #endregion

        #region Dispose
        private bool IsDisposed = false;

        /// <summary>
        /// Dispose of resources
        /// </summary>
        public void Dispose()
        {
            if (IsDisposed) return;

            IsDisposed = true;
        }
        #endregion
    }
    #endregion
}
