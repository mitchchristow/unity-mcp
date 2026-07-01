const test = require("node:test");
const assert = require("node:assert/strict");
const { interpolateArgs, summarizeForAudit } = require("../../../lib/batchUtils");

test("interpolateArgs resolves $0.id from prior results", () => {
  const results = [{ id: 42, path: "Assets/Foo.mat" }];
  const args = { id: "$0.id", path: "$0.path" };
  assert.deepEqual(interpolateArgs(args, results), { id: 42, path: "Assets/Foo.mat" });
});

test("interpolateArgs throws when reference is out of bounds", () => {
  assert.throws(
    () => interpolateArgs({ id: "$1.id" }, [{ id: 1 }]),
    /out of bounds/
  );
});

test("interpolateArgs returns bare $0 as id field when present", () => {
  const results = [{ id: 99 }];
  assert.equal(interpolateArgs("$0", results), 99);
});

test("summarizeForAudit truncates large payloads", () => {
  const big = { data: "x".repeat(3000) };
  const summary = summarizeForAudit(big);
  assert.equal(summary._truncated, true);
  assert.ok(summary.preview.length <= 2000);
});
