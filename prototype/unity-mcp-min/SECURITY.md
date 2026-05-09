# Security Boundary

## Threat Model (Prototype)

反射调用能力默认是高风险能力。本原型采用最小安全边界：

- namespace allowlist（仅 `UnityEngine`、`UnityEditor`）
- member denylist（如删除、退出、销毁等高风险成员）
- mutating 调用默认先 `dryRun=true` 验签

## Safety Rules

- 禁止直接扩展到任意系统 API。
- 新增允许类型前必须评估副作用。
- 所有错误需返回结构化信息，避免模型盲重试。

## Known Gaps

- 目前未实现用户确认弹窗链路。
- 未实现细粒度 RBAC 权限。
- 未实现完整审计日志与回放。

## Reporting

请在 issue 中标注 `security` 标签，并提供：

- 触发步骤
- 输入参数
- 预期与实际行为

