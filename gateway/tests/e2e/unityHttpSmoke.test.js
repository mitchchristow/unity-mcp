const { test } = require("node:test");
const assert = require("node:assert/strict");
const axios = require("axios");

const RPC_URL = process.env.UNITY_RPC_URL || "http://localhost:17890/mcp/rpc";

async function callUnityRpc(method, params = {}) {
  const response = await axios.post(
    RPC_URL,
    {
      jsonrpc: "2.0",
      method,
      params,
      id: 1,
    },
    {
      headers: { "Content-Type": "application/json" },
      timeout: 10000,
      validateStatus: () => true,
    }
  );

  assert.equal(response.status, 200, `HTTP ${response.status} from Unity RPC endpoint`);
  assert.ok(response.data, "Response body missing");

  if (response.data.error) {
    throw new Error(
      `RPC '${method}' failed (${response.data.error.code}): ${response.data.error.message}`
    );
  }

  return response.data.result;
}

async function isUnityReachable() {
  try {
    await callUnityRpc("unity.get_project_info");
    return true;
  }
  catch {
    return false;
  }
}

test("unity.get_project_info returns editor metadata", async (t) => {
  if (process.env.UNITY_E2E !== "1") {
    const reachable = await isUnityReachable();
    if (!reachable) {
      t.skip("Unity MCP server not running (open Unity editor or set UNITY_E2E=1)");
      return;
    }
  }

  const result = await callUnityRpc("unity.get_project_info");
  assert.ok(result.unityVersion, "unityVersion missing");
  assert.ok(result.projectPath, "projectPath missing");
  assert.equal(typeof result.isPlaying, "boolean");
  assert.equal(typeof result.isCompiling, "boolean");
});

test("unity.list_objects returns objects array", async (t) => {
  if (process.env.UNITY_E2E !== "1") {
    const reachable = await isUnityReachable();
    if (!reachable) {
      t.skip("Unity MCP server not running (open Unity editor or set UNITY_E2E=1)");
      return;
    }
  }

  const result = await callUnityRpc("unity.list_objects");
  assert.ok(Array.isArray(result.objects), "objects should be an array");
});
