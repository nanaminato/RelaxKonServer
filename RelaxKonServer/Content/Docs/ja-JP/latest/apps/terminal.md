---
title: ターミナル
description: 永続的なリモート端末セッションを使用します。
category: アプリケーション
order: 14
---

# ターミナル

Terminal は SignalR によるリモート PTY セッションを提供します。出力はクライアントでローカル描画され、接続が切断されてもサーバーはセッションを維持できます。

## 操作例：実行ホストとユーザーの確認

1. ログイン済み Server ワークスペースでターミナルを開き、接続とセッションを確認します。SSH は SSH ホスト、ローカルはクライアント上で実行します。
2. Linux は `hostname`、`id`、`pwd`、Windows は `hostname`、`whoami` と使用中シェルの現在ディレクトリ命令で対象を確認します。
3. テストディレクトリで通常命令を実行し、出力を確認します。複数セッションでは入力先を先に確認します。
4. Server の短い切断後は再接続して復元状態を確認します。永続化はあらゆる再起動・サインアウトやローカル端末の復元を保証しません。Windows 個人 Server はサインアウト時に停止します。

![スクリーンショットの仮画像：撮影箇所：接続・セッション状態とホスト／ユーザー確認。Server、SSH、ローカルを明示し、実際のアカウントを隠します。](/assets/docs/screenshots/ja-JP/terminal-session.svg)

> 撮影箇所：接続・セッション状態とホスト／ユーザー確認。Server、SSH、ローカルを明示し、実際のアカウントを隠します。

管理者端末は認可を先に確認します。Windows 個人モードでは LocalSystem を使い、ユーザーのネットワークドライブを継承しません。通常命令の既定にしないでください。配置失敗は[保守](/docs/ja-JP/latest/apps/server-maintenance)の操作結果で調べ、導入を繰り返して推測しません。

## 概要

ターミナルコントロールは RoyalTerminal をベースに RemoteWindow へ埋め込まれます。リモートモードでは PTY がサーバー上で動作し、サーバーは**バイト中継**のみを行い、VT 描画はクライアントで完結します。

## 機能

- 永続セッション復元：再接続すると同じシェル状態に戻る
- ローカル描画：スクロールバック、選択、フォントはクライアント側
- 入力とサイズの同期
- 1 ユーザーにつき複数セッション
- 未サインイン時はローカル PTY へフォールバック

## アーキテクチャ

```text
TerminalControl (Client)
      |
SignalRTransport (ITerminalTransport)
      |
SignalR Hub /hubs/terminals  (JWT)
      |
TerminalHub → IPty (ConPTY / forkpty)
      |
Shell
```

## セキュリティ

- SignalR 接続は JWT で認証され、現在のユーザーにスコープされます
- サーバーはバイトを中継するだけで、コマンドを解析しません
- セッションプロセスはサインインしたホスト OS ユーザーで実行されます

## 関連ドキュメント

- [プロトコルと通信](/docs/ja-JP/latest/concepts/protocol)
