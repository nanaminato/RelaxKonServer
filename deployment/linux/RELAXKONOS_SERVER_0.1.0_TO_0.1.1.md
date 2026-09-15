# RelaxKonOS Server：从 0.1.0 重装至 0.1.1

本指引适用于已经通过 `systemd` 服务直接运行的旧版 RelaxKonOS Server。旧版缺少 PAM 相关 Helper 时，Web 登录可能失败；更新目标是安装包含该修复的 **0.1.1** Server 发布包。

不要把新包继续标记为 `0.1.0`：线上发布目录中的版本化 ZIP 是不可变的。必须先发布新的 `0.1.1` 包，再让 `latest` 描述文件指向它。

> 这不是官网 API/前端的 `update-relaxkon-website.sh` 流程。该脚本不能更新产品服务器，也不能修复 RelaxKonOS 登录。

## 0. 前提与安全边界

- 使用服务器控制台、root SSH 或另一个确认仍可登录的管理员会话执行。不要在唯一一个 SSH 会话中改动 PAM 后立即退出。
- 本流程会停止 `relaxkonos-server.service` 与 `relaxkonos-guardian.service`，会有短暂服务中断。
- **不要**在升级中加入 `--remove-data`。默认保留 `/var/lib/relaxkonos`，从而保留用户、工作区、业务数据库和证书数据。
- 卸载器会删除 `/etc/relaxkonos`、`/etc/sudoers.d/relaxkonos-helpers` 和由 RelaxKonOS 创建的 `/etc/pam.d/relaxkonos`。因此必须先备份和记录旧配置。

以下命令按默认安装路径写作：安装目录 `/opt/relaxkonos`，数据目录 `/var/lib/relaxkonos`。若旧服务使用其他路径，必须在所有命令中替换为实际路径。

## 1. 先发布可安装的 0.1.1

在构建机制作 Linux Server 包时，把 manifest、ZIP 文件名和 Release Tag 都设为 `0.1.1`。发布包必须是 `linux-x64` 的 **server** 包，而非 client 包。

将该包和 SHA-256 发布到官网 API 的 `Content/ReleaseDelivery`，并把 `latest/linux-x64.json` 切换到 0.1.1。确认公网下载描述正确后再操作产品服务器：

```bash
ORIGIN='https://downloads.relaxkon.com'
curl --fail --silent --show-error "$ORIGIN/relaxkonos/stable/latest/linux-x64.json"
```

返回内容必须包含 `"version": "0.1.1"`，并且 `url` 是 HTTPS ZIP 地址、`sha256` 是 64 位 SHA-256 值。

如果网站使用其他下载域名，将上面的 `ORIGIN` 换成实际域名；发布 0.1.1 前也要用该域名生成发布描述文件。

## 2. 在旧服务器记录配置并备份

先确认旧服务名称、启动路径和监听参数。以下命令只读取信息：

```bash
sudo systemctl status relaxkonos-server.service relaxkonos-guardian.service --no-pager
sudo systemctl cat relaxkonos-server.service
sudo sed -n '1,160p' /etc/relaxkonos/server.env
sudo cat /var/lib/relaxkonos/install-state.json
```

记录以下值，重装时必须保持一致：

- 安装目录与数据目录；
- `--network`：`local`、`lan` 或 `reverse-proxy`；
- `--server-port`；
- 证书模式：`none`、`custom` 或 `self-signed`；
- 文件访问范围：`restricted`、`whitelist` 或 `full`；白名单模式还需要原白名单文件。

在仍可使用 root 权限时备份配置和数据。该备份位于 `/root`，不应上传到公开的 GitHub Release：

```bash
sudo install -d -m 0700 /root/relaxkonos-backups
sudo tar --xattrs --acls --ignore-failed-read -czf \
  /root/relaxkonos-backups/relaxkonos-before-0.1.1.tar.gz \
  /etc/relaxkonos /etc/pam.d/relaxkonos /var/lib/relaxkonos
sudo chmod 0600 /root/relaxkonos-backups/relaxkonos-before-0.1.1.tar.gz
```

