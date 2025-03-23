using System;
using Newtonsoft.Json;

namespace UnityMCP.Editor.Models
{
    [Serializable]
    public class MCPConfigServers
    {
        [JsonProperty("unityMCP")]
        public MCPConfigServer unityMCP;
    }
}
