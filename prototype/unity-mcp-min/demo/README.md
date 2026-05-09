# Demo Quick Run

## Demo Scope

- Animation flow: change animator speed/parameter.
- Particle flow: change emission rate/lifetime.

## Preconditions

- Unity project opened with the prototype plugin containing `ReflectCallTool.cs`.
- Python 3.11+ installed.
- Unity bridge endpoint reachable at `http://127.0.0.1:7890`.

## Start MCP Server

```bash
cd prototype/unity-mcp-min/Server
pip install -r requirements.txt
python main.py
```

## Run Demo Flows

- Follow `scripts/animation-demo.md`.
- Follow `scripts/particle-demo.md`.

## Expected Result

- `unity_reflect_call` returns `ok=true` on execution call.
- `unity_validate_result` returns `accepted=true`.
- Scene behavior changes are visible in Unity.

