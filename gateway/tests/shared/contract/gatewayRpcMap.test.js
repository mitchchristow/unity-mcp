const test = require("node:test");
const assert = require("node:assert/strict");
const fs = require("node:fs");
const path = require("node:path");
const { execFileSync } = require("node:child_process");

const repoRoot = path.resolve(__dirname, "../../../..");
const mapPath = path.join(repoRoot, "gateway/tests/fixtures/gateway-rpc-map.json");
const syncScript = path.join(repoRoot, "scripts/test/sync-gateway-rpc-map.js");

function stablePayload(json) {
  const data = JSON.parse(json);
  delete data.generatedAt;
  return JSON.stringify(data, null, 2) + "\n";
}

test("gateway-rpc-map.json is in sync with toolResolver", () => {
  const before = stablePayload(fs.readFileSync(mapPath, "utf8"));
  execFileSync(process.execPath, [syncScript], { cwd: repoRoot });
  const after = stablePayload(fs.readFileSync(mapPath, "utf8"));
  assert.equal(after, before, "Run: node scripts/test/sync-gateway-rpc-map.js and commit gateway-rpc-map.json");
});
