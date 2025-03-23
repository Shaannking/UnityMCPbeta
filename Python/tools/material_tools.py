from mcp.server.fastmcp import FastMCP, Context
from typing import List, Optional
from unity_connection import get_unity_connection

def register_material_tools(mcp: FastMCP):
    """Register all material-related tools with the MCP server."""
    
    @mcp.tool()
    def set_material(
        ctx: Context,
        object_name: str,
        material_name: Optional[str] = None,
        color: Optional[List[float]] = None,
        create_if_missing: bool = True
    ) -> str:
        """
        Apply or create a material for a game object. If material_name is provided,
        the material will be saved as a shared asset in the Materials folder.
        
        Args:
            object_name: Target game object.
            material_name: Optional material name. If provided, creates/uses a shared material asset.
            color: Optional [R, G, B] or [R, G, B, A] values (0.0-1.0).
            create_if_missing: Whether to create the material if it doesn't exist (default: True).
            
        Returns:
            str: Status message indicating success or failure.
        """
        try:
            unity = get_unity_connection()
            
            # Check if the object exists
            object_response = unity.send_command("FIND_OBJECTS_BY_NAME", {
                "name": object_name
            })
            
            objects = object_response.get("objects", [])
            if not objects:
                return f"GameObject '{object_name}' not found in the scene."
            
            # If a material name is specified, check if it exists
            if material_name:
                material_assets = unity.send_command("GET_ASSET_LIST", {
                    "type": "Material",
                    "search_pattern": material_name,
                    "folder": "Assets/Materials"
                }).get("assets", [])
                
                material_exists = any(asset.get("name") == material_name for asset in material_assets)
                
                if not material_exists and not create_if_missing:
                    return f"Material '{material_name}' not found. Use create_if_missing=True to create it."
            
            # Validate color values if provided
            if color:
                # Check if color has the right number of components (RGB or RGBA)
                if not (len(color) == 3 or len(color) == 4):
                    return f"Error: Color must have 3 (RGB) or 4 (RGBA) components, but got {len(color)}."
                
                # Check if all color values are in the 0-1 range
                for i, value in enumerate(color):
                    if not isinstance(value, (int, float)):
                        return f"Error: Color component at index {i} is not a number."
                    
                    if value < 0.0 or value > 1.0:
                        channel = "RGBA"[i] if i < 4 else f"component {i}"
                        return f"Error: Color {channel} value must be in the range 0.0-1.0, but got {value}."
            
            # Set up parameters for the command
            params = {
                "object_name": object_name,
                "create_if_missing": create_if_missing
            }
            if material_name:
                params["material_name"] = material_name
            if color:
                params["color"] = color
                
            result = unity.send_command("SET_MATERIAL", params)
            material_name = result.get("material_name", "unknown")
            material_path = result.get("path")
            
            if material_path:
                return f"Applied shared material '{material_name}' to {object_name} (saved at {material_path})"
            else:
                return f"Applied instance material '{material_name}' to {object_name}"
                
        except Exception as e:
            return f"Error setting material: {str(e)}" 