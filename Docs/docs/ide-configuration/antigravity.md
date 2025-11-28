---
sidebar_position: 1
---

# Antigravity integration

Antigravity can natively communicate with the Unity MCP server to perform complex agentic tasks.

## Configuration

Antigravity detects the `ide-integrations/antigravity/workspace.json` file.

1.  Ensure your Unity project is open and the MCP server is running.
2.  Open Antigravity.
3.  The agent will automatically have access to the `unity` toolset.

## Example Prompts

You can ask Antigravity to perform high-level tasks:

> "Create a new scene called 'Level1'."

> "Add a Plane at (0,0,0) and a Cube at (0,1,0). Make the Cube red."

> "Write a script 'Rotator.cs' that rotates the object 90 degrees per second and attach it to the Cube."

> "Enter play mode and tell me if there are any console errors."
