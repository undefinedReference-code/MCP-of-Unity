# Scene / create objects — no objectSelector

## Principle

When no existing GameObject is targeted, use **`invoke` on a static method** that returns the new object.  
`objectSelector` is **omitted** (static methods do not need an instance).

## Example — primitive cube

`GameObject.CreatePrimitive(PrimitiveType type)`:

```json
{
  "mode": "invoke",
  "targetType": "UnityEngine.GameObject",
  "member": "CreatePrimitive",
  "args": ["Cube"],
  "dryRun": false
}
```

Allowed enum names match Unity `PrimitiveType`, e.g. `Sphere`, `Capsule`, `Cylinder`, `Plane`, `Quad`.

## After creation

Use the JSON **`result`** from the tool response (`instanceId`, `name`) for follow-up `set`/`invoke` on `Transform` or `AddComponent`.

## Empty GameObject (no primitive mesh)

`new GameObject()` is not exposed as a static method on `GameObject`. Extending the bridge for parameterless constructors would be a separate feature; for MVP use primitives or add a tiny helper type in your project if needed.
