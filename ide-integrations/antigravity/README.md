# Antigravity IDE Setup

Currently, Antigravity requires MCP servers to be configured globally. It does not support project-specific configuration files like `.cursor/mcp.json` or `workspace.json` automatically.

## Setup Instructions

1.  **Locate your Project Path**:
    Copy the absolute path to this project folder.
    Example: `c:\Users\mitch\Documents\Projects\unity-mcp`

2.  **Open Global Config**:
    In Antigravity, go to **Agent Panel > ... > Manage MCP Servers > View raw config**.
    This opens your global `mcp_config.json`.

3.  **Add Server Entry**:
    Add the "unity" server configuration. You **MUST** use absolute paths for both the script and the `cwd` (Current Working Directory).

    ```json
    {
      "mcpServers": {
        "unity": {
          "command": "node",
          "args": ["<YOUR_ABSOLUTE_PATH>/gateway/index.js"],
          "cwd": "<YOUR_ABSOLUTE_PATH>"
        }
      }
    }
    ```

    **Example:**
    ```json
    {
      "mcpServers": {
        "unity": {
          "command": "node",
          "args": ["c:/Users/mitch/Documents/Projects/unity-mcp/gateway/index.js"],
          "cwd": "c:/Users/mitch/Documents/Projects/unity-mcp"
        }
      }
    }
    ```

4.  **Restart**:
    Restart the IDE to apply the changes.

## Troubleshooting

*   **EOF Error**: This usually means the path is incorrect or relative. Ensure you are using absolute paths (e.g., `c:/...` not `./...`).
*   **Connection Refused**: Ensure the Unity Editor is running and the scene is loaded.
