# Transform demo script

## Goal

Move `DemoCube` world position to `(0, 2, 0)` using **only** the reflection tool.

## 1) Hint

```json
{
  "tool": "unity_schema_hint",
  "arguments": {
    "domain": "transform",
    "intent": "move DemoCube up on Y axis world position"
  }
}
```

## 2) Dry-run set `Transform.position`

```json
{
  "tool": "unity_reflect_call",
  "arguments": {
    "mode": "set",
    "targetType": "UnityEngine.Transform",
    "member": "position",
    "args": [{ "x": 0, "y": 2, "z": 0 }],
    "objectSelector": { "gameObjectName": "DemoCube" },
    "dryRun": true
  }
}
```

## 3) Execute

Same payload with `"dryRun": false`.

## 4) Validate

```json
{
  "tool": "unity_validate_result",
  "arguments": {
    "task": "set DemoCube position",
    "result": { "ok": true, "message": "Set succeeded." }
  }
}
```

## Pass criteria

- Inspector **Transform** shows position `(0, 2, 0)` (or equivalent world placement).
