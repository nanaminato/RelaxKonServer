---
title: ターミナル
description: 永続的なリモート端末セッションを使用します。
category: アプリケーション
order: 14
---

# ターミナル

Terminal は SignalR によるリモート PTY セッションを提供します。出力はクライアントでローカル描画され、接続が切断されてもサーバーはセッションを維持できます。

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
