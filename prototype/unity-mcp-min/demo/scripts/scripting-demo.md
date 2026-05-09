# Script attach demo

## Goal

Attach existing behaviour `YourNamespace.DemoBehaviour` to `DemoCube` via **`GameObject.AddComponent`** through reflection.

Replace `YourNamespace.DemoBehaviour` with your actual class full name.

## 1) Hint

```json
{
  "tool": "unity_schema_hint",
  "arguments": {
    "domain": "scripting",
    "intent": "attach DemoBehaviour script component to DemoCube"
  }
}
```

## 2) Dry-run AddComponent

```json
{
  "tool": "unity_reflect_call",
  "arguments": {
    "mode": "invoke",
    "targetType": "UnityEngine.GameObject",
    "member": "AddComponent",
    "args": ["YourNamespace.DemoBehaviour"],
    "objectSelector": { "gameObjectName": "DemoCube" },
    "dryRun": true
  }
}
```

## 3) Execute

Same with `"dryRun": false`.

## 4) Validate

Inspect Unity: **DemoCube** should list the component.

```json
{
  "tool": "unity_validate_result",
  "arguments": {
    "task": "Add DemoBehaviour to DemoCube",
    "result": { "ok": true, "message": "Invoke succeeded." }
  }
}
```

## Pass criteria

- Component appears on GameObject; Console shows no reflection errors.
