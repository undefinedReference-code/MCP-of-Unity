from __future__ import annotations

import json
import os
from pathlib import Path
from typing import Any, Literal

import httpx
from mcp.server.fastmcp import FastMCP
from pydantic import BaseModel, Field, ValidationError


class ObjectSelector(BaseModel):
    instanceId: int | None = None
    gameObjectName: str | None = None


class ReflectCallRequest(BaseModel):
    mode: Literal["get", "set", "invoke"]
    targetType: str = Field(min_length=1)
    member: str = Field(min_length=1)
    args: list[Any] = Field(default_factory=list)
    objectSelector: ObjectSelector | None = None
    dryRun: bool = False


class SchemaHintRequest(BaseModel):
    domain: Literal["transform", "scripting"]
    intent: str = Field(min_length=3)


BASE_DIR = Path(__file__).resolve().parent
KNOWLEDGE_DIR = BASE_DIR / "knowledge"
UNITY_BRIDGE_ENDPOINT = os.getenv("UNITY_BRIDGE_ENDPOINT", "http://127.0.0.1:7890")

mcp = FastMCP("unity-mcp-min-python")


def _load_knowledge(domain: str) -> dict[str, Any]:
    path = KNOWLEDGE_DIR / f"{domain}.json"
    return json.loads(path.read_text(encoding="utf-8"))


def _score_template(intent: str, keywords: list[str]) -> int:
    text = intent.lower()
    return sum(1 for item in keywords if item.lower() in text)


@mcp.tool()
async def unity_reflect_call(
    mode: str,
    targetType: str,
    member: str,
    args: list[Any] | None = None,
    objectSelector: dict[str, Any] | None = None,
    dryRun: bool = False,
) -> dict[str, Any]:
    """
    Generic Unity reflection call.
    Use dryRun=true first for mutating operations.
    """
    try:
        request = ReflectCallRequest(
            mode=mode,
            targetType=targetType,
            member=member,
            args=args or [],
            objectSelector=ObjectSelector(**objectSelector) if objectSelector else None,
            dryRun=dryRun,
        )
    except ValidationError as exc:
        return {
            "ok": False,
            "message": "Invalid arguments for unity_reflect_call.",
            "details": json.loads(exc.json()),
        }

    payload = request.model_dump(exclude_none=True)

    try:
        async with httpx.AsyncClient(timeout=20.0) as client:
            response = await client.post(f"{UNITY_BRIDGE_ENDPOINT}/unity_reflect_call", json=payload)
            response.raise_for_status()
            return response.json()
    except Exception as exc:  # prototype-level fallback
        return {
            "ok": False,
            "message": "Unity bridge request failed.",
            "details": str(exc),
            "bridgeEndpoint": UNITY_BRIDGE_ENDPOINT,
            "request": payload,
        }


@mcp.tool()
def unity_schema_hint(domain: str, intent: str) -> dict[str, Any]:
    """
    Return intent-based reflection templates (transform / scripting).
    """
    try:
        request = SchemaHintRequest(domain=domain, intent=intent)
    except ValidationError as exc:
        return {
            "ok": False,
            "message": "Invalid arguments for unity_schema_hint.",
            "details": json.loads(exc.json()),
        }

    knowledge = _load_knowledge(request.domain)
    templates = knowledge.get("templates", [])
    ranked = sorted(
        templates,
        key=lambda item: _score_template(request.intent, item.get("keywords", [])),
        reverse=True,
    )[:3]

    suggestions = []
    for item in ranked:
        suggestions.append(
            {
                "title": item.get("title"),
                "when": item.get("when"),
                "requestTemplate": item.get("requestTemplate"),
                "score": _score_template(request.intent, item.get("keywords", [])),
            }
        )

    return {
        "ok": True,
        "domain": request.domain,
        "intent": request.intent,
        "suggestions": suggestions,
        "guidance": "Call unity_reflect_call with dryRun=true first, then execute with dryRun=false after validation.",
    }


@mcp.tool()
def unity_validate_result(task: str, result: dict[str, Any]) -> dict[str, Any]:
    """
    Minimal verifier to guide retry/accept decisions.
    """
    accepted = bool(result.get("ok", False))
    return {
        "ok": True,
        "task": task,
        "accepted": accepted,
        "verdict": "accept" if accepted else "retry_or_fail",
        "reason": result.get("message", "No message provided."),
    }


if __name__ == "__main__":
    mcp.run()

