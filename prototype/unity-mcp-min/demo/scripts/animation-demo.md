# Animation Demo Script

## User Goal

Increase player animation speed to 1.5 and set movement parameter.

## Steps

1. Ask for schema hint:

```json
{
  "tool": "unity_schema_hint",
  "arguments": {
    "domain": "animation",
    "intent": "increase player animation speed and movement blend"
  }
}
```

2. Take top template and set `dryRun=true`:

```json
{
  "tool": "unity_reflect_call",
  "arguments": {
    "mode": "set",
    "targetType": "UnityEngine.Animator",
    "member": "speed",
    "args": [1.5],
    "objectSelector": { "gameObjectName": "Player" },
    "dryRun": true
  }
}
```

3. Execute with `dryRun=false`:

```json
{
  "tool": "unity_reflect_call",
  "arguments": {
    "mode": "set",
    "targetType": "UnityEngine.Animator",
    "member": "speed",
    "args": [1.5],
    "objectSelector": { "gameObjectName": "Player" },
    "dryRun": false
  }
}
```

4. Validate:

```json
{
  "tool": "unity_validate_result",
  "arguments": {
    "task": "set animator speed to 1.5",
    "result": { "ok": true, "message": "Set succeeded." }
  }
}
```

## Pass Criteria

- Dry-run succeeds.
- Execute succeeds.
- Animator speed visibly changed.

