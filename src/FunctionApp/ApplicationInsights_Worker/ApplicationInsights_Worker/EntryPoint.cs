using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ApplicationInsights_Worker.Services;
using ApplicationInsights_Worker.Models;

namespace ApplicationInsights_Worker
{
    public class EntryPoint
    {
        #region VariableÅEConst
        private readonly ITelemetryService TelemetryService;
        #endregion

        #region [EntryPoint]
        /// <summary>
        /// EntryPoint
        /// </summary>
        /// <param name="telemetryService"></param>
        public EntryPoint(ITelemetryService telemetryService)
        {
            this.TelemetryService = telemetryService;
        }
        #endregion

        [FunctionName("Alpha")]
        public IActionResult RunAlphaProcess(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            var name = req.Query["name"];
            var returnModel = new TelemetryServiceReturnModel();

            try
            {
                this.TelemetryService.AlphaProcessAsync(name);
                returnModel.IsSucceed = true;
            }
            catch (Exception ex)
            {
                returnModel.IsSucceed = false;
                returnModel.Exception = ex.ToString();
            }

            return new OkObjectResult(JsonConvert.SerializeObject(returnModel, Formatting.Indented));
        }

        [FunctionName("Bravo")]
        public async Task<IActionResult> RunBravoProcessAsync(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            var returnModel = new TelemetryServiceReturnModel();

            try
            {
                await this.TelemetryService.BravoProcessAsync();
                returnModel.IsSucceed = true;
            }
            catch (Exception ex)
            {
                returnModel.IsSucceed = false;
                returnModel.Exception = ex.ToString();
            }

            return new OkObjectResult(JsonConvert.SerializeObject(returnModel, Formatting.Indented));
        }
    }
}
