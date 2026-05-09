# Particles Reflection Reference

## Typical Targets

- `UnityEngine.ParticleSystem.Play()` (invoke)
- `UnityEngine.ParticleSystem+EmissionModule.rateOverTimeMultiplier` (set)
- `UnityEngine.ParticleSystem+MainModule.startLifetime` (set)

## Recommended Call Pattern

1. `unity_schema_hint(domain="particles", intent="...")`
2. Pick a template and adapt selector/value.
3. Dry-run with `unity_reflect_call`.
4. Execute with `dryRun=false`.
5. Verify with `unity_validate_result`.

## Notes About Nested Module Types

`ParticleSystem.MainModule` and `ParticleSystem.EmissionModule` are nested types.
Use `+` in reflection name:

- `UnityEngine.ParticleSystem+MainModule`
- `UnityEngine.ParticleSystem+EmissionModule`

## Example

```json
{
  "mode": "set",
  "targetType": "UnityEngine.ParticleSystem+EmissionModule",
  "member": "rateOverTimeMultiplier",
  "args": [40.0],
  "objectSelector": { "gameObjectName": "VFX_Emitter" },
  "dryRun": true
}
```

