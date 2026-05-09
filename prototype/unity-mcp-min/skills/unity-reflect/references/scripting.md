# Script attach — `GameObject.AddComponent(Type)`

## Idea

Target the **GameObject**, invoke **`AddComponent`**, pass the script type as a **string**; the Unity bridge resolves it to `System.Type` and checks it is a concrete `Component` type.

## Example — attach behaviour

Replace `YourNamespace.DemoBehaviour` with your real class (full name if ambiguous).

```json
{
  "mode": "invoke",
  "targetType": "UnityEngine.GameObject",
  "member": "AddComponent",
  "args": ["YourNamespace.DemoBehaviour"],
  "objectSelector": { "gameObjectName": "DemoCube" },
  "dryRun": true
}
```

## Optional — check existing component

```json
{
  "mode": "invoke",
  "targetType": "UnityEngine.GameObject",
  "member": "GetComponent",
  "args": ["YourNamespace.DemoBehaviour"],
  "objectSelector": { "gameObjectName": "DemoCube" },
  "dryRun": false
}
```

If result is null in Unity, safe to `AddComponent`.

## Creating new scripts

Not part of this MVP path: use normal Unity / asset workflows or a dedicated “write script file” tool later.
