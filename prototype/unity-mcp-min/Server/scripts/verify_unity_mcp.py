#!/usr/bin/env python3
"""
Automated smoke test for Unity MCP prototype.

Checks:
  1) GET {endpoint}/health
  2) POST {endpoint}/unity_reflect_call (CreatePrimitive dry-run, no objectSelector)
  3) Local knowledge JSON files exist and parse

Requires: Unity Editor running with Reflect Bridge started on the same port.

Usage:
  python scripts/verify_unity_mcp.py
  python scripts/verify_unity_mcp.py --endpoint http://127.0.0.1:7890
  python scripts/verify_unity_mcp.py --endpoint http://127.0.0.1:7890 --live-create

Exit codes: 0 = all passed, 1 = failure
"""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

import httpx

# Server directory (parent of scripts/)
SERVER_DIR = Path(__file__).resolve().parent.parent
KNOWLEDGE_DIR = SERVER_DIR / "knowledge"


def ok_response(body: dict) -> bool:
    return bool(body.get("ok") is True)


def step_health(client: httpx.Client, base: str) -> bool:
    url = f"{base.rstrip('/')}/health"
    print(f"[1/3] GET {url}")
    try:
        r = client.get(url, timeout=10.0)
    except httpx.ConnectError as e:
        print(f"  FAIL: cannot connect — is Unity bridge running? ({e})")
        return False
    if r.status_code != 200:
        print(f"  FAIL: HTTP {r.status_code} {r.text[:200]}")
        return False
    data = r.json()
    if not data.get("ok"):
        print(f"  FAIL: body ok!=true: {data}")
        return False
    print(f"  PASS: {data}")
    return True


def step_reflect_dry_run(client: httpx.Client, base: str) -> bool:
    url = f"{base.rstrip('/')}/unity_reflect_call"
    payload = {
        "mode": "invoke",
        "targetType": "UnityEngine.GameObject",
        "member": "CreatePrimitive",
        "args": ["Cube"],
        "dryRun": True,
    }
    print(f"[2/3] POST {url} (dry-run CreatePrimitive)")
    try:
        r = client.post(url, json=payload, timeout=30.0)
    except httpx.ConnectError as e:
        print(f"  FAIL: cannot connect ({e})")
        return False
    if r.status_code != 200:
        print(f"  FAIL: HTTP {r.status_code} {r.text[:500]}")
        return False
    try:
        data = r.json()
    except json.JSONDecodeError:
        print(f"  FAIL: not JSON: {r.text[:500]}")
        return False
    # Unity returns either {ok: true, ...} top-level or nested; bridge serializes ReflectCallTool output
    if not ok_response(data):
        print(f"  FAIL: reflect response: {json.dumps(data, ensure_ascii=False)[:800]}")
        return False
    print("  PASS: dry-run invoke resolved")
    return True


def step_reflect_live_create(client: httpx.Client, base: str) -> bool:
    url = f"{base.rstrip('/')}/unity_reflect_call"
    payload = {
        "mode": "invoke",
        "targetType": "UnityEngine.GameObject",
        "member": "CreatePrimitive",
        "args": ["Cube"],
        "dryRun": False,
    }
    print(f"[live] POST {url} (CreatePrimitive — modifies scene)")
    try:
        r = client.post(url, json=payload, timeout=30.0)
    except httpx.ConnectError as e:
        print(f"  FAIL: cannot connect ({e})")
        return False
    if r.status_code != 200:
        print(f"  FAIL: HTTP {r.status_code} {r.text[:500]}")
        return False
    data = r.json()
    if not ok_response(data):
        print(f"  FAIL: {json.dumps(data, ensure_ascii=False)[:800]}")
        return False
    print("  PASS: primitive created (check Hierarchy for new Cube)")
    return True


def step_knowledge_files() -> bool:
    print("[3/3] Knowledge JSON files")
    required = ["transform.json", "scripting.json", "scene.json"]
    for name in required:
        path = KNOWLEDGE_DIR / name
        if not path.is_file():
            print(f"  FAIL: missing {path}")
            return False
        try:
            json.loads(path.read_text(encoding="utf-8"))
        except json.JSONDecodeError as e:
            print(f"  FAIL: invalid JSON {path}: {e}")
            return False
        print(f"  OK: {name}")
    print("  PASS")
    return True


def main() -> int:
    parser = argparse.ArgumentParser(description="Verify Unity Reflect Bridge + local knowledge.")
    parser.add_argument(
        "--endpoint",
        default="http://127.0.0.1:7890",
        help="Unity bridge base URL (same as UNITY_BRIDGE_ENDPOINT)",
    )
    parser.add_argument(
        "--live-create",
        action="store_true",
        help="Also run CreatePrimitive with dryRun=false (creates object in scene)",
    )
    parser.add_argument(
        "--skip-unity",
        action="store_true",
        help="Only verify knowledge files (no HTTP to Unity)",
    )
    args = parser.parse_args()
    base = args.endpoint.rstrip("/")

    all_ok = True
    if not step_knowledge_files():
        all_ok = False
        print("Knowledge check failed; skipping HTTP tests.", file=sys.stderr)
        return 1

    if args.skip_unity:
        print("--skip-unity: skipped HTTP tests")
        return 0

    with httpx.Client() as client:
        if not step_health(client, base):
            all_ok = False
        elif not step_reflect_dry_run(client, base):
            all_ok = False
        elif args.live_create:
            if not step_reflect_live_create(client, base):
                all_ok = False

    if all_ok:
        print("\nAll checks passed.")
        return 0
    print("\nSome checks failed.", file=sys.stderr)
    return 1


if __name__ == "__main__":
    raise SystemExit(main())
