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

## サーバーの起動

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

- [クイックスタート](/docs/ja-JP/latest/getting-started/quick-start)
- [セキュリティモデル](/docs/ja-JP/latest/concepts/security)
