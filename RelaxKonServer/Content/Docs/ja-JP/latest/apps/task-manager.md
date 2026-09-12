---
title: タスクマネージャー
description: ホスト OS のリアルタイム性能指標とプロセスを表示します。
category: アプリケーション
order: 26
---

# タスクマネージャー

タスクマネージャー（RemoteTaskManager）は **サーバーホスト**の実際の状態を表示します。

## パフォーマンスタブ

- CPU：全体とコア別の使用率、棒グラフ
- メモリ：総量・使用量と棒グラフ
- ディスク I/O とファイルシステム
- ネットワーク速度
- GPU 使用率（利用可能な場合は nvidia-smi）
- 稼働時間

サーバー側の単一サンプラーが **1 Hz** で SignalR（`/hubs/performance`）経由で配信し、**60 秒の履歴**を保持します。

## プロセスタブ

- プロセス一覧（名前 / PID / ユーザーでフィルター）
- 低頻度サンプリングとページング
- プロセスの終了（権限不足は明示的に通知）

## クロスプラットフォーム

収集は `ISystemMetricsProvider` で抽象化されています。Windows は `GetSystemTimes` と `GlobalMemoryStatusEx`、Linux は `/proc/stat`、`/proc/meminfo`、`/proc/[pid]/status` を読み取ります。

> ホストやサービス ID が対応しない能力は、**明示的に縮退**し、偽の値を表示しません。

## 関連ドキュメント

- [プロトコルと通信](/docs/ja-JP/latest/concepts/protocol)
- [セキュリティモデル](/docs/ja-JP/latest/concepts/security)
