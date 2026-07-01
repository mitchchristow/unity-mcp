const test = require("node:test");
const assert = require("node:assert/strict");
const fs = require("node:fs");
const path = require("node:path");
const { resolveToolCall } = require("../../../lib/toolResolver");

const tools = JSON.parse(
  fs.readFileSync(path.join(__dirname, "../../fixtures/gateway-tools.json"), "utf8")
);
const minimalArgs = JSON.parse(
  fs.readFileSync(path.join(__dirname, "../../fixtures/tool-minimal-args.json"), "utf8")
);

test("gateway tool manifest matches index.js tool count", () => {
  assert.ok(tools.length >= 80);
  assert.ok(tools.includes("unity_batch"));
});

for (const toolName of tools) {
  if (toolName === "unity_batch") {
    continue;
  }

  test(`resolveToolCall maps ${toolName} to unity.* RPC`, () => {
    const args = minimalArgs[toolName] ?? {};
    const { method, rpcParams } = resolveToolCall(toolName, args);
    assert.ok(method.startsWith("unity."), `Expected unity.* method, got ${method}`);
    assert.ok(rpcParams !== undefined);
  });
}

test("direct tool names use unity_ to unity. convention", () => {
  const { method } = resolveToolCall("unity_list_objects", {});
  assert.equal(method, "unity.list_objects");
});
