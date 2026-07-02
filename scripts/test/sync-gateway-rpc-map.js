#!/usr/bin/env node
/**
 * Regenerates gateway/tests/fixtures/gateway-rpc-map.json from toolResolver + fixtures.
 * Run: node scripts/test/sync-gateway-rpc-map.js
 */
const fs = require("node:fs");
const path = require("node:path");
const { resolveToolCall } = require("../../gateway/lib/toolResolver");

const repoRoot = path.resolve(__dirname, "../..");
const toolsPath = path.join(repoRoot, "gateway/tests/fixtures/gateway-tools.json");
const minimalPath = path.join(repoRoot, "gateway/tests/fixtures/tool-minimal-args.json");
const outPath = path.join(repoRoot, "gateway/tests/fixtures/gateway-rpc-map.json");

const tools = JSON.parse(fs.readFileSync(toolsPath, "utf8"));
const minimalArgs = JSON.parse(fs.readFileSync(minimalPath, "utf8"));

const entries = [];
for (const tool of tools) {
  if (tool === "unity_batch") continue;
  const args = minimalArgs[tool] ?? {};
  const { method } = resolveToolCall(tool, args);
  entries.push({ tool, method, args });
}

const rpcMethods = [...new Set(entries.map((e) => e.method))].sort();
const payload = {
  generatedAt: new Date().toISOString(),
  toolCount: tools.length,
  mappedToolCount: entries.length,
  rpcMethodCount: rpcMethods.length,
  entries,
  rpcMethods,
};

fs.writeFileSync(outPath, JSON.stringify(payload, null, 2) + "\n", "utf8");
console.log(`Wrote ${outPath} (${entries.length} tools -> ${rpcMethods.length} RPC methods)`);
