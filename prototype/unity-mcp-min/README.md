# Unity MCP Min (Reflection + Skill)

一个最小可演示的 Unity MCP 原型，目标是：

- 用极少工具（<=3）覆盖更多 Unity 控制能力
- 用反射参数替代大规模功能工具枚举
- 用 Skill 的渐进披露降低模型填参错误

## Tool Surface

- `unity_reflect_call`：通用反射调用（`get/set/invoke` + `dryRun`）
- `unity_schema_hint`：按领域给参数模板（`transform` / `scripting`）
- `unity_validate_result`：轻量验收（accept/retry）

## Project Layout

- `Packages/com.mcpofunity.reflect-bridge/`：**开箱即用 Unity 包**（`ReflectCallTool` + HTTP 桥接 + **Window → MCP Reflect Bridge** 配置窗口）
- `Server/main.py`：Python MCP server
- `Server/knowledge/*.json`：模板知识库
- `skills/unity-reflect/SKILL.md`：渐进披露工作流
- `demo/`：可复现演示脚本

## Quick Start

1. 在 Unity 工程的 `Packages/manifest.json` 中用 `file:` 依赖引用  
   `prototype/unity-mcp-min/Packages/com.mcpofunity.reflect-bridge`（详见包内 `Documentation~/INSTALL.zh.md`）。
2. 打开 Unity：**Window → MCP Reflect Bridge**，设置端口并 **Start server**（可与 Python 默认 `7890` 一致）。
3. 启动 Python MCP server：

```bash
cd prototype/unity-mcp-min/Server
pip install -r requirements.txt
python main.py
```

4. 按 `demo/README.md` 执行 **Transform** 与 **挂载脚本** Demo。

## Why Not 200+ Tools

- 工具数量越多，注册与路由成本越高。
- 长尾功能会推动工具不断膨胀。
- 少量稳定工具 + 模板/Skill 更利于迭代和协作。

## Current Scope

- 仅 Editor 场景
- MVP 验证：**Transform**、**静态创建物体（CreatePrimitive）**、**挂载已有脚本（AddComponent）**
- 原型性质，优先验证路径而非全覆盖

