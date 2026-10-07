---
title: ログインとアカウント安全
description: ログインとアカウント安全
category: はじめに
order: 6
---

# ログインとアカウント安全

> 現在のソース機能を説明します。公開パッケージの内容は[リリースノート](/releases/0.1.2)、検証状況は関連する実装文書を参照してください。

## Windows 10/11 の個人用 PC

Windows 10/11 個人モードの所有者は、同じ PC のループバック Negotiate で**この Windows 端末を設定**を実行できます。アカウントパスワードや Hello PIN は不要で、他の Windows 管理者は所有者を代替できません。以後は端末鍵でログインします。

10 分間有効な 1 回限りのコードでデスクトップや Android を同じ所有者アカウントとワークスペースにペアリングできます。失効すると対象端末のトークンと Hub 接続が無効になります。Windows 10/11 システムモードでは本機管理者が端末鍵を設定・復旧できますが、Windows Server はこのワークステーション向け入口を提供しません。

詳しくは[Windows 10/11 の個人用 PC](/docs/ja-JP/latest/getting-started/windows)を参照してください。

## システム認証と Alias

システム認証は Windows LogonUser または Linux PAM が検証します。システムモードでは、実際のシステムアカウントで認証した後、設定で独立した Alias とパスワードを作成できます。両方の資格情報は同じホスト ID・User・Workspace に結び付き、Alias は新しい OS ユーザーや追加のファイル権限を作りません。

Alias パスワードは一方向ハッシュだけを保存します。サーバーはシステム認証パスワードを永続保存しません。資格情報を記憶する選択をした場合、デスクトップは OS 安全ストレージ、Android は端末内の保管庫を使用します。これらはワークスペース同期の対象ではありません。

## SSH トンネル経由で Server にログイン

デスクトップと Android の Server ログイン画面は SSH トンネル接続に対応します。SSH ホスト、ポート、アカウントと、パスワードまたは PEM/OpenSSH 秘密鍵・必要なパスフレーズを入力します。Server アドレスには SSH ホスト上のループバックサービス（例：`http://127.0.0.1:5000`）を指定します。初回接続やホスト鍵変更時は指紋を確認してください。

SSH と Server の資格情報は別々に入力・安全保存できます。パスワード認証では同じユーザー名とパスワードを使う設定も明示的に選べます。接続テスト終了時にテスト用トンネルを閉じます。正式接続では端末のループバックポートを自動割り当てし、ログアウトやセッション失効時に閉じます。保存済み設定は再選択でき、秘密の資格情報はワークスペース同期の対象ではありません。

この接続は完全な Server ワークスペースへのログインであり、独立した SSH デスクトップのターミナル/SFTP 接続とは異なります。踏み台、リバースプロキシのサブパス、自動バックグラウンド再接続は現在未対応で、切断後は再ログインが必要です。HTTPS は証明書名、有効期限、信頼チェーンを検証し、証明書はアクセス先の `127.0.0.1` と一致する必要があります。端末のスリープとモバイル回線切り替えは実機検証が必要です。

## 認証方法の管理

アカウント設定で Alias の確認・作成・改名・パスワード変更・削除を行い、重要な変更では現在の資格情報を再検証します。システムアカウントの直接ログインを無効にするには有効な Alias が必要です。変更されるのは RelaxKonOS のログイン方針だけで、SSH・SMB・システムアカウントには影響しません。Alias の削除にはシステムパスワードの再確認が必要で、システム認証が復旧します。パスワードやセキュリティ方針の変更で古いセッションが失効する場合は再認証します。

## モードと認可

Linux ユーザーモードは Server を実行する Unix アカウントだけを受け付け、Alias と管理者認可を提供しません。システムモードでも、Alias はシステムパスワード認証による管理者の再入力免除を継承しません。一般ユーザーと Alias は選択した管理者を明示的に認証します。別アカウントの Guardian・スクリプト操作は今回の明示的承認が必要で、ファイル操作は別のパス認可に従います。

参考：[認証実装](https://github.com/nanaminato/RelaxKonOS/blob/master/RelaxKonOS.Server/Identity/LoginAuthenticationService.cs)、[Alias 管理](https://github.com/nanaminato/RelaxKonOS/blob/master/RelaxKonOS.Server/Identity/AliasCredentialService.cs)、[特権操作](https://github.com/nanaminato/RelaxKonOS/blob/master/docs/platform/RelaxKonOS.PrivilegedOperations.Operations.md)。

[ユーザーモード](/docs/ja-JP/latest/getting-started/user-mode) · [セキュリティモデル](/docs/ja-JP/latest/concepts/security)
