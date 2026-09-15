# RelaxKon 官网服务器部署指引

本目录用于把 RelaxKon 官网前端、`RelaxKonServer` API 和 RelaxKonOS 的联网安装服务部署到一台 Debian 12 或 Ubuntu 22.04/24.04/26.04 服务器。请将本目录全部上传到服务器，例如 `/root/relaxkon-deploy/`。

`install-relaxkon-website.sh` 从 GitHub Release 下载并验证四个文件：

```text
RelaxKonServer-VERSION-linux-x64.zip
RelaxKonServer-VERSION-linux-x64.zip.sha256
RelaxKon-web-VERSION.zip
RelaxKon-web-VERSION.zip.sha256
```

其中 Server ZIP 必须已经包含 `Content/ReleaseDelivery` 内的 RelaxKonOS 发布包、安装器和 `latest/*.json` 描述文件；这些才是网站向用户提供联网安装服务的数据来源。

## 1. 首次部署（HTTP）

先在 GitHub Release 上传上面的四个文件。然后在服务器执行：

```bash
cd /root/relaxkon-deploy

sudo bash install-relaxkon-website.sh \
  --repository nanaminato/RelaxKonOS \
  --tag v0.1.0 \
  --version 0.1.0
```

脚本自动从下列地址下载发布物：

```text
https://github.com/nanaminato/RelaxKonOS/releases/download/v0.1.0/RelaxKonServer-0.1.0-linux-x64.zip
https://github.com/nanaminato/RelaxKonOS/releases/download/v0.1.0/RelaxKonServer-0.1.0-linux-x64.zip.sha256
https://github.com/nanaminato/RelaxKonOS/releases/download/v0.1.0/RelaxKon-web-0.1.0.zip
https://github.com/nanaminato/RelaxKonOS/releases/download/v0.1.0/RelaxKon-web-0.1.0.zip.sha256
```

如果不使用标准 GitHub 地址，可直接指定 Release 资源目录：

```bash
sudo bash install-relaxkon-website.sh \
  --release-base-uri 'https://github.com/nanaminato/RelaxKonOS/releases/download/v0.1.0' \
  --version 0.1.0
```

私有 Release 在执行前设置 `GITHUB_TOKEN`。若 ZIP 文件名不同，额外传入 `--server-asset NAME.zip --web-asset NAME.zip`；校验文件仍须分别追加 `.sha256`。

首次部署会安装 Nginx、创建 `relaxkon-server.service`，并只开启 HTTP，便于进行 ACME HTTP-01 验证。文件部署在 `/srv/relaxkon/`；不要手动改写 `current` 软链接。

## 2. 配置 DNS 与证书

将主域、`www` 和下载域的 A/AAAA 记录都指向此服务器。默认域名是 `relaxkon.com`、`www.relaxkon.com`、`downloads.relaxkon.com`。如果使用别的域名，首次安装、证书和 HTTPS 启用三个命令都传入相同的 `--domain`、`--www-domain`、`--downloads-domain` 参数。

申请 Let's Encrypt 证书：

```bash
sudo apt-get install -y certbot
sudo certbot certonly --webroot -w /srv/relaxkon/acme \
  -d relaxkon.com \
  -d www.relaxkon.com \
  -d downloads.relaxkon.com
```

证书成功签发后，切换 Nginx 至 HTTPS：

```bash
sudo bash enable-relaxkon-https.sh
```

验证服务：

```bash
curl --fail https://downloads.relaxkon.com/api/health
curl --fail https://downloads.relaxkon.com/relaxkonos/stable/latest/linux-x64.json
```

## 3. 用户联网安装命令

HTTPS 已启用且 `latest/*.json` 指向正确下载域名后，用户可使用：

```bash
curl -fsSL https://downloads.relaxkon.com/relaxkonos/stable/latest/bootstrap/install-relaxkonos.sh | sudo bash
```

```powershell
irm https://downloads.relaxkon.com/relaxkonos/stable/latest/install.ps1 | iex
```

## 4. 更新

更新脚本会先停止旧的 API 服务、保留 Nginx、证书和已发布文件，再下载、校验、解压并切换新版。已有标准 Let's Encrypt 证书时，脚本会自动恢复 HTTPS。

```bash
cd /root/relaxkon-deploy
sudo bash update-relaxkon-website.sh \
  --repository nanaminato/RelaxKonOS \
  --tag v0.1.0 \
  --version 0.1.0
```

## 5. 卸载

默认卸载只移除服务和 Nginx 站点配置，保留证书与已下载发布物：

```bash
sudo bash uninstall-relaxkon-website.sh
```

确认不再需要旧发布物后，才删除发布目录：

```bash
sudo bash uninstall-relaxkon-website.sh --remove-releases
```

## 域名与发布描述的重要约束

`latest/*.json` 中的 ZIP URL 在发布时生成。当前项目默认是 `https://downloads.relaxkon.com`。如改用 `downloads.relaxkonos.com`，应在制作 Server ZIP **之前**运行 `Publish-RelaxKonOSRelease.ps1 -PublicBaseUri https://downloads.relaxkonos.com`，再打包并上传新的 `RelaxKonServer` ZIP；仅更改 Nginx 域名不足以改变安装器下载地址。
