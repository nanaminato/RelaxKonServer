---
title: RelaxKonOS へようこそ
description: RelaxKonOS とは何か、どのような課題を解決し、リモートデスクトップ製品とどう違うのか。
category: はじめに
order: 1
---

# RelaxKonOS へようこそ

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
- [クイックスタート](/docs/ja-JP/latest/getting-started/quick-start)
- [クライアント / サーバー アーキテクチャ](/docs/ja-JP/latest/concepts/architecture)
