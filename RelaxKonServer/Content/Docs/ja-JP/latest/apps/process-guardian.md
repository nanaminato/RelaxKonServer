---
title: プロセスガーディアン
description: 保護対象のワークロードを宣言し、重要なプロセスを稼働し続けます。
category: アプリケーション
order: 30
---

# プロセスガーディアン

プロセスガーディアンは、重要なワークロードを稼働し続けるための仕組みです。

## 概要

昇格した権限で動作する独立した Guardian Agent プロセスが、認証済みのローカル名前付きパイプを通じてサーバーから指示を受け取ります。

## 機能

- 永続化を伴う宣言的なワークロード定義
- 開始、停止、再起動
- 保護対象サーバープロセスのヘルス監視
- Guardian ログを SignalR（`/hubs/guardian-logs`）でクライアントへ配信

## アーキテクチャ

```text
Client
  |
SignalR /hubs/guardian-logs
  |
RelaxKonOS.Server  (IProcessGuardianService)
  |
Named pipe IPC (local authentication)
  |
RelaxKonOS.Guardian.Agent
  |
WorkloadSupervisor → guarded processes
```

## 権限とセキュリティ

- エージェントはサーバーから分離され、権限境界が明確になります
- IPC は認証付きのローカル名前付きパイプで、正当なプロセスのみが接続できます
- ヘルスチェックとネイティブサービスアダプター（systemd / SCM）は現在も進化中です

## 関連ドキュメント

- [タスクマネージャー](/docs/ja-JP/latest/apps/task-manager)
- [セキュリティモデル](/docs/ja-JP/latest/concepts/security)
