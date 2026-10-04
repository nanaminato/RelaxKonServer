---
title: 初めてサーバーに接続する
description: 接続方法を選び、RelaxKonOS のデスクトップを開きます。
category: はじめに
order: 11
---

# 初めてサーバーに接続する

デスクトップクライアントと RelaxKonOS Server ワークスペースを例に説明します。利用できる機能はクライアントとサーバーのバージョン、インストールモード、アカウント権限によって異なります。

## 1. クライアントとサーバーを準備する

[ダウンロード](/downloads)から端末に合ったクライアントを取得し、サーバーのアドレスとアカウントを用意します。未導入の場合は[インストール](/docs/ja-JP/latest/getting-started/installation)または[Linux ユーザーモード](/docs/ja-JP/latest/getting-started/user-mode)を参照してください。

![スクリーンショット：クライアントとサーバーを準備する](/assets/tutorials/ja-JP/tutorial-connect-1.svg)

*スクリーンショット：クライアントとサーバーを準備する*

## 2. 接続方法を選ぶ

クライアントで RelaxKonOS Server を選ぶと完全なワークスペースが開きます。単独の SSH 接続ではターミナル、SFTP、サーバー管理向けの簡易デスクトップが開きます。目的に応じて選択してください。

![スクリーンショット：接続方法を選ぶ](/assets/tutorials/ja-JP/tutorial-connect-2.svg)

*スクリーンショット：接続方法を選ぶ*

## 3. 接続情報を入力する

Server のアドレスと認証情報を入力します。Linux ユーザーモードは既定でループバックに待ち受けます。Server ログイン画面で SSH トンネル接続を選び、SSH ホスト、ポート、アカウント、サーバー上のアドレス（例：http://127.0.0.1:5000）を入力します。初回 SSH 接続ではホスト鍵の指紋を確認してください。

![スクリーンショット：接続情報を入力する](/assets/tutorials/ja-JP/tutorial-connect-3.svg)

*スクリーンショット：接続情報を入力する*

## 4. ログインして確認する

デスクトップとスタートメニューが表示されることを確認します。SSH と Server の認証情報は別々で、明示的に選択したパスワード認証の場合に共用できます。失敗した場合はアドレス、ポート、アカウント、サーバー状態を確認し、[ログインとセキュリティ](/docs/ja-JP/latest/getting-started/login)を参照してください。

![スクリーンショット：ログインして確認する](/assets/tutorials/ja-JP/tutorial-connect-4.svg)

*スクリーンショット：ログインして確認する*

[次のチュートリアル：デスクトップとウィンドウを使う](/docs/ja-JP/latest/getting-started/tutorial-desktop)
