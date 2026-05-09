# com.mcpofunity.reflect-bridge — 安装说明

## 方式 A：从任意 Unity 工程引用本地包（推荐）

1. 复制或克隆本仓库后，记下包路径，例如：

   `…/prototype/unity-mcp-min/Packages/com.mcpofunity.reflect-bridge`

2. 打开 Unity 工程的 `Packages/manifest.json`，在 `dependencies` 里增加一行（路径按本机修改）：

```json
{
  "dependencies": {
    "com.mcpofunity.reflect-bridge": "file:../../../MCP-of-Unity/prototype/unity-mcp-min/Packages/com.mcpofunity.reflect-bridge"
  }
}
```

相对路径从「工程根目录下的 `Packages` 文件夹」算起；也可用绝对路径 `file:D:/...`。

3. 回到 Unity，等待 Package Manager 解析；首次会自动拉取依赖 **`com.unity.nuget.newtonsoft-json`**。

4. 菜单 **Window → MCP Reflect Bridge**：
   - 设置端口（默认与 Python 一致：**7890**）
   - 可选勾选 **Auto-start when Unity opens**
   - 点击 **Start server**

5. 浏览器或 curl 自检：`GET http://127.0.0.1:7890/health`

## 方式 B：作为仓库内嵌示例工程

将整个 `prototype/unity-mcp-min` 拷入解决方案并设为 Unity 工程根（含 `Assets` 与你的 manifest），同样用 `file:` 引用上述包路径。

## Python 侧

环境变量 `UNITY_BRIDGE_ENDPOINT` 与端口一致，例如 `http://127.0.0.1:7890`。

## Windows 备注

若 `HttpListener` 启动失败，可能与 URL ACL 有关；本包仅绑定 **127.0.0.1**，一般无需管理员权限。若仍失败，请查看 Console 日志。
