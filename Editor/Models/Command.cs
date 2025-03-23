using Newtonsoft.Json.Linq;

namespace UnityMCP.Editor.Models
{
    /// <summary>
    /// Represents a command received from the MCP client
    /// </summary>
    public class Command
    {
        /// <summary>
        /// The type of command to execute
        /// </summary>
        public string type { get; set; }

        /// <summary>
        /// The parameters for the command
        /// </summary>
        public JObject @params { get; set; }
    }
}