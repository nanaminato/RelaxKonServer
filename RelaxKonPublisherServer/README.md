# RelaxKon Publisher Server

本机发布者 API，仅绑定 `127.0.0.1` 和 `::1`。将 `appsettings.Local.example.json` 复制为已忽略的 `appsettings.Local.json` 后填写本机绝对路径；该文件可存放机器专属路径及签名配置，不能提交。

API 不提供网页、不写入所选官网 `Content/`，也不接收命令行参数。它只在独立输出目录中生成 `Downloads/`、`ReleaseDelivery/` 和 `publisher-manifest.json`。运行前端时默认调用 `http://127.0.0.1:5112/api/publisher`。

替换独立输出时，旧内容先整目录改名到 `artifacts/recovery/<时间戳>-<任务号>/`，再移入已校验的暂存输出；成功后保留最近 5 份恢复副本。整目录改名若被其它进程短暂占用（资源管理器、杀毒软件、索引服务或正在删除的目录）会按退避重试约 14 秒，仍失败则以改名的原名回滚到替换前内容，不会递归删除新旧任一侧。错误信息会指出被占用的路径。

Android 发布采用“签名发布机 + 导入”模式：发布机在仓库外持有 keystore 和口令，生成已签名的 APK、AAB 及其 `release.json`；Publisher 只从 `appsettings.Local.json` 中配置的固定导入目录读取这些文件。导入时会验证 APK/AAB 签名、证书 SHA-256、APK 应用 ID/版本、发布清单和 SHA-256。

`AndroidImport` 配置只能包含公开证书指纹、受控路径和验签工具路径，绝不能包含 keystore、alias 或口令。APK 才会加入公开下载清单；AAB 只归档以供 Play Console 上传。

Linux 服务端打包必须复制 `deployment/user/` 用户模式入口和 `deployment/verify-release-inventory.py`，在 payload 中声明 `launcher`，并在将部署 Shell 脚本规范为 LF 后生成 `manifest.json.files`（相对路径、长度及 SHA-256）。预检及清单生成前会检查用户入口、校验器和必需安装/卸载脚本是否存在。无扩展名的 `relaxkon` 同样规范为 LF。客户端包同样生成文件清单，macOS 说明文件在清单生成前写入。修复发布者后须重新生成旧发布包；此前漏打校验器或文件清单的 ZIP 不能直接部署。

独立组件服务沿用当前服务端打包流程：Server、Guardian、Helper 与 ServerModuleHost 分别自包含发布，完整发布目录和平台部署脚本一起进入 ZIP，payload 键依次为 `server`、`guardian`、`privilegedHelper`、`moduleHost`。ServerModuleHost 必须打在 `payload/<platform>/server/module-host/`：安装器要在 Server 载荷旁拿到这个自包含运行时，缺了它个人模式与系统模式在安装 Helper 阶段都会失败。Windows 的 `--component-service` 宿主编译在 Helper 中，安装组件时将完整 Helper 发布目录复制到独立且受保护的宿主目录；Linux FRP 的 systemd 服务由 Helper 注册，部署脚本写入受保护的包来源策略。

因此不需要新增组件宿主项目或额外 payload。Nginx、Mihomo、FRP 的运行时仍由组件安装流程获取和校验，不直接内嵌到服务端 ZIP。Server 与 Helper 必须使用同一源码构建，当前共享 Helper 协议标识为 `1.0`，接口保留独立服务模型；修改协议后需重新生成发布包并同步部署两端。

## 接口

| 方法 | 路由 | 说明 |
| --- | --- | --- |
| GET | `api/publisher/settings` | 返回 `PublisherPaths` 配置 |
| POST | `api/publisher/preview` | 预检、既有包与计划变更（`PublisherPreview`） |
| POST | `api/publisher/generate` | 启动发布任务，返回 202 与 `PublisherJob` |
| GET | `api/publisher/jobs/{id}` | 任务状态、进度、日志与受影响文件 |
| POST | `api/publisher/jobs/{id}/cancel` | 请求取消 |

SignalR Hub 在 `/hubs/publisher`：`Subscribe(jobId)` 后接收 `jobUpdated`，`StartPreview(request)` 可直接在连接上取预检结果。任务与日志只保存在进程内存里（每个任务最多 1000 条日志），进程重启即丢失。

## 验证

`Tests/PublisherPackaging.Tests` 是引用本服务的控制台测试工程，覆盖打包不变量：Linux 脚本必须是 LF、`verify-release-inventory.py` 只进 Linux 包、`manifest.json.files` 重复生成时不含自身、macOS 说明文件在清单生成前写入。

```bash
dotnet run --project ../Tests/PublisherPackaging.Tests
```
