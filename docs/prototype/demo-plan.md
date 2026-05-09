# Unity MCP 反射最小工具 Demo 计划书（可执行版）

## 总目标

在 3 个 MCP 工具以内，完成两个可复现演示：

1. **Transform**：用同一反射入口设置 `position`（rotation 仅换 `member`，不设单独工具）
2. **挂载脚本**：`GameObject.AddComponent`，参数为脚本类型全名

并用 Skill 渐进披露指导模型稳定填参。

## 阶段拆分（每阶段可见验收）

### Stage 0：边界冻结（0.5 天）

工作：

- 固定范围：`transform` + `scripting`。
- 固定工具：`unity_reflect_call`、`unity_schema_hint`、`unity_validate_result`。
- 固定指标：首次跑通 <=30 分钟，演示任务各成功 >=2 次。

可见产物：

- `docs/prototype/scope.md`
- `docs/prototype/benchmark.md`

验收标准：

- 有明确“不做什么”清单。
- 指标可测量。

### Stage 1：核心反射调用（1 天）

工作：

- Unity C# 端实现 `ReflectCallTool`。
- 支持 `get/set/invoke`（由 **字典分发** 到三个通用处理器，不按业务操作符分支）。
- 支持 `objectSelector`（`gameObjectName` / `instanceId`）。
- 支持 `dryRun`。
- `invoke` 支持 `System.Type` 参数（字符串解析为具体 **Component** 类型，供 `AddComponent`/`GetComponent`）。
- allowlist + denylist + 参数类型转换。

可见产物：

- `prototype/unity-mcp-min/Unity/Editor/Tools/ReflectCallTool.cs`

验收标准：

- 错误参数返回结构化错误。
- `Transform.position` **set** 成功。
- `GameObject.AddComponent` **invoke** 成功。

### Stage 2：参数提示层（0.5 天）

工作：

- Python MCP 端 `unity_schema_hint`。
- 构建 `transform.json`、`scripting.json`。
- 意图匹配返回前三模板。

可见产物：

- `prototype/unity-mcp-min/Server/main.py`
- `prototype/unity-mcp-min/Server/knowledge/transform.json`
- `prototype/unity-mcp-min/Server/knowledge/scripting.json`

验收标准：

- 输入自然语言意图，能返回可执行模板。

### Stage 3：Skill 渐进披露（0.5 天）

工作：

- L1：触发条件（Transform / 挂脚本）。
- L2：hint → dryRun → execute → validate。
- L3：`references/transform.md`、`references/scripting.md`。

可见产物：

- `prototype/unity-mcp-min/skills/unity-reflect/SKILL.md`
- `prototype/unity-mcp-min/skills/unity-reflect/references/transform.md`
- `prototype/unity-mcp-min/skills/unity-reflect/references/scripting.md`

验收标准：

- 模型按流程调用，默认不跳过 dry-run。

### Stage 4：Demo 脚本（0.5 天）

可见产物：

- `prototype/unity-mcp-min/demo/README.md`
- `prototype/unity-mcp-min/demo/scripts/transform-demo.md`
- `prototype/unity-mcp-min/demo/scripts/scripting-demo.md`

验收标准：

- 新用户按文档可在 Unity 内看到位置变化与组件挂载。

### Stage 5：发布包（0.5 天）

可见产物：

- `prototype/unity-mcp-min/README.md`
- `prototype/unity-mcp-min/SECURITY.md`
- `prototype/unity-mcp-min/CONTRIBUTING.md`

## Go / No-Go

- MCP 工具数 <= 3
- Transform + AddComponent 两条链路可复现
- README + SECURITY 完整
