function interpolateArgs(args, results) {
  if (typeof args === "string") {
    const match = args.match(/^\$(\d+)(?:\.(.+))?$/);
    if (match) {
      const index = parseInt(match[1], 10);
      const fieldPath = match[2];
      if (index >= results.length) {
        throw new Error(`Interpolation reference $${index} is out of bounds (only ${results.length} result(s) so far)`);
      }
      const resultValue = results[index];
      if (!fieldPath) {
        if (resultValue !== null && typeof resultValue === "object" && "id" in resultValue) {
          return resultValue.id;
        }
        return resultValue;
      }
      const parts = fieldPath.split(".");
      let value = resultValue;
      for (const part of parts) {
        if (value === null || value === undefined || typeof value !== "object") {
          throw new Error(`Cannot access field "${part}" on ${JSON.stringify(value)} (interpolating "${args}")`);
        }
        value = value[part];
      }
      return value;
    }
    return args;
  }
  if (Array.isArray(args)) {
    return args.map(item => interpolateArgs(item, results));
  }
  if (args !== null && typeof args === "object") {
    const result = {};
    for (const [key, value] of Object.entries(args)) {
      result[key] = interpolateArgs(value, results);
    }
    return result;
  }
  return args;
}

function summarizeForAudit(value) {
  if (value == null) return value;
  try {
    const text = JSON.stringify(value);
    if (text.length <= 2000) return value;
    return { _truncated: true, preview: text.slice(0, 2000) };
  } catch {
    return String(value);
  }
}

module.exports = {
  interpolateArgs,
  summarizeForAudit,
};
