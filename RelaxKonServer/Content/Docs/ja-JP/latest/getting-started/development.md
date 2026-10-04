---
title: ソースからの開発とデバッグ
description: .NET と Angular の環境設定、ソースのビルド、NuGet/npm 依存関係の保守。
category: はじめに
order: 7
---

# ソースからの開発とデバッグ

このチュートリアルはソースを変更する開発者向けです。配布パッケージの導入は[インストール](/docs/ja-JP/latest/getting-started/installation)を参照してください。

## 環境と依存バージョン

| プロジェクト | 環境と現在の依存関係 |
| --- | --- |
| RelaxKonOS デスクトップとサーバー | .NET 10 SDK、Avalonia コア 12.1.3、DataGrid 12.1.2、Microsoft.OpenApi 2.12.2 |
| 公式サイトと Publisher のフロントエンド | Angular 22.2.1、TypeScript 6.0.3、RxJS 7.8.2、Vitest 5.0.3、jsdom 30.1.2 |
| 公式サイトと Publisher の API | .NET 10 SDK |

Node.js は 22.x の 22.22.3 以降、24.x の 24.15.0 以降、または 26 以降を使用します。パッケージマネージャーの基準は npm 11.19.1 です。Angular ビルダーは TypeScript `>=6.0 <6.1` を要求するため、TypeScript 7 へ直接更新しないでください。Angular 関連パッケージは揃え、リポジトリの `package.json` と `package-lock.json` に従います。[Angular 互換性表](https://angular.dev/reference/versions)も参照してください。

## RelaxKonOS の起動

[RelaxKonOS ソース](https://github.com/nanaminato/RelaxKonOS)を取得し、リポジトリのルートで実行します。

```powershell
dotnet restore RelaxKonOS.sln
dotnet build RelaxKonOS.sln -c Release -m:1
```

3 つのターミナルで Guardian、Server、デスクトップクライアントをそれぞれ起動します。

```powershell
$env:RELAXKONOS_GUARDIAN_PIPE = 'relaxkonos-guardian-dev'
$env:RELAXKONOS_GUARDIAN_SHARED_SECRET = 'dev-guardian-secret-local-only'
dotnet run --project RelaxKonOS.Guardian.Agent
dotnet run --project RelaxKonOS.Server --launch-profile http
dotnet run --project Client/RelaxKonOS.Client.Desktop
```

ローカルでは `http://localhost:5090` に接続します。通常のファイル、ターミナル、Git のデバッグには開発設定の `local-identity` を使用し、Server とログインアカウントを同じホストユーザーにします。通常のデバッグにシステムサービスの導入は不要です。保護ファイル、実際のファイアウォール、管理対象ランタイムには特権 Helper の別途設定が必要です。ソースの[開発ガイド](https://github.com/nanaminato/RelaxKonOS/blob/master/docs/development/RelaxKonOS.Develop.md)を参照してください。Android は独立した Kotlin/Compose プロジェクトです。[モバイルガイド](/docs/ja-JP/latest/getting-started/android)に従ってください。

## 公式サイトの起動

[RelaxKon フロントエンド](https://github.com/nanaminato/RelaxKon)と [RelaxKonServer バックエンド](https://github.com/nanaminato/RelaxKonServer)を取得します。バックエンドのリポジトリルートで実行します。

```powershell
dotnet run --project RelaxKonServer --launch-profile https
```

フロントエンドのリポジトリルートで実行します。

```powershell
npm ci
npm start
```

`http://localhost:4200` を開きます。`proxy.conf.json` は既定で `/api` を `https://localhost:7252` に転送します。変更後は開発サーバーを再起動してください。初回のローカル HTTPS 設定には `dotnet dev-certs https --trust` を使い、Linux ではブラウザーの信頼ストアへ証明書を取り込みます。本番ビルドとテストを確認します。

```powershell
npm run build
npm test -- --watch=false
```

公式サイトの文書はバックエンドの `RelaxKonServer/Content/Docs/{language}/latest/` にあります。中国語、英語、日本語の slug を揃え、`order` を一意にします。編集後はバックエンドリポジトリで `node tools/verify-doc-order.mjs` と `node tools/verify-doc-links.mjs` を実行します。

## 依存関係の保守

- NuGet バージョンは RelaxKonOS の `Directory.Packages.props` に集約し、各プロジェクトの参照にはバージョンを記載しません。`dotnet list RelaxKonOS.sln package --outdated` で確認し、互換バージョンへ更新して再ビルドします。Avalonia コアを揃え、DataGrid など独立配布のコンポーネントは各安定版に従います。
- ASP.NET Core OpenAPI 10.0.x は Microsoft.OpenApi 3 未満を要求します。ImageSharp は 3.1.12 を維持し、メジャー更新前に API と `THIRD_PARTY_NOTICES.md` のライセンス条件を確認します。
- npm 依存関係は各フロントエンドの `package.json` で管理します。`npm outdated`、バージョン編集、`npm update` の順で更新し、ロックファイルも同時にコミットします。その後ビルドとテストを実行し、新しいチェックアウトでは `npm ci` を使います。`--force` や `--legacy-peer-deps` で互換性エラーを回避しないでください。
- Publisher の依存バージョンとビルド手順は同じです。ローカル保守者専用で、開発ポートは 4201 です。

## 次のステップ

- [アーキテクチャ](/docs/ja-JP/latest/concepts/architecture)
- [アプリケーションモデル](/docs/ja-JP/latest/concepts/application-model)
