from typing import Optional, List
from mcp.server.fastmcp import FastMCP, Context
from unity_connection import get_unity_connection

def register_ui_tools(mcp: FastMCP):
    """Register all UI-related tools with the MCP server."""
    
    @mcp.tool()
    def create_ui_button(
        ctx: Context,
        name: str = "New Button",
        position: Optional[List[float]] = None
    ) -> str:
        """Create a UI Button in the scene.
        
        Args:
            ctx: The MCP context
            name: Name for the button (default: "New Button")
            position: Optional [x, y, z] position for the button
            
        Returns:
            str: Success message or error details
        """
        try:
            unity = get_unity_connection()
            
            params = {"name": name}
            if position:
                params["position"] = position
                
            response = unity.send_command("CREATE_UI_BUTTON", params)
            return response.get("message", "UI Button created successfully")
        except Exception as e:
            return f"Error creating UI Button: {str(e)}"
    
    @mcp.tool()
    def create_ui_text(
        ctx: Context,
        name: str = "New Text",
        content: str = "New Text",
        position: Optional[List[float]] = None
    ) -> str:
        """Create a UI Text element in the scene.
        
        Args:
            ctx: The MCP context
            name: Name for the text element (default: "New Text")
            content: Text content to display (default: "New Text")
            position: Optional [x, y, z] position for the text
            
        Returns:
            str: Success message or error details
        """
        try:
            unity = get_unity_connection()
            
            params = {
                "name": name,
                "content": content
            }
            if position:
                params["position"] = position
                
            response = unity.send_command("CREATE_UI_TEXT", params)
            return response.get("message", "UI Text created successfully")
        except Exception as e:
            return f"Error creating UI Text: {str(e)}"
