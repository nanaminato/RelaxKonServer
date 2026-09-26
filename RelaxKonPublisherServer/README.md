# RelaxKon Publisher Server

本机发布者 API，仅绑定 `127.0.0.1` 和 `::1`。将 `appsettings.Local.example.json` 复制为已忽略的 `appsettings.Local.json` 后填写本机绝对路径；该文件可存放机器专属路径及签名配置，不能提交。

API 不提供网页、不写入所选官网 `Content/`，也不接收命令行参数。它只在独立输出目录中生成 `Downloads/`、`ReleaseDelivery/` 和 `publisher-manifest.json`。运行前端时默认调用 `http://127.0.0.1:5112/api/publisher`。

Android 发布采用“签名发布机 + 导入”模式：发布机在仓库外持有 keystore 和口令，生成已签名的 APK、AAB 及其 `release.json`；Publisher 只从 `appsettings.Local.json` 中配置的固定导入目录读取这些文件。导入时会验证 APK/AAB 签名、证书 SHA-256、APK 应用 ID/版本、发布清单和 SHA-256。`AndroidImport` 配置只能包含公开证书指纹、受控路径和验签工具路径，绝不能包含 keystore、alias 或口令。APK 才会加入公开下载清单；AAB 只归档以供 Play Console 上传。
