---
title: アプリケーションモデル
description: アプリケーションがどのように宣言され、起動され、ウィンドウとライフサイクル管理に接続されるかを説明します。
category: 概念
order: 60
---

# アプリケーションモデル

> 現在のソース機能を説明します。公開パッケージの内容は[リリースノート](/releases/0.1.2)、検証状況は関連する実装文書を参照してください。

RelaxKonOS のランタイムがアプリを読み込み、ウィンドウとライフサイクルを管理します。

## 構造

```text
Application Package
├── Manifest          # identity, name, icon, instance policy, extension declarations
├── UI                # views
├── Logic             # behaviour
├── State Manager     # state
└── Remote Connector  # connection to the server
```

## 統合方法

内蔵アプリは RemoteApplicationBase を継承し Manifest/Activate(AppContext) を実装できます。デスクトップ .roapp は IExternalRemoteApplication を実装し、ActivateAsync(IExternalAppContext) から限定された機能を受け取り、ホストの IServiceProvider は取得しません。以下の最小例には現在の SDK と Avalonia の参照が必要です。Android はデスクトップアセンブリを実行しません。

```csharp
using Avalonia.Controls;
using RelaxKonOS.AppSDK;
using RelaxKonOS.Core.Applications;
using System.Threading;
using System.Threading.Tasks;

public sealed class MyApp : IExternalRemoteApplication
{
    public ApplicationManifest Manifest { get; } = new(
        new AppId("com.example.myapp"), "My Application");

    public Task ActivateAsync(IExternalAppContext context,
        CancellationToken cancellationToken = default)
    {
        context.Windows.ShowWindow("My Window",
            new TextBlock { Text = "Hello, RelaxKonOS!" });
        return Task.CompletedTask;
    }
}
```

## 起動フロー

```text
Desktop icon / start menu
      |
ApplicationManager.Launch
      |
Create AppContext
      |
Built-in Activate / Package ActivateAsync
      |
WindowManager.Create
      |
RemoteWindow (Avalonia control)
```

## インスタンスポリシー

`ApplicationManifest.InstancePolicy` は、そのアプリケーションが単一ウィンドウか複数ウィンドウかを決定します。設定、タスクマネージャー、ポートフォワーディング、ファイアウォール、プロセスガーディアン、Docker は単一ウィンドウです。メモ帳とコードエディターは複数ウィンドウを許可します。

## 機能

- ウィンドウ API：`ShowWindow`
- モーダルダイアログ：`ShowDialogAsync<TResult>`（ネスト可能、任意の戻り値型）
- ケーパビリティとプライベート設定：`/api/v1.0/capabilities` と App Settings
- 検証済みの `relaxkonos://` ルートによる起動 URI

## 関連ドキュメント

- [ウィンドウマネージャー](/docs/ja-JP/latest/concepts/window-manager)
- [プロトコルと通信](/docs/ja-JP/latest/concepts/protocol)
