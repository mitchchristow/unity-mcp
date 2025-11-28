import * as vscode from "vscode";
import axios from "axios";

export class McpClient {
  private baseUrl = "http://localhost:17890/mcp/rpc";
  private requestId = 1;

  constructor() {}

  public async sendRequest(method: string, params: any = {}): Promise<any> {
    try {
      const payload = {
        jsonrpc: "2.0",
        method: method,
        params: params,
        id: this.requestId++,
      };

      const response = await axios.post(this.baseUrl, payload);

      if (response.data.error) {
        throw new Error(response.data.error.message);
      }

      return response.data.result;
    } catch (error: any) {
      console.error(`MCP Error (${method}):`, error);
      throw error;
    }
  }

  public async checkConnection(): Promise<boolean> {
    try {
      await this.sendRequest("unity.list_scenes");
      return true;
    } catch {
      return false;
    }
  }
}
