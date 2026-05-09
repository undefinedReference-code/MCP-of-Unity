# Unity MCP 路线验证报告（网页证据版）

## 目标

验证你的核心设想是否成立：

1. 现有方案是否主要是“按功能拆很多工具”
2. “少量通用反射工具 + Skill 渐进披露”是否有意义

## 网页证据（抽样）

- 大量功能工具路线（工具数量高）  
  - [AnkleBreaker-Studio/unity-mcp-server](https://github.com/AnkleBreaker-Studio/unity-mcp-server)
- 两层工具/代理工具以缓解工具列表膨胀  
  - [two-tier tool system 提交](https://github.com/AnkleBreaker-Studio/unity-mcp-server/commit/1117dc9eb349586a96de5d014d7e3249e79742b1)
- 反射与执行代码能力已出现（不是空白）  
  - [CoplayDev/unity-mcp](https://github.com/CoplayDev/unity-mcp)  
  - [execute_code 提交](https://github.com/CoplayDev/unity-mcp/commit/97a61efdec7dc9b4c9af8c8f5cf68657b18d4fd2)
- Skill 与渐进披露模式公开可见  
  - [Unity MCP skill 示例](https://github.com/CoplayDev/unity-mcp/blob/v9.6.6/unity-mcp-skill/SKILL.md)  
  - [Skills guidance](https://aka.ms/skills/guidance)
- Unity 官方对反射开销风险说明  
  - [Unity Manual: Avoid C# reflection overhead](https://docs.unity3d.com/Manual/performance-gc-avoid-reflection.html)

## 结论

### 1) 你的观察是正确的（大方向）

- 主流 Unity MCP 仍然以“功能工具集合”作为主体，覆盖创建对象、组件、场景等常见操作。
- 当想覆盖更细粒度引擎能力时，工具数量容易增长很快。

### 2) 你的方案不是“从零没人做”，但有明显差异化空间

- 反射/代码执行作为能力原子已经有人做过。
- 但“**极少工具优先** + **Skill 分阶段参数引导** + **可复现安全边界**”这条产品化路径仍有空间。

### 3) 这个想法有意义

- **扩展性**：减少新能力必须新增工具的压力。
- **迁移性**：小而稳定的 tool schema 更利于跨模型迁移。
- **协作性**：社区可主要贡献“参数模板+Skill 参考”，而非持续增加工具数量。

## 风险与约束

- **安全风险**：反射面太宽可能触发危险 API。
- **可控性风险**：参数自由度高导致不可复现。
- **性能风险**：频繁反射扫描可能带来开销。

## 原型防线（已纳入实现）

- allowlist/denylist
- `dryRun=true` 先签名匹配后执行
- 结构化错误（可重试）
- animation/particles 两域模板化提示

