---
title: インストール
description: RelaxKonOS のクライアントとサーバーを準備してインストールします。
category: はじめに
order: 2
---

# インストール

RelaxKonOS は**クライアント**と**サーバー**で構成されます。クライアントは毎日使うデバイスに、サーバーはワークスペースを常時保持したいマシンに配置します。

## 前提条件

- **.NET 10.0 SDK** 以降
- クライアント OS：Windows 10/11、macOS、Ubuntu 20.04+
- サーバー OS：Ubuntu 20.04+ または Windows Server 2016+

## まずサーバーのインストール方法を選ぶ

| 方式 | 権限 | 説明 |
| --- | --- | --- |
| [ユーザーモード](/docs/ja-JP/latest/getting-started/user-mode) | sudo 不要 | 一般の Linux アカウントでサーバーを実行。`127.0.0.1` のみ待ち受け、システムディレクトリは変更しません |
| システムモード | root / 管理者 | ワンコマンド インストーラーがシステムサービスと特権ヘルパーを登録。マルチユーザー本番環境向け |
| ソースから実行 | .NET 10 SDK | 以下に示す手順。開発とデバッグ向けです |

> **Note**: 以下の手順は .NET SDK を必要とし、ソースツリーを直接実行するもので、**一般ユーザー向けのインストール方法ではありません**。サーバーを配備する場合は、先に[ユーザーモードでのインストール](/docs/ja-JP/latest/getting-started/user-mode)または公式サイトの[ダウンロードページ](https://relaxkon.com/downloads)をお読みください。

## ソースからサーバーを起動する

```bash
cd RelaxKonOS.Server
dotnet run
```

本番環境では `appsettings.json` の `Jwt:Secret` を 32 文字以上のランダム文字列に変更し、リバースプロキシで HTTPS を終端してください。

## クライアントの起動

```bash
cd Client/RelaxKonOS.Client.Desktop
dotnet run
```

ログインウィンドウが開きます。**ホスト OS** のアカウント資格情報でサインインします。

## 初回サインイン

1. クライアントを起動してログインウィンドウを待つ
2. サーバーマシンのホスト OS 資格情報を入力
3. デスクトップが開き、ワークスペースが作成・同期されます

> ID はホスト OS（Windows LogonUser / Linux PAM + NSS）が検証します。RelaxKonOS がパスワードを保存することはありません。

## 次のステップ

- [ユーザーモードでのインストール（Linux）](/docs/ja-JP/latest/getting-started/user-mode)
- [クイックスタート](/docs/ja-JP/latest/getting-started/quick-start)
- [セキュリティモデル](/docs/ja-JP/latest/concepts/security)
- ソースと Issue: [nanaminato/RelaxKonOS](https://github.com/nanaminato/RelaxKonOS)
