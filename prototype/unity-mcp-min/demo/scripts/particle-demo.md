# Particle Demo Script

## User Goal

Increase particle emission and adjust particle lifetime.

## Steps

1. Ask for schema hint:

```json
{
  "tool": "unity_schema_hint",
  "arguments": {
    "domain": "particles",
    "intent": "increase emission rate and make particles live longer"
  }
}
```

2. Dry-run emission change:

```json
{
  "tool": "unity_reflect_call",
  "arguments": {
    "mode": "set",
    "targetType": "UnityEngine.ParticleSystem+EmissionModule",
    "member": "rateOverTimeMultiplier",
    "args": [35.0],
    "objectSelector": { "gameObjectName": "VFX_Emitter" },
    "dryRun": true
  }
}
```

3. Execute emission change (`dryRun=false`), then lifetime:

```json
{
  "tool": "unity_reflect_call",
  "arguments": {
    "mode": "set",
    "targetType": "UnityEngine.ParticleSystem+MainModule",
    "member": "startLifetime",
    "args": [2.5],
    "objectSelector": { "gameObjectName": "VFX_Emitter" },
    "dryRun": false
  }
}
```

4. Validate:

```json
{
  "tool": "unity_validate_result",
  "arguments": {
    "task": "update particle emission and lifetime",
    "result": { "ok": true, "message": "Set succeeded." }
  }
}
```

## Pass Criteria

- At least one dry-run succeeds.
- Execute succeeds.
- Particle output visibly denser and longer-lived.

