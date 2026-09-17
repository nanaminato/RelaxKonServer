---
title: ユーザーモードでのインストール（Linux）
description: 一般の Linux アカウントで RelaxKonOS サーバーを導入します。sudo は不要で、システムディレクトリも変更しません。
category: はじめに
order: 3
---

# ユーザーモードでのインストール（Linux）

**ユーザーモード**は「自分の Linux アカウントでサーバーを1つ動かしたい」という用途のための方式です。システムモードと同じ Server / Guardian バイナリを使いますが、永続状態はすべてそのアカウントの XDG ディレクトリに収めます。systemd のシステムユニットは作成せず、常駐する特権ヘルパーも導入せず、PAM・sudoers・ファイアウォール・`/etc` にも触れません。

## まず3つのインストール方法を区別する

RelaxKonOS のサーバーには用途が重複しない3つの導入方式があります。このページで扱うのは**ユーザーモード**のみです。

| 方式 | 権限 | 用途 | 取得するパッケージ |
| --- | --- | --- | --- |
| **ユーザーモード** | 一般アカウント、**root は拒否** | 個人が自分のアカウントでサーバーを動かす | `*-user-server.zip` |
| [システムモード](/docs/ja-JP/latest/getting-started/installation) | root / 管理者 | マルチユーザー本番環境、システムサービス登録 | `*-server.zip` |
| 開発者モード | .NET 10 SDK | RelaxKonOS 自体の開発 | ソースリポジトリ |

> **警告**: クライアント用パッケージとサーバー用パッケージは互換ではありません。ユーザーモードは manifest の `packageKind` が `user-server` のリリースバンドルのみを受け付けます。

### ユーザーモードとシステムモードの違い

| 項目 | ユーザーモード | システムモード |
| --- | --- | --- |
| 実行ユーザー | 自分のアカウント | 専用のサービスアカウント |
| サービス登録 | なし。`relaxkon` コマンドがプロセスを管理 | systemd ユニットまたは Windows サービス |
| 待ち受けアドレス | `127.0.0.1` のみ | 設定可能：ローカルのみ / LAN / リバースプロキシ |
| 特権ヘルパー | 導入しない（`Privileges: disabled`） | 導入し、固定の sudoers 規則から呼び出す |
| ホストへの変更 | XDG ディレクトリのみ | `/etc`、systemd、sudoers、ファイアウォール |
| アップグレード | `relaxkon upgrade`。準備完了の確認に失敗したら自動ロールバック | インストーラーを再実行 |
| アンインストール | `relaxkon uninstall` | リリースバンドル内のアンインストールスクリプト |

## インストール前の準備

- **一般（非 root）の Linux アカウント**。インストーラーとライフサイクルコマンドはいずれも root を拒否するため、`sudo` を使うとインストールは失敗します。
- システムコマンド: `bash`、`realpath`、`stat`、`find`、`sha256sum`、および `flock`（通常は `util-linux` に含まれます）。`flock` が無い場合は即座にエラーになります。
- HTTPS のリリース URL からオンラインインストールする場合は `curl` と `unzip` も必要です。
- `*-user-server.zip` リリースバンドルと、公開されている SHA-256。
- systemd、sudo、root はいずれも不要です。

