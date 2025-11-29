const { Client } = require("@modelcontextprotocol/sdk/client/index.js");
const { StdioClientTransport } = require("@modelcontextprotocol/sdk/client/stdio.js");

async function main() {
  console.log("Starting Gateway Test...");

  const transport = new StdioClientTransport({
    command: "node",
    args: ["index.js"],
  });

  const client = new Client(
    {
      name: "test-client",
      version: "1.0.0",
    },
    {
      capabilities: {},
    }
  );

  try {
    await client.connect(transport);
    console.log("Connected to Gateway!");

    // List Tools
    const tools = await client.listTools();
    console.log(`Found ${tools.tools.length} tools.`);
    const createObjTool = tools.tools.find(t => t.name === "unity_create_object");
    if (createObjTool) {
        console.log(" - Verified: unity_create_object exists");
    } else {
        console.error(" - Failed: unity_create_object missing");
    }

    // Call Tool: Create a Test Cube
    console.log("Calling unity_create_primitive...");
    const result = await client.callTool({
      name: "unity_create_primitive",
      arguments: {
        type: "Cube",
        name: "GatewayTestCube"
      },
    });

    console.log("Result:", JSON.stringify(result, null, 2));

  } catch (error) {
    console.error("Test Failed:", error);
  } finally {
    await client.close();
  }
}

main();
