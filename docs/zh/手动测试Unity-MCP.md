# 如何手动测试 Unity MCP 是否正常

本文面向本仓库的 **Python MCP Server** + **Unity 包 `com.mcpofunity.reflect-bridge`**（HTTP 桥接）。按顺序做完即可判断链路是否正常。

---

## 一、测试分层

| 层级 | 验证什么 | 是否需要 Unity |
|------|----------|------------------|
| A. Unity HTTP 桥 | `GET /health`、`POST /unity_reflect_call` | **需要** |
| B. Python MCP 工具 | `unity_schema_hint` / `unity_validate_result`（不连 Unity） | 否（若只测这两项） |
| C. 整条链路 | MCP 里调 `unity_reflect_call` → Unity 执行 | **需要** |

建议先做 **A**，再做 **C**；**B** 可用脚本或临时 Python 一行命令验证。

---

## 二、环境准备

1. **Unity 工程**已安装本地包 `com.mcpofunity.reflect-bridge`（`manifest.json` 里 `file:` 指向包目录，见包内 `Documentation~/INSTALL.zh.md`）。
2. 打开 Unity，等待编译完成。
3. 菜单 **Window → MCP Reflect Bridge**：
   - 端口设为 **7890**（或与下述环境变量一致）
   - 点击 **Start server**
   - 窗口状态显示 **Listening**

4. 本机已安装 **Python 3.11+**，并能执行 `pip`。

---

## 三、手动测试 A：Unity HTTP 桥（推荐第一步）

在 PowerShell 或 CMD 中（端口按你的设置改）：

### 1. 健康检查

```powershell
curl.exe -s http://127.0.0.1:7890/health
```

**期望**：返回 JSON，且含 `"ok":true`（以及 `listening`、`port` 等字段）。

若连接被拒绝：Unity 未开、未 Start、或端口不一致。

### 2. 反射 dry-run（不真正创建物体）

```powershell
curl.exe -s -X POST http://127.0.0.1:7890/unity_reflect_call -H "Content-Type: application/json" -d "{\"mode\":\"invoke\",\"targetType\":\"UnityEngine.GameObject\",\"member\":\"CreatePrimitive\",\"args\":[\"Cube\"],\"dryRun\":true}"
```

**期望**：HTTP 200，JSON 里 **`ok` 为 `true`**，且 `message` 类似 “Dry-run invoke resolved”，`data` 里能看到解析后的签名等信息。

若 `ok` 为 `false`：看 `message` / `details`（常见为 JSON 转义错误、Unity 正在编译等）。

### 3.（可选）真实创建 Primitive

仅在确认 dry-run 正常后执行：

```powershell
curl.exe -s -X POST http://127.0.0.1:7890/unity_reflect_call -H "Content-Type: application/json" -d "{\"mode\":\"invoke\",\"targetType\":\"UnityEngine.GameObject\",\"member\":\"CreatePrimitive\",\"args\":[\"Sphere\"],\"dryRun\":false}"
```

**期望**：`ok: true`，Hierarchy 中出现新物体（名称通常为 `Sphere`）。

---

## 四、手动测试 B：Python MCP（不启动 Unity 时）

仅验证 **知识库 + 无 Unity 的工具** 时，可在 `Server` 目录：

```powershell
cd prototype\unity-mcp-min\Server
pip install -r requirements.txt
python -c "import json; from pathlib import Path; p=Path('knowledge'); assert (p/'scene.json').exists(); print('knowledge OK')"
```

说明：`unity_schema_hint` / `unity_validate_result` 在 MCP 进程内运行，**不访问 Unity**；完整联调仍需 **A** 正常。

---

## 五、手动测试 C：MCP Client → Python → Unity

1. 保持 Unity **Start server** 与 Python 环境变量一致，例如：

   ```text
   UNITY_BRIDGE_ENDPOINT=http://127.0.0.1:7890
   ```

2. 启动 MCP Server（stdio，供 Cursor / Claude Desktop 使用）：

   ```powershell
   cd prototype\unity-mcp-min\Server
   python main.py
   ```

3. 在 MCP 宿主（如 Cursor）中确认已加载本 server，工具列表含：

   - `unity_reflect_call`
   - `unity_schema_hint`
   - `unity_validate_result`

4. 在对话中让模型执行与 **第三节** 等价的 `unity_reflect_call`（建议先 `dryRun: true`），观察返回是否与 curl 一致。

---

## 六、常见问题

| 现象 | 可能原因 |
|------|----------|
| `curl` 连接失败 | 桥未启动、防火墙、端口错误 |
| `health` 404 | URL 写错（需带 `/health`） |
| `unity_reflect_call` 返回 bridge 失败 | `UNITY_BRIDGE_ENDPOINT` 与 Unity 端口不一致；或 Unity 未监听 |
| dry-run 失败 “No overload matched” | `args` 数量或类型与 API 不符；检查 JSON |
| Unity Console 报错 | 主线程/反射异常，看 Unity 日志栈 |

---

## 七、自动化脚本

仓库提供一键脚本：**[`verify_unity_mcp.py`](../../prototype/unity-mcp-min/Server/scripts/verify_unity_mcp.py)**（相对本文件路径）。

用法：

```powershell
cd prototype\unity-mcp-min\Server
pip install -r requirements.txt
python scripts\verify_unity_mcp.py
python scripts\verify_unity_mcp.py --endpoint http://127.0.0.1:7890 --live-create
```

- 默认只测 **health + dry-run**（不写场景）。
- `--live-create` 会真实调用一次 `CreatePrimitive`（会改 Hierarchy，慎用）。

退出码：`0` 全部通过，`1` 有失败。
