namespace UnityMcp.Tests.Editor.Shared
{
  /// <summary>
  /// RPC method names corresponding to gateway MCP tools (contract parity subset).
  /// Expand in Phase 2+; generated list lives in gateway/tests/fixtures/gateway-tools.json.
  /// </summary>
  public static class GatewayToolRpcMap
  {
    public static readonly string[] CoreToolRpcMethods =
    {
      "unity.list_objects",
      "unity.create_object",
      "unity.create_primitive",
      "unity.set_transform",
      "unity.delete_object",
      "unity.set_material",
      "unity.create_material",
      "unity.set_material_property",
      "unity.get_object_details",
      "unity.get_project_info",
      "unity.get_physics_settings",
      "unity.list_tags",
      "unity.list_layers",
      "unity.play",
      "unity.stop",
      "unity.undo",
      "unity.begin_undo_group",
      "unity.end_undo_group",
    };
  }
}
