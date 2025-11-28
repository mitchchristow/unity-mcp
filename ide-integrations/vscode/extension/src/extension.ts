import * as vscode from "vscode";
import { McpClient } from "./mcpClient";

let client: McpClient;
let statusBarItem: vscode.StatusBarItem;

export function activate(context: vscode.ExtensionContext) {
  console.log("Unity MCP Extension is now active!");

  client = new McpClient();
  statusBarItem = vscode.window.createStatusBarItem(
    vscode.StatusBarAlignment.Right,
    100
  );
  statusBarItem.command = "unity.connect";
  context.subscriptions.push(statusBarItem);

  updateStatus(false);

  // Register Commands
  context.subscriptions.push(
    vscode.commands.registerCommand("unity.connect", async () => {
      const connected = await client.checkConnection();
      updateStatus(connected);
      if (connected) {
        vscode.window.showInformationMessage("Connected to Unity MCP Server");
      } else {
        vscode.window.showErrorMessage(
          "Failed to connect to Unity MCP Server. Is Unity running?"
        );
      }
    }),
    vscode.commands.registerCommand("unity.play", async () => {
      await client.sendRequest("unity.play");
    }),
    vscode.commands.registerCommand("unity.stop", async () => {
      await client.sendRequest("unity.stop");
    }),
    vscode.commands.registerCommand("unity.refresh", async () => {
      const scenes = await client.sendRequest("unity.list_scenes");
      console.log("Scenes:", scenes);
    })
  );

  // Auto-connect
  setTimeout(() => vscode.commands.executeCommand("unity.connect"), 1000);
}

function updateStatus(connected: boolean) {
  if (connected) {
    statusBarItem.text = "$(check) Unity Connected";
    statusBarItem.tooltip = "Connected to Unity MCP Server";
    statusBarItem.backgroundColor = undefined;
  } else {
    statusBarItem.text = "$(circle-slash) Unity Disconnected";
    statusBarItem.tooltip = "Click to retry connection";
    statusBarItem.backgroundColor = new vscode.ThemeColor(
      "statusBarItem.errorBackground"
    );
  }
  statusBarItem.show();
}

export function deactivate() {}
