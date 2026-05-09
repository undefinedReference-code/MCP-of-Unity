# Animation Reflection Reference

## Typical Targets

- `UnityEngine.Animator.speed` (set)
- `UnityEngine.Animator.SetFloat(string, float)` (invoke)
- `UnityEngine.Animator.SetBool(string, bool)` (invoke)

## Recommended Call Pattern

1. `unity_schema_hint(domain="animation", intent="...")`
2. Copy top `requestTemplate`, change object selector and values.
3. Set `dryRun=true`, call `unity_reflect_call`.
4. If resolved, set `dryRun=false`, call again.
5. Validate with `unity_validate_result`.

## Common Failures

- **No object found**: `gameObjectName` not present in scene.
- **No overload matched**: argument count or types mismatch.
- **Type ambiguity**: use fully-qualified type name.

## Example

```json
{
  "mode": "invoke",
  "targetType": "UnityEngine.Animator",
  "member": "SetFloat",
  "args": ["Speed", 1.2],
  "objectSelector": { "gameObjectName": "Player" },
  "dryRun": true
}
```

