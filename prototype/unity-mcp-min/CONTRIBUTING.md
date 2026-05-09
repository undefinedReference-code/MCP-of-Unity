# Contributing

欢迎贡献以下方向：

- 新领域模板（如 UI、Physics）
- Skill 参考文档增强
- 反射参数转换与错误恢复策略
- 安全策略完善（allowlist/denylist/RBAC）

## Contribution Flow

1. Fork / 新分支。
2. 提交最小、单一目的改动。
3. 为新增模板提供一个可复现示例。
4. 提交 PR，说明：
   - 解决的问题
   - 影响的 tool/skill
   - 如何验证成功

## Template Contribution Rule

- 每个新模板必须包含：
  - `title`
  - `when`
  - `keywords`
  - `requestTemplate`
- `requestTemplate` 必须可 dry-run。

## Coding Notes

- Python 侧保持简单、可读、可调试。
- Unity 侧 C# 改动优先放在 Editor 目录。

