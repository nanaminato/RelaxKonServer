# RelaxKon 0.1.1 完整升级手册

本手册按实际依赖关系升级两个不同的服务器：

1. **RelaxKon 官网服务器**：运行 `RelaxKonServer` API、官网前端和下载服务（通常是 `relaxkon.com` / `downloads.relaxkon.com`）。
2. **RelaxKonOS 产品服务器**：运行 `relaxkonos-server.service` 和 `relaxkonos-guardian.service`，由官网下载的脚本安装和管理。

必须先升级官网服务器。新版官网 Server 包中的 `Content/ReleaseDelivery` 才包含 RelaxKonOS 0.1.1 的发布包、`latest` 描述文件，以及新版安装/卸载脚本。官网更新成功且下载地址验证通过后，才能在产品服务器上卸载 0.1.0 并安装 0.1.1。

> 本流程会移除旧 RelaxKonOS 安装和数据目录。仅在确认 `/var/lib/relaxkonos` 没有需要保留的数据后继续；否则先备份并停止。

## 0. 发布物与执行顺序

GitHub Release [`v0.1.1`](https://github.com/nanaminato/RelaxKonOS/releases/tag/v0.1.1) 必须已经包含以下四个官网部署文件：

```text
RelaxKonServer-0.1.1-linux-x64.zip
RelaxKonServer-0.1.1-linux-x64.zip.sha256
RelaxKon-web-0.1.1.zip
RelaxKon-web-0.1.1.zip.sha256
```

按下面顺序执行，不要跳过第 2 步的公网验证：

```text
1. 将本目录上传到官网服务器
2. 官网服务器：update-relaxkon-website.sh 移除旧官网并安装 0.1.1
3. 官网服务器：确认 latest/linux-x64.json 已返回 0.1.1
4. 产品服务器：从新版官网下载 uninstall-relaxkonos.sh，卸载旧 RelaxKonOS
5. 产品服务器：从新版官网下载 install-relaxkonos.sh，安装 RelaxKonOS 0.1.1
6. 产品服务器：验证 systemd、PAM、日志与健康检查
```

`update-relaxkon-website.sh` 会调用同目录的安装、卸载和 HTTPS 脚本。因此必须上传**整个** `deployment/linux` 目录，不能只上传一个更新脚本。

## 1. 将官网部署脚本上传到官网服务器

以下命令在构建机的仓库根目录执行。将 `<WEBSITE_HOST>` 替换为官网服务器的 IP 或主机名；使用其他 SSH 用户时相应替换 `root`。

```powershell
ssh root@<WEBSITE_HOST> "install -d -m 0700 /root/relaxkon-deploy"
scp -r .\RelaxKonServer\deployment\linux root@<WEBSITE_HOST>:/root/relaxkon-deploy/
```

上传完成后登录官网服务器，并确认脚本齐全：

```bash
ssh root@<WEBSITE_HOST>
cd /root/relaxkon-deploy/linux
ls -1 update-relaxkon-website.sh install-relaxkon-website.sh \
  uninstall-relaxkon-website.sh enable-relaxkon-https.sh
```

## 2. 官网服务器：移除旧 RelaxKon Server 并安装 0.1.1

在官网服务器的 `/root/relaxkon-deploy/linux` 中运行：

```bash
sudo bash update-relaxkon-website.sh \
  --repository nanaminato/RelaxKonOS \
  --tag v0.1.1 \
  --version 0.1.1
```

该脚本会按以下顺序执行：

1. 停止并删除旧 `relaxkon-server.service`。
2. 删除旧 Nginx 站点配置，以及 `/srv/relaxkon/api/releases` 和 `/srv/relaxkon/frontend/releases` 中的旧发布内容。
3. 从 GitHub Release 下载四个 0.1.1 文件，验证 SHA-256，并部署新的 API 与前端。
4. 恢复 `relaxkon-server.service`、Nginx 和 HTTPS；已有 ACME 证书会保留并复用。

默认域名为 `relaxkon.com`、`www.relaxkon.com` 和 `downloads.relaxkon.com`。如实际域名不同，必须在同一命令中补充一致的 `--domain`、`--www-domain` 与 `--downloads-domain` 参数。

如果 Release 是私有的，先在官网服务器设置令牌再执行更新：

```bash
export GITHUB_TOKEN='<GitHub fine-grained access token>'
```

## 3. 官网服务器：确认 0.1.1 已对外提供

官网更新脚本成功结束后，在任意可访问公网的终端执行：

```bash
ORIGIN='https://downloads.relaxkon.com'
curl --fail --silent --show-error "$ORIGIN/api/health"
curl --fail --silent --show-error \
  "$ORIGIN/relaxkonos/stable/latest/linux-x64.json"
```

第二条命令的输出必须包含：

```json
"version": "0.1.1"
```

并且 `url` 必须是 HTTPS 地址、`sha256` 必须是 64 位 SHA-256。若这里仍是 `0.1.0` 或请求失败，停止流程，先修复官网部署；**不要**卸载产品服务器上的旧 RelaxKonOS。

## 4. 产品服务器：下载新版脚本并卸载旧 RelaxKonOS

在需要升级的 RelaxKonOS 产品服务器上执行。请保留一个 root SSH/控制台会话，直到最终测试登录完成；不要在唯一的管理员会话中改动 PAM 后立即断开。

先确认旧服务和目录。以下命令只读取信息：

```bash
sudo systemctl status relaxkonos-server.service relaxkonos-guardian.service --no-pager
sudo systemctl show relaxkonos-server.service -p ExecStart --no-pager
sudo ls -ld /opt/relaxkonos /var/lib/relaxkonos /etc/relaxkonos 2>/dev/null || true
```

默认安装目录为 `/opt/relaxkonos`，数据目录为 `/var/lib/relaxkonos`。若输出显示其他路径，以下命令必须替换为已核实的实际路径。

下载**刚刚由新版官网提供**的卸载与安装脚本到本地文件：

```bash
ORIGIN='https://downloads.relaxkon.com'
WORK=/root/relaxkonos-upgrade-0.1.1
sudo install -d -m 0700 "$WORK"
sudo curl --fail --location --show-error \
  "$ORIGIN/relaxkonos/stable/latest/bootstrap/uninstall-relaxkonos.sh" \
  -o "$WORK/uninstall-relaxkonos.sh"
sudo curl --fail --location --show-error \
  "$ORIGIN/relaxkonos/stable/latest/bootstrap/install-relaxkonos.sh" \
  -o "$WORK/install-relaxkonos.sh"
sudo chmod 0700 "$WORK"/*.sh
```

先用官网提供的卸载脚本移除服务、程序文件、Helper 配置和 RelaxKonOS 管理的 PAM 文件。在交互确认时输入 `y`：

```bash
sudo bash "$WORK/uninstall-relaxkonos.sh" \
  --install-root /opt/relaxkonos \
  --data-root /var/lib/relaxkonos
```

为完成本次“彻底替换”，删除旧数据目录。新版卸载器会保护没有 `install-state.json` 的旧数据目录，因此这是旧版 0.1.0 升级时必需的单独一步。再次确认目标无误后执行：

```bash
sudo rm -rf -- /var/lib/relaxkonos
sudo test ! -e /opt/relaxkonos && echo 'Program files removed.'
sudo test ! -e /var/lib/relaxkonos && echo 'Data removed.'
```

## 5. 产品服务器：安装新的 RelaxKonOS 0.1.1

推荐使用交互式安装，让操作者按实际网络、证书和文件访问需求作选择：

```bash
sudo bash "$WORK/install-relaxkonos.sh" \
  --release-catalog-base "$ORIGIN/relaxkonos/stable/latest" \
  --install-root /opt/relaxkonos \
  --data-root /var/lib/relaxkonos
```

安装器中选择“官方稳定版”。显示的发布版本必须是 `0.1.1`。下面仅是已确认使用反向代理、本机 HTTP 5000、无 Kestrel 证书、受限文件访问时的无人值守示例；不要把它当作所有服务器的默认配置：

```bash
sudo bash "$WORK/install-relaxkonos.sh" \
  --release-catalog-base "$ORIGIN/relaxkonos/stable/latest" \
  --install-root /opt/relaxkonos \
  --data-root /var/lib/relaxkonos \
  --network reverse-proxy \
  --server-port 5000 \
  --certificate-mode none \
  --file-access restricted \
  --non-interactive
```

## 6. 最终验证

保持当前 root/控制台会话，不要急于关闭。确认服务、PAM 和日志正常：

```bash
sudo systemctl status relaxkonos-server.service relaxkonos-guardian.service --no-pager
sudo journalctl -u relaxkonos-server.service -u relaxkonos-guardian.service -n 150 --no-pager
sudo visudo -cf /etc/sudoers.d/relaxkonos-helpers
sudo sed -n '1,20p' /etc/pam.d/relaxkonos
curl --fail http://127.0.0.1:5000/healthz
```

`/etc/pam.d/relaxkonos` 的第一行应为：

```text
# Managed by RelaxKonOS PAM authentication service.
```

若第 5 步选择的端口、协议或网络模式不同，最后一条健康检查命令也要相应调整。最后在保留当前管理员会话的前提下，用非关键测试账户登录验证；确认成功后再结束管理员会话。

## 故障停点

- 官网 `latest/linux-x64.json` 不是 `0.1.1`：不要卸载产品服务器，先重新部署官网。
- 官网更新失败：查看 `journalctl -u relaxkon-server.service -n 100 --no-pager` 和 `nginx -t`。
- 卸载器拒绝删除目录：根据第 4 步 `ExecStart` 输出核实安装路径；不要猜测或扩大删除范围。
- 新服务不能通过健康检查：执行 `journalctl -u relaxkonos-server.service -u relaxkonos-guardian.service -n 150 --no-pager`，保留当前管理员会话以便回滚或排查。
