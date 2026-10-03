---
title: RelaxKonOS へようこそ
description: RelaxKonOS とは何か、どのような課題を解決し、リモートデスクトップ製品とどう違うのか。
category: はじめに
order: 1
---

# RelaxKonOS へようこそ

> このページは現在のソース機能を説明し、特定パッケージの収録機能一覧ではありません。公開成果物と日付は[リリースノート](/releases/0.1.2)、検証状況はリンク先の実装記録で確認してください。

RelaxKonOS は**クロスプラットフォームのクラウドネイティブなデスクトップ OS 環境**です。UI は目の前のデバイスに、ワークスペースはサーバーに置き、同じワークスペースを複数のデバイスで継続して利用できます。

リモートデスクトップ製品と異なり、RelaxKonOS が転送するのは**状態と操作の意図**であり、デスクトップのピクセルではありません。

## 中核となるモデル

| レイヤー | 場所 | 役割 |
| --- | --- | --- |
| Client | 手元のデバイス | デスクトップシェル、ウィンドウ管理、アプリ UI、入力、ローカル描画 |
| Protocol | その間 | 明示的な REST エンドポイントと SignalR ハブ契約 |
| Server | Ubuntu / Windows Server | ID、ワークスペース、ストレージ、同期、リモートランタイム、リモートサービス |

> サーバーは**デスクトップ画像をキャプチャも生成もしません**。ワークスペースを永続させる状態とサービスを管理します。

## RelaxKonOS ではないもの

- RDP・VNC・画面ストリーミングではない
- ブラウザー上の Web 管理ダッシュボードではない
- 仮想マシンの画面をクライアントに投影するものではない

## 次のステップ

- [インストール](/docs/ja-JP/latest/getting-started/installation)
- [ユーザーモードでのインストール（Linux、sudo なし）](/docs/ja-JP/latest/getting-started/user-mode)
- [クイックスタート](/docs/ja-JP/latest/getting-started/quick-start)
- [クライアント / サーバー アーキテクチャ](/docs/ja-JP/latest/concepts/architecture)
- ソースリポジトリと Issue: [nanaminato/RelaxKonOS](https://github.com/nanaminato/RelaxKonOS)

## クライアントとアカウントのガイド

- [Android スマートフォンとタブレット](/docs/ja-JP/latest/getting-started/android)
- [システムアカウント・Alias・資格情報](/docs/ja-JP/latest/getting-started/login)
