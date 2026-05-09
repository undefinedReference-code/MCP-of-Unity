# Demo Quick Run (Transform + Script attach)

## What this validates

- **Anti-pattern**: separate MCP tools for “set position”, “set rotation”, …
- **Target pattern**: one `unity_reflect_call` with `targetType` + `member` + `args`:
  - `UnityEngine.Transform` + `position` / `eulerAngles` / …
  - `UnityEngine.GameObject` + `AddComponent` + `[ "Full.Type.Name" ]`

## Preconditions

- Unity project with bridge exposing `POST /unity_reflect_call` (wired to `ReflectCallTool.HandleCommand`).
- Scene object named `DemoCube` with `Transform`.
- A demo behaviour class (e.g. `DemoBehaviour`) already exists in the project for attach demo.

## MCP server

```bash
cd prototype/unity-mcp-min/Server
pip install -r requirements.txt
python main.py
```

## Scripts

- `scripts/transform-demo.md`
- `scripts/scripting-demo.md`
