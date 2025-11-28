---
sidebar_position: 2
---

# Cursor integration

To use Unity MCP with Cursor:

1. **Generate Manifest**:
   In Unity, go to `Tools > MCP > Generate IDE Manifests`.
   This creates `mcp-manifest.json` in your project root.

2. **Configure Cursor**:
   Add the following to your `.cursor/cursor.json` or project settings:

   ```json
   {
     "mcpServers": {
       "unity": {
         "command": "curl",
         "args": [
           "-X", "POST",
           "-H", "Content-Type: application/json",
           "-d", "@-",
           "http://localhost:17890/mcp/rpc"
         ]
       }
     }
   }
   ```

3. **Use It**:
   Ask Cursor: "Create a cube and add a Rigidbody to it."
