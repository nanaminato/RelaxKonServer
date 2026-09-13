# RelaxKon 公式サイト API

[中文](./README.md) · [English](./README.en.md)

`RelaxKonServer/RelaxKonServer` は RelaxKon 公式サイト向けの純粋な **ASP.NET Core 10** REST API です。Razor ページ、MVC ビュー、静的サイトファイル、Angular クライアントのいずれもホストしません。ドキュメントとサイトコンテンツの唯一の情報源であり、管理された API エンドポイントから RelaxKonOS リリースも配信するため、別のダウンロード プロジェクトは不要です。

## ローカルでの実行

- **.NET 10 SDK** をインストールします。
- このディレクトリで `dotnet restore`、続いて `dotnet watch run` を実行します。
- 起動プロファイルは `RelaxKonServer/Properties/launchSettings.json` にあります。`https` プロファイルは `https://localhost:7252` と `http://localhost:5062` の両方を、`http` プロファイルは `5062` のみを待ち受けます。
- 初回は `dotnet dev-certs https --trust` で開発用証明書を信頼させます（Windows/macOS）。Linux ではブラウザーの信頼ストアへ証明書をインポートしてください。

OpenAPI 記述は `/swagger/v1/swagger.json`、Swagger UI の入口は `/swagger`、死活監視プローブは `/api/health` で提供されます。

> **既知の制限**：`/swagger` は HTML ページを返しますが、そのページにもセキュリティミドルウェアが設定する全体 CSP（`default-src 'none'`）が付きます。そのためブラウザーは unpkg 上の Swagger UI 資産を読み込めず、そのままでは動作しません。コマンドラインからの `swagger.json` 取得には影響しません。UI を復旧するには、このパスだけ CSP を緩める必要があります。

## コンテンツレイアウト

サイトコンテンツはすべてサーバーが所有し、`Content/` の下に置かれます。

```text
Content/
├── Docs/{language}/{version}/…      # Markdown ドキュメントツリー
├── Releases/{version}.md            # Markdown リリースノート（ファイル名がバージョン）
├── Faq/{language}.json              # 言語ごとの FAQ
├── Downloads/downloads.json         # ダウンロード記述ファイル
└── ReleaseDelivery/…                # 配置済み RelaxKonOS ZIP、チェックサム、記述、ブートストラップ
```

### ドキュメント

Markdown ファイルは任意で YAML 風のフロントマターから始められます。

```markdown
---
title: Terminal
description: Use a persistent remote terminal session.
category: Applications
order: 14
---

# Terminal
```

- **ドキュメントの追加**：バージョンディレクトリ配下の任意の場所に `.md` を作成します。slug は `.md` を除いたパスです（`apps/terminal`）。
- **ナビゲーションのグループ化**：`category` がサイドバーのグループになります。グループとドキュメントは `order`、次いでタイトル順に並びます。
- **言語の追加**：`Content/Docs/<code>/latest` を作成します。言語は `en-US`、`zh-CN`、`ja-JP` の固定順で返り、`zh-CN` と `ja-JP` の表示名は組み込み（简体中文 / 日本語）、その他のコードはコード自体が使われます。
- **バージョンの追加**：`latest` の隣に別のディレクトリを作成します。`latest` が先頭になります。
- **翻訳フォールバック**：要求された言語に slug が無い場合は `en-US` の版が返り、レスポンスの `isFallback` が `true` になります。フォールバック項目のカテゴリ名は要求言語のままに保たれ、ナビゲーションのグループ見出しが揃います。
- **現状**：`en-US`、`zh-CN`、`ja-JP` はいずれも 26 件で完全に揃っています。**ファイルを増減するときは 3 言語の件数を揃えてください。** 揃っていないとナビゲーションにフォールバック項目が現れます。
- 言語とバージョンのセグメントは `[A-Za-z0-9-]` のみ、slug はさらに `/` と `_` を許容し最大 256 文字です。それ以外は 404 を返します。

### リリースノート、FAQ、ダウンロード

- リリースノートはバージョン名の Markdown ファイルです。フロントマターは `version`、`title`、`date`、`summary`、`prerelease` に対応します。省略時はファイル名がバージョン、ファイルの最終更新時刻が日付になります。一覧はリリース日の降順で、詳細レスポンスの `highlights` は本文の最初の 6 個のリスト項目です。
- `Faq/<language>.json` は `{ question, answer, category, order }` の配列です。該当言語のファイルが無い、または言語コードが不正な場合は `en-US` へフォールバックし、`category` が空なら `General` になります。
- `Downloads/downloads.json` は `{ platform, architecture, version, url, size, checksum, releaseDate, isAvailable, fileName }` の配列です。

### オプション

厳密に型付けされたオプションは構成からバインドされ、コントローラーが生の構成文字列を読むことはありません。

```json
{
  "Documentation": { "RootPath": "Content/Docs", "CacheMinutes": 30 },
  "Content": {
    "RootPath": "Content",
    "ReleasesPath": "Releases",
    "FaqPath": "Faq",
    "DownloadsPath": "Downloads/downloads.json",
    "CacheMinutes": 30
  }
}
```

`appsettings.Development.json` はキャッシュ TTL を 2 分に短縮して開発中の内容変更を素早く反映させ、開発用 CORS の許可オリジンも提供します。構成された TTL はコード側で 1〜120 分にクランプされます。

## エンドポイント

