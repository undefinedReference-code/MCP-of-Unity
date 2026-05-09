---
name: unity-reflect-minimal
description: Minimal Unity MCP — one reflection tool for Transform edits and script attachment via parameters + progressive Skill guidance.
---

# Unity Reflect Minimal Skill

## Purpose

Use **one execution surface** (`unity_reflect_call`): same JSON shape for moving objects (`Transform.position`, etc.) and mounting behaviours (`GameObject.AddComponent(Type)`). Avoid separate tools like “set position” vs “set rotation”.

Supporting tools:

- `unity_schema_hint` — pick `transform` or `scripting` templates from intent text
- `unity_validate_result` — accept / retry

## Progressive Disclosure

### L1 — When to use

Use when the user wants to:

- change **Transform** properties (position, rotation, scale, local vs world)
- **attach** an existing MonoBehaviour / Component type to a named GameObject

### L2 — Fixed workflow

1. Classify domain: `transform` or `scripting`.
2. `unity_schema_hint(domain=..., intent="...")`.
3. Take top template → set `dryRun=true` → `unity_reflect_call`.
4. On success, same payload with `dryRun=false`.
5. `unity_validate_result`.

One retry: fix `gameObjectName`, use fully qualified script type, or pick next template.

### L3 — Deep references

- `references/transform.md`
- `references/scripting.md`

## Completion

Done when `unity_reflect_call` returns `ok=true`, Unity scene reflects the change, and validation `accepted=true`.

## Notes

- **Script authoring**: this MVP covers **attaching an existing type** already in the project. Generating new `.cs` files is a separate editor workflow.
- **Unity-side design**: execution is generic reflection (resolve instance → resolve member → bind args → get/set/invoke). No per-operation `switch("setPosition")` in app code.