若 `systemctl cat` 显示的不是 `relaxkonos-server.service`，或 `ExecStart` 不在 `/opt/relaxkonos/`，不要直接执行下一步的默认卸载命令；先用实际服务名、安装目录和数据目录替换命令中的值。

## 3. 下载 0.1.1 的引导脚本并卸载旧版

将新的引导脚本保存到磁盘，而不是直接管道执行。这样 sudo 提升、日志排查和版本核对都更可靠：

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

卸载旧 0.1.0。**不要添加 `--remove-data`**；交互确认时输入 `y`：

```bash
sudo bash "$WORK/uninstall-relaxkonos.sh" \
  --install-root /opt/relaxkonos \
  --data-root /var/lib/relaxkonos
```

卸载后验证旧服务已停止，数据仍在：

```bash
sudo systemctl is-active relaxkonos-server.service || true
sudo systemctl is-active relaxkonos-guardian.service || true
sudo test -f /var/lib/relaxkonos/install-state.json && echo 'Data retained.'
```

## 4. 重装 0.1.1

### 推荐：交互式安装

使用刚下载的 0.1.1 引导脚本。它会读取 `latest/linux-x64.json`、下载 ZIP、验证 SHA-256 后安装。按第 2 步记录的旧值选择网络、证书和文件访问模式：

```bash
sudo bash "$WORK/install-relaxkonos.sh" \
  --release-catalog-base "$ORIGIN/relaxkonos/stable/latest" \
  --install-root /opt/relaxkonos \
  --data-root /var/lib/relaxkonos
```

当安装器询问来源时，选择“官方稳定版”。安装器显示的版本必须是 `0.1.1`。

### 无人值守模板

只有在已确认旧配置值时才使用 `--non-interactive`。以下是“反向代理、本机 HTTP 5000、无 Kestrel 证书、受限文件访问”的示例；它不是所有服务器的通用默认值：

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

若旧安装使用自己的 PFX，补充 `--certificate-mode custom --certificate-path PATH --certificate-password-file PATH`；若使用白名单，补充 `--file-access whitelist --file-roots PATH`。这些文件必须在第 3 步卸载之前已备份到卸载器不会删除的位置。

## 5. 验证 PAM 修复和服务状态

先不要关闭当前管理员会话。确认安装器已经重新创建服务、sudo 规则和 PAM 服务文件：

```bash
sudo systemctl status relaxkonos-server.service relaxkonos-guardian.service --no-pager
sudo journalctl -u relaxkonos-server.service -u relaxkonos-guardian.service -n 150 --no-pager
sudo visudo -cf /etc/sudoers.d/relaxkonos-helpers
sudo sed -n '1,20p' /etc/pam.d/relaxkonos
```

正常情况下 `/etc/pam.d/relaxkonos` 的第一行是：

```text
# Managed by RelaxKonOS PAM authentication service.
```

随后应仅包含认证与账户策略（`common-auth`、`common-account`），不应把 login 或 session 堆栈写入该服务文件。

健康检查地址取决于第 4 步选择的协议和端口。上面无人值守示例对应：

```bash
curl --fail http://127.0.0.1:5000/healthz
```

最后在保留当前 root/控制台会话的前提下，用一个非关键测试账户从客户端登录。确认登录成功、服务稳定后再关闭管理员会话。

## 故障处理

- `latest/linux-x64.json` 仍显示 0.1.0：停止，不要卸载；先完成 0.1.1 发布和 latest 切换。
- 卸载器提示无法识别安装目录：核对旧 `ExecStart`，传入正确的 `--install-root`；不要手工删除未知目录。
- 新安装器无法健康检查：保留当前日志，执行 `journalctl -u relaxkonos-server.service -n 150 --no-pager`；不要在没有备份的情况下删除 `/var/lib/relaxkonos`。
- PAM 文件不是 RelaxKonOS 标记文件：安装器会拒绝覆盖它。这是保护管理员自定义 PAM 配置的预期行为，必须先人工审查该文件。
