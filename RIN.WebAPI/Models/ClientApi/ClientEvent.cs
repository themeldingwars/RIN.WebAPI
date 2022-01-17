using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace RIN.WebAPI.Models.ClientApi
{
    public class ClientEvent
    {
        public enum Events
        {
            [EnumMember(Value = "event")]
            Event,
            [EnumMember(Value = "error")]
            Error,
            [EnumMember(Value = "crash")]
            Crash
        }
        
        public struct Data
        {
            public string message { get; set; }
            public string source  { get; set; }
            public string data    { get; set; }
        }
        
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public Events @event { get; set; }
        public string action { get; set; }
        public Data   data   { get; set; }
    }
}