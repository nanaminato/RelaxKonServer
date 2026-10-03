---
title: イベントとアラート
description: イベントとアラート
category: アプリケーション
order: 53
---

# イベントとアラート

> このページは現在のソース機能を説明し、特定パッケージの収録機能一覧ではありません。公開成果物と日付は[リリースノート](/releases/0.1.2)、検証状況はリンク先の実装記録で確認してください。

イベントとアラートセンターには永続イベント台帳・集約アラート・カーソル照会・基本処理 API があり、デスクトップには最小の読み取り専用概要と一覧があります。デプロイと Compose の終状態通知は初期接続済みで、センター障害で完了したデプロイをロールバックしません。

## 閲覧と処理

- 対象リソース・状態・重要度・回数・最終観測時刻を確認します。アラート一覧だけで全リソースの現在の健全性は証明できません。
- 確認、許可された手動終了、期間付き抑制、抑制解除には個別の API 認可と監査が必要です。閲覧権限は処理権限ではありません。
- リアルタイム Hub は無効化通知を送り、正式な詳細は REST で再取得します。最小デスクトップ UI は全詳細・処理・深いリンクの接続完了を意味しません。

## 未提供の範囲

デプロイ/Compose の永続再送、証明書/Docker/トンネルの全イベント源、Guardian sequence/checkpoint、完全なデスクトップ操作、実障害演習は未完了です。Android の操作監視や前景通知は信頼できる背景アラートを保証しません。現段階では唯一の本番監視・送達保証として使えません。

参考：[実装進捗](https://github.com/nanaminato/RelaxKonOS/blob/master/docs/applications/RelaxKonOS.EventAlertCenter.Progress.md)、[Android 操作と復元](https://github.com/nanaminato/RelaxKonOS/blob/master/Client/RelaxKonOS.Client.Android/docs/features/OperationsRecovery.md)。
