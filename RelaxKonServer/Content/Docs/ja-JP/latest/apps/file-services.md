---
title: ファイルサービス
description: SMB など、ホストのファイル共有サービスを管理された形で提供します。
category: アプリケーション
order: 36
---

# ファイルサービス

ファイルサービスは、ホストのファイル共有の導入と運用を管理します。最初の対応プロトコルは SMB です。

## 目標

- 管理されたタスクを通じてサービスの開始、設定、復旧を行う
- Linux では Samba、Windows では SMB Server を使用する
- マシンごとの設定ではなく、ワークスペースの設定と一貫させる

## ステータス

最初の SMB 実装は設計・提供の途上にあり、長期的な仕様では検出、ライフサイクル、復旧を定義しています。

## 関連ドキュメント

- [ファイルマネージャー](/docs/ja-JP/latest/apps/file-manager)
- [永続化](/docs/ja-JP/latest/concepts/persistence)
