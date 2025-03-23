using System;
using Newtonsoft.Json;

namespace UnityMCP.Editor.Models
{
    [Serializable]
    public class MCPConfigServer
    {
        [JsonProperty("command")]
        public string command;

        [JsonProperty("args")]
        public string[] args;
    }
}
