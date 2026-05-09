---
name: unity-reflect-minimal
description: Use minimal Unity MCP tools to operate Animator and ParticleSystem safely through reflection templates.
---

# Unity Reflect Minimal Skill

## Purpose

Operate Unity Editor with a tiny tool surface:

- `unity_schema_hint`
- `unity_reflect_call`
- `unity_validate_result`

This skill prioritizes reproducibility and safety over broad free-form invocation.

## Progressive Disclosure

### L1 Metadata (always-on intent routing)

Use this skill when the user asks to:

- adjust animation speed/parameters/state transitions
- tune particle emission/lifetime/playback
- execute Unity changes while keeping tools minimal

### L2 Workflow (default execution policy)

For each user task:

1. **Classify domain** (`animation` or `particles`).
2. **Call `unity_schema_hint`** with user intent.
3. **Select highest score template**, then set `dryRun=true`.
4. **Call `unity_reflect_call`** (dry-run) to validate signature and selector.
5. If dry-run is valid, run same request with `dryRun=false`.
6. **Call `unity_validate_result`** to decide accept/retry.
7. If failed, retry once by:
   - changing selector (gameObjectName/instanceId), or
   - choosing next hint template.

### L3 References (on-demand)

- `references/animation.md`
- `references/particles.md`

Use references when:

- dry-run fails on overload or argument type
- nested module APIs are needed
- objectSelector requires correction

## Safety Rules

- Never bypass denylist members.
- Prefer instance selection via stable object name or explicit instanceId.
- Mutating calls must pass dry-run first unless the user explicitly says to skip.
- When reflection returns ambiguity, ask for a fully qualified type.

## Completion Criteria

The task is complete only when:

- tool response shows `ok=true`
- expected value/state has been changed
- validation verdict is `accept`