> **Tip**: 公式サイトの[ダウンロードページ](https://relaxkon.com/downloads)に安定版チャネルのファイル名、サイズ、チェックサムが掲載されています。オフラインのサーバーにはサーバーパッケージだけをコピーしてください。

## 手順

### 1. リリースバンドルを展開する

対象アカウントで実行し、`sudo` は**使わないで**ください。

```bash
unzip RelaxKonOS-<version>-linux-x64-user-server.zip -d RelaxKonOS-user-server
```

### 2. インストーラーを実行する

```bash
./RelaxKonOS-user-server/deployment/user/install-relaxkonos.sh \
  --mode user \
  --bundle ./RelaxKonOS-user-server
```

インストーラーは次の順に処理します。

1. バンドルの完全性を検証（`manifest.json`、`payload/`、`deployment/user/relaxkon`）。
2. `manifest.json` の `schemaVersion` と `packageKind: "user-server"` を検証。
3. シンボリックリンクを含むバンドルを拒否。
4. `sha256sum` で全ファイルを検証し、ファイル一覧が実際の内容と完全に一致することを確認。
5. バージョンを `server/versions/<version>/` に配置し、`server/current` シンボリックリンクを切り替えて `bin/relaxkon` を更新。

公式リリース URL からのオンラインインストールも可能です。`--release-uri` は HTTPS である必要があり、64 桁の 16 進 SHA-256 が必須です。

```bash
./deployment/user/install-relaxkonos.sh \
  --mode user \
  --release-uri https://<host>/relaxkonos/stable/<version>/linux-x64/server/<archive>.zip \
  --release-sha256 <64-hex-sha256>
```

### 3. インストール先を確認する

ユーザーモードはそのアカウントの XDG ディレクトリだけに書き込み、権限は `0700` / `0600` です。

| 用途 | 既定のパス |
| --- | --- |
| プログラムとバージョンディレクトリ | `${XDG_DATA_HOME:-$HOME/.local/share}/relaxkonos/` |
| ライフサイクルコマンド | `${XDG_DATA_HOME:-$HOME/.local/share}/relaxkonos/bin/relaxkon` |
| 設定とシークレット | `${XDG_CONFIG_HOME:-$HOME/.config}/relaxkonos/`（`appsettings.user.json`、`secrets/guardian.secret`） |
| 実行状態 | `${XDG_STATE_HOME:-$HOME/.local/state}/relaxkonos/`（PID、制御ソケット、`install-state.json`、SQLite データベース） |
| ログ | `${XDG_STATE_HOME:-$HOME/.local/state}/relaxkonos/logs/{server,guardian}.log` |
| ダウンロードキャッシュ | `${XDG_CACHE_HOME:-$HOME/.cache}/relaxkonos/` |

インストーラーは `PATH` を変更せず、システム全体の実行ファイルも書き込みません。

## 起動と確認

```bash
RELAXKON=""${XDG_DATA_HOME:-$HOME/.local/share}/relaxkonos/bin/relaxkon""

"$RELAXKON" start
"$RELAXKON" status
```

`status` の期待する出力:

```text
RelaxKonOS User Mode is running (pid <n>, loopback 127.0.0.1:5000).
```

これはプロセスの有無を見るだけではありません。そのユーザー専用の制御ソケット（`…/relaxkonos/run/server.sock`、モード `0600`）経由で `/ready` を確認します。ソケットが準備できていなければ、成功と偽らず未準備であることを明示します。

その他のコマンド:

```bash
"$RELAXKON" start --foreground   # フォアグラウンドで実行し、出力を直接確認する
"$RELAXKON" stop                 # Server と Guardian を停止
```

サーバーは既定で `http://127.0.0.1:5000` を待ち受けます。ポートは変更できます。

```bash
RELAXKONOS_PORT=5100 "$RELAXKON" start
```

### 手元のマシンから接続する

ユーザーモードはループバックにのみバインドするため、リモート接続は SSH のローカル転送を使い、クライアントをローカルアドレスに向けます。

```bash
ssh -L 5000:127.0.0.1:5000 <user>@<server>
```

## アップグレード

```bash
"$RELAXKON" upgrade --bundle ./RelaxKonOS-<new-version>-linux-x64-user-server
```

アップグレードはサービスを停止し、新しいバージョンを導入して再起動し、準備完了を待ちます。**準備完了の確認に失敗した場合はアップグレード前のバージョンへ自動的に切り替わる**ため、起動しないサーバーが残ることはありません。

> **Note**: 同じバージョン番号を重複してインストールすることはできません。アップグレード時は新しいバージョン番号を使用してください。

## アンインストール

```bash
"$RELAXKON" uninstall
```

まずサービスを停止し、そのうえで data / config / state / cache の4ディレクトリを削除します。

> **警告**: `uninstall` はデータベース、設定、シークレット、ログをまとめて削除し、**復元できません**。残したいものがある場合は、先に `${XDG_STATE_HOME:-$HOME/.local/state}/relaxkonos/` と `${XDG_CONFIG_HOME:-$HOME/.config}/relaxkonos/` をバックアップしてください。

## よくある問題

| 症状 | 原因と対処 |
| --- | --- |
| `User Mode must not be installed as root.` | `sudo` を使ったか root に切り替えています。一般アカウントで再実行してください。 |
| `--mode user or --mode system is required.` | `--mode user` が抜けているか、`sudo` なしで `--mode system` を指定しています。 |
| `not a complete user-server bundle` | 展開したのが `*-user-server` バンドルではないか、不完全です。 |
| `bundle is not a user-server manifest` | バンドルの `manifest.json` が `packageKind: "user-server"` ではありません。 |
| `bundle file checksum verification failed` | ファイルが破損しています。再ダウンロードし、公開されている SHA-256 を確認してください。 |
| `another RelaxKonOS lifecycle operation is already running` | 別のセッションがライフサイクル操作を実行し `…/relaxkonos/run/launcher.lock` を保持しています。終了を待ってください。 |
| `flock is required for safe User Mode lifecycle operations` | `flock`（`util-linux`）がありません。導入して再試行してください。 |
| `status` はプロセスが動作中と表示するが制御ソケットが未準備 | `logs/server.log` を確認してください。初回起動の初期化中か、ポートが使用中であることが多いです。 |
| `version already installed: <version>` | そのバージョンは導入済みです。新しいバージョン番号を使うか、先に `uninstall` してください。 |

## ソースの確認とフィードバック

ユーザーモードの実装はリポジトリでそのまま読めます。

- ライフサイクルコマンド: `deployment/user/relaxkon`
- ユーザーモードの入口: `deployment/user/install-relaxkonos.sh`
- システムモードのインストーラー（比較用）: `deployment/bootstrap/install-relaxkonos.sh`

ソースコード、Issue、Pull Request はすべて [nanaminato/RelaxKonOS](https://github.com/nanaminato/RelaxKonOS) にあります。

## 次のステップ

- [クイックスタート](/docs/ja-JP/latest/getting-started/quick-start)
- [インストール](/docs/ja-JP/latest/getting-started/installation)
- [セキュリティモデル](/docs/ja-JP/latest/concepts/security)
