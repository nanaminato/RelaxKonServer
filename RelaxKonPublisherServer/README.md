# RelaxKon Publisher Server

本机发布者 API，仅绑定 `127.0.0.1` 和 `::1`。将 `appsettings.Local.example.json` 复制为已忽略的 `appsettings.Local.json` 后填写本机绝对路径；该文件可存放机器专属路径及签名配置，不能提交。

API 不提供网页、不写入所选官网 `Content/`，也不接收命令行参数。它只在独立输出目录中生成 `Downloads/`、`ReleaseDelivery/` 和 `publisher-manifest.json`。运行前端时默认调用 `http://127.0.0.1:5112/api/publisher`。
