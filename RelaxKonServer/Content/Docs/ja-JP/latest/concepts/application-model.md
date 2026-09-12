---
title: アプリケーションモデル
description: アプリケーションがどのように宣言され、起動され、ウィンドウとライフサイクル管理に接続されるかを説明します。
category: 概念
order: 60
---

# アプリケーションモデル

RelaxKonOS のアプリケーションは**単なる実行ファイルではありません**。ランタイムがそれらを組み立て、ウィンドウ管理とライフサイクル処理へ接続します。

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

```csharp
public class MyApp : RemoteApplicationBase
{
    public override string Id => "com.example.myapp";
    public override string DisplayName => "My Application";

    public override void Activate(AppContext context)
    {
        context.ShowWindow("My Window", contentFactory: () => new MyView());
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
IRemoteApplication.Activate
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
