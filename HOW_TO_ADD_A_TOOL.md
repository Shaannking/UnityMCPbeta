# Unity MCP Server

This directory contains the Unity MCP Server implementation, which provides a bridge between Python and Unity Editor functionality.

## Adding New Tools

To add a new tool to the MCP Server, follow these steps:

### 1. Create the C# Command Handler

First, create or modify a command handler in the `Editor/Commands` directory:

```csharp
// Example: NewCommandHandler.cs
public static class NewCommandHandler
{
    public static object HandleNewCommand(JObject @params)
    {
        // Extract parameters
        string param1 = (string)@params["param1"];
        int param2 = (int)@params["param2"];

        // Implement the Unity-side functionality
        // ...

        // Return results
        return new {
            message = "Operation successful",
            result = someResult
        };
    }
}
```

### 2. Register the Command Handler

Add your command handler to the `CommandRegistry.cs` in the `Editor/Commands` directory:

```csharp
public static class CommandRegistry
{
    private static readonly Dictionary<string, Func<JObject, object>> _handlers = new()
    {
        // ... existing handlers ...
        { "NEW_COMMAND", NewCommandHandler.HandleNewCommand }
    };
}
```

### 3. Create the Python Tool

Add your tool to the appropriate Python module in the `Python/tools` directory:

```python
@mcp.tool()
def new_tool(
    ctx: Context,
    param1: str,
    param2: int
) -> str:
    """Description of what the tool does.

    Args:
        ctx: The MCP context
        param1: Description of param1
        param2: Description of param2

    Returns:
        str: Success message or error details
    """
    try:
        response = get_unity_connection().send_command("NEW_COMMAND", {
            "param1": param1,
            "param2": param2
        })
        return response.get("message", "Operation successful")
    except Exception as e:
        return f"Error executing operation: {str(e)}"
```

### 4. Register the Tool

Ensure your tool is registered in the appropriate registration function:

```python
# In Python/tools/__init__.py
def register_all_tools(mcp):
    register_scene_tools(mcp)
    register_script_tools(mcp)
    register_material_tools(mcp)
    # Add your new tool registration if needed
```

### 5. Update the Prompt

If your tool should be exposed to users, update the prompt in `Python/server.py`:

```python
@mcp.prompt()
def asset_creation_strategy() -> str:
    return (
        "Follow these Unity best practices:\n\n"
        "1. **Your Category**:\n"
        "   - Use `new_tool(param1, param2)` to do something\n"
        # ... rest of the prompt ...
    )
```

## Best Practices

1. **Existence Checking**:

   - ALWAYS check if objects, scripts, assets, or materials exist before creating or updating them
   - Use appropriate search tools (`find_objects_by_name`, `list_scripts`, `get_asset_list`) to verify existence
   - Handle both cases: creation when it doesn't exist and updating when it does
   - Implement proper error handling when an expected resource is not found

2. **Error Handling**:

   - Always include try-catch blocks in Python tools
   - Validate parameters in C# handlers
   - Return meaningful error messages

3. **Documentation**:

   - Add XML documentation to C# handlers
   - Include detailed docstrings in Python tools
   - Update the prompt with clear usage instructions

4. **Parameter Validation**:

   - Validate parameters on both Python and C# sides
   - Use appropriate types (str, int, float, List, etc.)
   - Provide default values when appropriate

5. **Testing**:

   - Test the tool in both Unity Editor and Python environments
   - Verify error handling works as expected
   - Check that the tool integrates well with existing functionality

6. **Code Organization**:
   - Group related tools in appropriate handler classes
   - Keep tools focused and single-purpose
   - Follow existing naming conventions

## Example Implementation

Here's a complete example of adding a new tool:

1. **C# Handler** (`Editor/Commands/ExampleHandler.cs`):

```csharp
public static class ExampleHandler
{
    public static object CreatePrefab(JObject @params)
    {
        string prefabName = (string)@params["prefab_name"];
        string template = (string)@params["template"];
        bool overwrite = @params["overwrite"] != null ? (bool)@params["overwrite"] : false;

        // Check if the prefab already exists
        string prefabPath = $"Assets/Prefabs/{prefabName}.prefab";
        bool prefabExists = System.IO.File.Exists(prefabPath);

        if (prefabExists && !overwrite)
        {
            return new {
                message = $"Prefab already exists: {prefabName}. Use overwrite=true to replace it.",
                exists = true,
                path = prefabPath
            };
        }

        // Implementation
        GameObject prefab = new GameObject(prefabName);
        // ... setup prefab ...

        return new {
            message = prefabExists ? $"Updated prefab: {prefabName}" : $"Created prefab: {prefabName}",
            exists = prefabExists,
            path = prefabPath
        };
    }
}
```

2. **Python Tool** (`Python/tools/example_tools.py`):

```python
@mcp.tool()
def create_prefab(
    ctx: Context,
    prefab_name: str,
    template: str = "default",
    overwrite: bool = False
) -> str:
    """Create a new prefab in the project or update if it exists.

    Args:
        ctx: The MCP context
        prefab_name: Name for the new prefab
        template: Template to use (default: "default")
        overwrite: Whether to overwrite an existing prefab (default: False)

    Returns:
        str: Success message or error details
    """
    try:
        # First check if the prefab already exists
        assets = get_unity_connection().send_command("GET_ASSET_LIST", {
            "type": "Prefab",
            "search_pattern": prefab_name,
            "folder": "Assets/Prefabs"
        }).get("assets", [])
        
        prefab_exists = any(asset.get("name") == prefab_name for asset in assets)
        
        if prefab_exists and not overwrite:
            return f"Prefab '{prefab_name}' already exists. Use overwrite=True to replace it."
            
        # Create or update the prefab
        response = get_unity_connection().send_command("CREATE_PREFAB", {
            "prefab_name": prefab_name,
            "template": template,
            "overwrite": overwrite
        })
        
        return response.get("message", "Prefab operation completed successfully")
    except Exception as e:
        return f"Error with prefab operation: {str(e)}"
```

3. **Update Prompt**:

```python
"1. **Prefab Management**:\n"
"   - ALWAYS check if a prefab exists before creating it\n"
"   - Create or update prefabs with `create_prefab(prefab_name, template, overwrite=False)`\n"
```

## Troubleshooting

If you encounter issues:

1. Check the Unity Console for C# errors
2. Verify the command name matches between Python and C#
3. Ensure all parameters are properly serialized
4. Check the Python logs for connection issues
5. Verify the tool is properly registered in both environments
