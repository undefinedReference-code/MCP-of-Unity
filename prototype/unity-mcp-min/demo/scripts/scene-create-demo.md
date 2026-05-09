# Scene create demo（无 objectSelector）

## Goal

Create a primitive cube in the scene **without** naming an existing GameObject.

## 1) Hint

```json
{
  "tool": "unity_schema_hint",
  "arguments": {
    "domain": "scene",
    "intent": "create a new cube primitive in the scene"
  }
}
```

## 2) Dry-run CreatePrimitive

Note: **omit** `objectSelector`.

```json
{
  "tool": "unity_reflect_call",
  "arguments": {
    "mode": "invoke",
    "targetType": "UnityEngine.GameObject",
    "member": "CreatePrimitive",
    "args": ["Cube"],
    "dryRun": true
  }
}
```

## 3) Execute

Same payload with `"dryRun": false`.

## 4) Follow-up

Read `data.result` from the Unity JSON (normalized `name`, `instanceId`). Use `instanceId` in later calls if you target that object by id.
