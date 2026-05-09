# Unity MCP 反射最小工具 Demo 计划书（可执行版）

## 总目标

在 3 个 MCP 工具以内，完成两个可复现演示：

1. 动画参数/速度控制
2. 粒子发射率/生命周期控制

并用 Skill 渐进披露指导模型稳定填参。

## 阶段拆分（每阶段可见验收）

### Stage 0：边界冻结（0.5 天）

工作：

- 固定范围：仅 Animation + ParticleSystem。
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
- 支持 `get/set/invoke`。
- 支持 `objectSelector`（`gameObjectName` / `instanceId`）。
- 支持 `dryRun`。
- 加入 allowlist + denylist + 参数类型转换。

可见产物：

- `prototype/unity-mcp-min/Unity/Editor/Tools/ReflectCallTool.cs`

验收标准：

- 错误参数返回结构化错误。
- 对 `Animator` 与 `ParticleSystem` 至少各一次有效调用。

### Stage 2：参数提示层（0.5 天）

工作：

- Python MCP 端实现 `unity_schema_hint`。
- 构建 animation/particles 模板知识库。
- 意图匹配后返回前三模板。

可见产物：

- `prototype/unity-mcp-min/Server/main.py`
- `prototype/unity-mcp-min/Server/knowledge/animation.json`
- `prototype/unity-mcp-min/Server/knowledge/particles.json`

验收标准：

- 输入自然语言意图，能返回可执行模板。

### Stage 3：Skill 渐进披露（0.5 天）

工作：

- L1：触发条件与范围说明。
- L2：固定流程（hint -> dryRun -> execute -> validate）。
- L3：故障排查与参数参考。

可见产物：

- `prototype/unity-mcp-min/skills/unity-reflect/SKILL.md`
- `prototype/unity-mcp-min/skills/unity-reflect/references/animation.md`
- `prototype/unity-mcp-min/skills/unity-reflect/references/particles.md`

验收标准：

- 模型按流程调用，不跳过 dry-run（默认场景）。

### Stage 4：Demo 脚本与复现文档（0.5 天）

工作：

- 编写动画演示脚本。
- 编写粒子演示脚本。
- 补最小复现步骤。

可见产物：

- `prototype/unity-mcp-min/demo/README.md`
- `prototype/unity-mcp-min/demo/scripts/animation-demo.md`
- `prototype/unity-mcp-min/demo/scripts/particle-demo.md`

验收标准：

- 新用户按文档 30 分钟内跑通。

### Stage 5：发布包（0.5 天）

工作：

- 写项目 README（价值主张 + 快速开始）。
- 写 SECURITY（安全边界）。
- 写 CONTRIBUTING（贡献模板）。

可见产物：

- `prototype/unity-mcp-min/README.md`
- `prototype/unity-mcp-min/SECURITY.md`
- `prototype/unity-mcp-min/CONTRIBUTING.md`

验收标准：

- 可直接上传仓库并邀请协作者。

## Go / No-Go

进入公开招募前必须满足：

- MCP 工具数 <= 3
- 两条演示链路均可复现
- 文档齐全（README + SECURITY + CONTRIBUTING）
- 至少一次从零环境到首个成功调用 <=30 分钟

