using Newtonsoft.Json;

namespace ApplicationInsights_Worker.Models
{
    public class TelemetryServiceReturnModel
    {
        [JsonProperty("IsSucceed")]
        public bool IsSucceed { get; set; } = false;

        [JsonProperty("Exception")]
        public string Exception { get; set; } = "-";
    }
}