| メソッド | ルート | 用途 |
| --- | --- | --- |
| GET | `/api/health` | 死活監視プローブ |
| GET | `/api/docs/languages` | ドキュメントの言語一覧 |
| GET | `/api/docs/versions?language=` | ドキュメントのバージョン一覧（`language` 省略時は全言語を統合） |
| GET | `/api/docs/{language}/{version}/navigation` | `category` でグループ化したサイドナビゲーションツリー |
| GET | `/api/docs/{language}/{version}/{slug}` | 本文と `headings`、`previous`/`next`、`isFallback` |
| GET | `/api/docs/search?q=&language=&version=` | タイトル、説明、本文の検索（`language` の既定は `en-US`、`version` は `latest`） |
| GET | `/api/downloads` | ダウンロード記述 |
| GET | `/api/releases` | リリースノートの要約一覧 |
| GET | `/api/releases/{version}` | Markdown 本文を含むリリースノート詳細 |
| GET | `/api/faq?language=` | 指定言語の FAQ 項目（既定は `en-US`） |
| GET/HEAD | `/relaxkonos/{artifact}` | RelaxKonOS ZIP、チェックサム、記述、ブートストラップ。HTTP Range 対応 |

検索の詳細：`q` が 2 文字未満の場合は 400 を返します。本文が一致した場合の `snippet` は Markdown 記法を除去したプレーンテキストで、結果は最大 20 件、タイトル一致が先頭に並びます。

## RelaxKonOS の配布とワンコマンド インストール

`Content/ReleaseDelivery/` は既存 API が配信する配置データであり、新しい Web プロジェクトではありません。バージョン付き ZIP は 1 年 immutable キャッシュ、`latest` の記述とインストーラーは `no-cache`、GET、HEAD、Range 再開に対応します。リリース担当者は先に次を実行します。

```powershell
./deployment/Publish-RelaxKonOSRelease.ps1 `
  -SourceDirectory 'D:\artifacts\relaxkonos' `
  -BootstrapDirectory '..\RelaxKonOS\deployment\bootstrap'
```

ZIP の SHA-256 を検証し、`stable/{version}/{runtime}/` に置き、`latest/{runtime}.json` を作成します。`-PublicBaseUri https://relaxkon.com` でメインサイトを正規 URL にでき、既定値は `https://downloads.relaxkon.com` です。両方の名前は一つのデプロイを提供します。

`deployment/nginx/relaxkon.com.conf` を配置し、`relaxkon.com`、`www.relaxkon.com`、`downloads.relaxkon.com` を同じサーバーへ向け、全名前を含む証明書を設定します。`/api/` と `/relaxkonos/` はこの API にプロキシされ、その他は既存 Angular ビルドが処理します。

利用者はどちらのドメインも使用でき、インストーラーは安定版記述を取得して ZIP SHA-256 を検証します。

```bash
curl -fsSL https://downloads.relaxkon.com/relaxkonos/stable/latest/bootstrap/install-relaxkonos.sh | sudo bash
curl -fsSL https://relaxkon.com/relaxkonos/stable/latest/bootstrap/install-relaxkonos.sh | sudo bash -s -- --non-interactive
```

```powershell
irm https://downloads.relaxkon.com/relaxkonos/stable/latest/install.ps1 | iex
& ([scriptblock]::Create((irm 'https://relaxkon.com/relaxkonos/stable/latest/install.ps1'))) -InstallerArguments '-NonInteractive'
```

`install.ps1` は実際の Windows インストーラーを先にディスクへ保存するため、UAC 昇格時にも安全に再起動できます。オフライン ZIP と、リリース URI + SHA-256 の明示指定も引き続き利用可能です。

## キャッシュとセキュリティ

- `IMemoryCache` が言語、バージョン、ナビゲーション索引、ドキュメント、リリースノート、FAQ、ダウンロードをキャッシュします。開発は短い TTL、本番は長い TTL を使います。キャッシュ無効化は後から重ねて実装できます。
- コンテンツサービスは論理セグメントのみを受け取り、管理下のコンテンツルートに対してパスを正規化し、ルート外へ出るものはすべて拒否するため、ディレクトリトラバーサルは成立しません。
- 言語、バージョン、slug、検索入力は検証され、長さも制限されます。
- すべてのレスポンスに `X-Content-Type-Options`、`X-Frame-Options`、`Referrer-Policy`、`X-Permitted-Cross-Domain-Policies`、`Cross-Origin-Resource-Policy`、`Content-Security-Policy`、`Permissions-Policy` が付きます。HTTPS リダイレクトと（開発以外の）HSTS が有効で、レスポンス圧縮も有効です。
- 未処理の例外はサーバー側でログに記録し、外部へは汎用の ProblemDetails として返します。スタックトレースや絶対パスは公開されません。
- 開発用 CORS のオリジンは `appsettings.Development.json` から取得し、`Program.cs` にハードコードしません。`AllowAnyOrigin` は使いません。

## スモークテスト

リポジトリ直下の `smoke-test.ps1` は `http` プロファイル（`http://localhost:5062`）で API を起動し、ヘルスチェック、ドキュメントのナビゲーション、3 言語の本文、リリースノート、ダウンロード、FAQ を順に要求して結果を `api-smoke.log` に書き出します。エンドポイントやコンテンツ構成を変更した後は実行してください。

## プロジェクト境界

この API は Angular アプリケーションを配信してはいけません。ワークスペース全体の境界制約は [`../WEBSITE_ARCHITECTURE.ja.md`](../WEBSITE_ARCHITECTURE.ja.md) を参照してください。
