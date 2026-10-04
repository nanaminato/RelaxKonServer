---
title: Android スマートフォンとタブレット
description: Android スマートフォンとタブレット
category: はじめに
order: 5
---

# Android スマートフォンとタブレット

> このページは現在のソース機能を説明し、特定パッケージの収録機能一覧ではありません。公開成果物と日付は[リリースノート](/releases/0.1.2)、検証状況はリンク先の実装記録で確認してください。

Android はスマートフォンとタブレット向けの独立した Kotlin・Jetpack Compose・Material 3 アプリです。Avalonia や .NET Android は使用しません。最低対応バージョンは Android 10（API 29） ですが、すべての OS・端末で全機能の検証が完了したことを意味しません。

## 接続と機能

- RelaxKonOS Server 接続では、認証したアカウントのワークスペース、ファイル、端末、監視、機能と権限に応じたサービス管理を利用します。
- SSH 接続はホスト鍵確認、SSH 端末、SFTP、サーバー保守を提供します。完全な RelaxKonOS ワークスペースは作成しません。
- スマートフォンとタブレットで適応型ナビゲーションを使用します。Shell の Compact・Medium・Expanded 対応は、全画面のタブレット分割表示の検証完了を意味しません。
- 分割アップロードと再開、Docker/Compose、Nginx/サイト、証明書、Mihomo、FRP、SMB、UFW、Git、デプロイ、タスク管理、Guardian の画面を接続済みです。操作は環境・サーバーモード・認可に従います。
- モバイルアプリモデルはデスクトップ .roapp と独立しており、デスクトップ拡張を Android で直接実行できません。

## インストールと最初の接続

1. [ダウンロード](/downloads)で公開済み署名付き Android APK を探し、SHA-256 を確認します。項目がなければ、このサイトではまだパッケージを提供していません。
2. Android の画面でインストールを確認します。更新は同じ署名証明書を使用します。AAB はストア配布用で、直接インストールには使いません。
3. 端末から到達できるサーバーアドレスを入力するか、SSH ホストを追加して指紋を確認します。実機の localhost では開発 PC に接続できません。
4. システムアカウントまたは利用可能な Alias で認証します。Windows 10/11 個人モードでは本機認証済み端末のコードでペアリングし、端末鍵でログインできます。[Windows 10/11](/docs/ja-JP/latest/getting-started/windows)を参照してください。Linux ユーザーモードは SSH 転送と loopback の範囲を守ります。

## 実装と検証

初回導入と保守の実行経路は接続済みですが、端末と実ホストの完全な受け入れ検証は別途追跡します。信頼できるバックグラウンド通知、ボリューム/DB 復元、Android の復元送信 UI は提供済みとして紹介できません。詳細なモバイル仕様と進捗は Android プロジェクトが管理し、サイトは入門要約を提供します。

正本：[Android 文書](https://github.com/nanaminato/RelaxKonOS/blob/master/Client/RelaxKonOS.Client.Android/docs/README.md)、[実装状況](https://github.com/nanaminato/RelaxKonOS/blob/master/Client/RelaxKonOS.Client.Android/docs/status/Progress.md)、[検証](https://github.com/nanaminato/RelaxKonOS/blob/master/Client/RelaxKonOS.Client.Android/docs/status/Verification.md)、[署名と公開](https://github.com/nanaminato/RelaxKonOS/blob/master/Client/RelaxKonOS.Client.Android/docs/development/android-release.md)。
