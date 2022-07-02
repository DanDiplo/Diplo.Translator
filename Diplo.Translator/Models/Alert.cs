using Newtonsoft.Json.Converters;
using System.Text.Json.Serialization;

namespace Diplo.Translator.Models
{
    /// <summary>
    /// Represents an alert message
    /// </summary>
    public class Alert
    {
        public Alert(AlertType alertType, string message)
        {
            this.AlertType = alertType;
            this.Message = message;
        }

        public Alert(string message)
        {
            this.AlertType = AlertType.Info;
            this.Message = message;
        }

        [Newtonsoft.Json.JsonProperty("message")]
        [JsonPropertyName("message")]
        public string Message { get; set; }


        [Newtonsoft.Json.JsonProperty("alertType")]
        [JsonPropertyName("alertType")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        [Newtonsoft.Json.JsonConverter(typeof(StringEnumConverter))]
        public AlertType AlertType { get; set; }
    }

    public enum AlertType
    {
        Info,
        Success,
        Warning,
        Error
    }
}
