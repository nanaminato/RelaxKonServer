---
title: Docker マネージャー
description: ホストの Docker Engine を管理します：コンテナー、イメージ、スタック、ネットワーク、ボリューム。
category: アプリケーション
order: 28
---

# Docker マネージャー

Docker マネージャー（RemoteDocker）は、デスクトップから**サーバーホスト**の Docker Engine を操作します。

## 機能

- エンジン状態の検出とインストールガイダンス
- コンテナー：表示、起動、停止、再起動
- イメージ：一覧と取得
- スタック：Compose の検証、デプロイ、停止
- ネットワークとボリューム：表示と管理

## 実装

サーバーは `IDockerEngineService` を通じて `docker` CLI を呼び出し、`IDockerComposeService` で Compose を処理します。エンドポイントは `/api/v1.0/docker/*` です。

## 権限とセキュリティ

- 操作はホスト OS 上のサービスプロセス ID で実行されます
- 変更操作にはホストの権限（`docker` グループや root など）が必要です
- 独自の資格情報ストアは持ち込みません

## 関連ドキュメント

- [プロセスガーディアン](/docs/ja-JP/latest/apps/process-guardian)
- [セキュリティモデル](/docs/ja-JP/latest/concepts/security)
