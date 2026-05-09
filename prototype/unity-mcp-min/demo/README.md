# Demo Quick Run

## What this validates

- **Anti-pattern**: separate MCP tools for “set position”, “set rotation”, “create cube”, …
- **Target pattern**: one `unity_reflect_call` with `targetType` + `member` + `args`:
  - `UnityEngine.Transform` + `position` / `eulerAngles` / …
  - `UnityEngine.GameObject` + `CreatePrimitive` + `[ "Cube" ]` (**no** `objectSelector`)
  - `UnityEngine.GameObject` + `AddComponent` + `[ "Full.Type.Name" ]`

## Preconditions

- Unity：已安装 **`com.mcpofunity.reflect-bridge`**，菜单 **Window → MCP Reflect Bridge** → **Start server**。
- Python：`UNITY_BRIDGE_ENDPOINT` 与桥接端口一致（默认 `http://127.0.0.1:7890`）。
- Transform / 挂脚本演示：场景中有 `DemoCube`；项目中已有示例 Behaviour 类型（脚本演示）。

## MCP server

```bash
cd prototype/unity-mcp-min/Server
pip install -r requirements.txt
python main.py
```

## Scripts

- `scripts/transform-demo.md`
- `scripts/scripting-demo.md`
- `scripts/scene-create-demo.md`
