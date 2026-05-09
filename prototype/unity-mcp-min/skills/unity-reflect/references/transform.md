# Transform — single-tool patterns

## Idea

One tool, many calls: set **which component** (`UnityEngine.Transform`) and **which member** (`position`, `localPosition`, `eulerAngles`, `localScale`, …) plus `args`.

## Example — world position

```json
{
  "mode": "set",
  "targetType": "UnityEngine.Transform",
  "member": "position",
  "args": [{ "x": 0, "y": 2, "z": -1 }],
  "objectSelector": { "gameObjectName": "DemoCube" },
  "dryRun": true
}
```

## Example — rotation without a “rotation tool”

Same tool; change `member` to `eulerAngles` or `localEulerAngles` and pass a `Vector3`-shaped object.

## Selector

- Prefer `gameObjectName` for demos; use `instanceId` when stable ids are known.
