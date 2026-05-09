---
name: unity-reflect-minimal
description: Minimal Unity MCP — one reflection tool for Transform, scene creation (static invoke), script attach + Skill guidance.
---

# Unity Reflect Minimal Skill

## Purpose

Use **one execution surface** (`unity_reflect_call`): same JSON shape for:

- **Transform** (`Transform.position`, etc.)
- **New objects** (static `GameObject.CreatePrimitive` — no `objectSelector`)
- **Attach behaviours** (`GameObject.AddComponent(Type)`)

Avoid separate tools like “set position” vs “set rotation” vs “create cube”.

Supporting tools:

- `unity_schema_hint` — templates for `transform`, `scripting`, `scene`
- `unity_validate_result` — accept / retry

## Progressive Disclosure

### L1 — When to use

Use when the user wants to:

- change **Transform** properties
- **create** primitives (or other static factory flows)
- **attach** an existing Component type to a GameObject

### L2 — Fixed workflow

1. Classify domain: `transform`, `scripting`, or `scene`.
2. `unity_schema_hint(domain=..., intent="...")`.
3. Take top template → set `dryRun=true` → `unity_reflect_call`.
4. On success, same payload with `dryRun=false`.
5. `unity_validate_result`.

One retry: fix names/types, or pick next template. For **scene** creates, follow up using `instanceId` from the **invoke result** if the next step targets that object.

### L3 — Deep references

- `references/transform.md`
- `references/scripting.md`
- `references/scene.md`

## Completion

Done when `unity_reflect_call` returns `ok=true`, Unity reflects the change, and validation `accepted=true`.

## Notes

- **Script authoring**: attaching **existing** types only; generating `.cs` files is out of scope here.
- **Unity-side execution**: generic reflection binding — no per-operation `switch("setPosition")` in tool code.
