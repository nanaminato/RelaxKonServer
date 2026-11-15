# RelaxKon Publisher Server

本机发布者 API，仅绑定 `127.0.0.1` 和 `::1`。将 `appsettings.Local.example.json` 复制为已忽略的 `appsettings.Local.json` 后填写本机绝对路径；该文件可存放机器专属路径及签名配置，不能提交。

API 不提供网页、不写入所选官网 `Content/`，也不接收命令行参数。独立输出包含发布清单 `publisher-manifest.json`，以及所选任务对应的 `Downloads/`、`ReleaseDelivery/` 或 `ModuleDelivery/`。运行前端时默认调用 `http://127.0.0.1:5112/api/publisher`。

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
| POST | `api/publisher/modules` | 按 `relaxKonOSPath` 发现配置了发布的服务端模块和可选 RID |
| POST | `api/publisher/preview` | 预检、既有包与计划变更（`PublisherPreview`） |
| POST | `api/publisher/generate` | 启动发布任务，返回 202 与 `PublisherJob` |
| GET | `api/publisher/jobs/{id}` | 任务状态、进度、日志与受影响文件 |
| POST | `api/publisher/jobs/{id}/cancel` | 请求取消 |

SignalR Hub 在 `/hubs/publisher`：`Subscribe(jobId)` 后接收 `jobUpdated`，`StartPreview(request)` 可直接在连接上取预检结果。任务与日志只保存在进程内存里（每个任务最多 1000 条日志），进程重启即丢失。

## 独立模块批量发布

构建本服务需要同级的 RelaxKonOS 源码仓库，以引用共享打包和验签项目。工作台读取所选 RelaxKonOS 源码下 `ServerModules/<项目>/module-release.json`。配置声明模块 ID、显示名、同目录项目文件、入口程序集和类型、运行模式、Host API 版本区间及支持的 RID。CloudStorage 提供了可复用的示例。可选字段 `capabilities`、`dependencies`、`privileges`、`ui` 沿用模块清单结构。

在本机 `appsettings.Local.json` 填写 `ModulePublishing`。`ArtifactOrigin` 为模块归档所在的 HTTPS 根地址，`PrivateKeyPath` 指向仓库外的 ECDSA P-256 PEM 私钥。每个独立输出目录对应一个发布来源。后续批次应沿用该输出目录及其签名历史。首次接续已有线上来源时，先导入其现有签名目录和归档；`MinimumSequence` 可设置本次允许使用的最小新序号。公钥和来源授权由目标服务器管理员配置。

在工作台勾选多个模块及各自的 RID，填入三段数字版本号后预检并生成。每个模块与 RID 组合生成一个框架依赖的 `.rkmodule`。暂存区通过服务端同一套验包逻辑后，工作台提交 `ModuleDelivery/`，其中包含归档、统一签名的 `catalog.json` 和 `public-key.spki`。已发布的模块、版本和 RID 组合不可覆盖，既有发布条目和撤销记录会保留，序号递增。对外分发时将归档放到签名目录声明的 URL，全部可用后再更新线上目录。

### 官方静态来源

官方来源使用示例配置中的 `official-modules`、`relaxkonos` 和 `https://modules.relaxkon.com/`。将 `modules.relaxkon.com` 的 DNS 指向分发服务器，安装覆盖该域名的 TLS 证书，在 Nginx 中启用 [独立虚拟主机](../deployment/nginx/modules.relaxkon.com.conf)，运行 `nginx -t` 后重载。

1. 将 `ModuleDelivery/` 内的 `.rkmodule` 文件发布到 `/srv/relaxkon/modules/` 根目录。确认签名目录中声明的包 URL 均可下载，既有版本文件须保留原内容。
2. 将签名目录原子替换到 `/srv/relaxkon/modules/stable/catalog.json`。在当前目录过期前发布有效的新目录，并递增序号。
3. 通过独立可信渠道交付 `public-key.spki`，由 RelaxKonOS 管理员配置到 `ModulePackages:Sources:official-modules:PublicKeysSpki` 并启用该来源。私钥留在签名发布机。

## 验证

`Tests/PublisherPackaging.Tests` 是引用本服务的控制台测试工程，覆盖打包不变量：Linux 脚本必须是 LF、`verify-release-inventory.py` 只进 Linux 包、`manifest.json.files` 重复生成时不含自身、macOS 说明文件在清单生成前写入。

```bash
dotnet run --project ../Tests/PublisherPackaging.Tests
```

从包含两个仓库的父目录运行 CloudStorage 双 RID 验收。测试使用一次性密钥，检查发现、预检、实际发布、目录验签和后续批次保留旧条目。

```powershell
dotnet run --project RelaxKonServer/Tests/PublisherPackaging.Tests -- --module-acceptance RelaxKonOS RelaxKonServer/RelaxKonServer RelaxKonServer/RelaxKonPublisherServer/artifacts
```
