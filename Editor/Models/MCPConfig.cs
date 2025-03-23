using System;
using Newtonsoft.Json;

namespace UnityMCP.Editor.Models
{
    [Serializable]
    public class MCPConfig
    {
        [JsonProperty("mcpServers")]
        public MCPConfigServers mcpServers;
    }
}
