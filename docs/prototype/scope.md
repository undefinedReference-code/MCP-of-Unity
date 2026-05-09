# Unity MCP Reflection Prototype Scope

## Problem Statement

Current Unity MCP implementations often expose many task-specific tools. This is powerful, but it increases:

- tool registration payload size
- model routing complexity
- maintenance overhead when Unity APIs evolve

This prototype validates a different approach: keep MCP tools minimal and drive most Unity operations through reflection-based parameters plus Skill guidance.

## In Scope

- Editor-only workflows (no runtime build pipeline support)
- Two MVP verticals:
  - **Transform** (`UnityEngine.Transform` properties)
  - **Script attach** (`GameObject.AddComponent(Type)`, optional `GetComponent(Type)` guard)
- Core MCP tools:
  - `unity_reflect_call`
  - `unity_schema_hint`
  - `unity_validate_result` (optional helper, included in prototype)
- Skill-based progressive disclosure with 3 levels (L1/L2/L3)
- Safety controls:
  - namespace allowlist
  - member denylist
  - dry-run signature validation

## Out of Scope

- Full parity with large feature-style Unity MCP servers
- Terrain/UI/NavMesh/etc. coverage in this phase
- Runtime AOT platform support
- Autonomous destructive editor operations
- Production-grade auth/multi-tenant architecture

## Success Metrics

- Exposed tools <= 3
- Two end-to-end demo tasks pass:
  - change **Transform** position (same tool as rotation: different `member`)
  - **attach** an existing behaviour via **AddComponent**
- From fresh setup to first successful tool call <= 30 minutes
- Structured, retry-friendly errors on invalid parameters
- Safety policy docs complete and understandable

## Non-Goals

- Replacing all specialized tools immediately
- Eliminating all reflection risks
- Achieving perfect one-shot model success on every prompt

