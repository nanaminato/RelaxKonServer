---
title: ウィンドウマネージャー
description: ウィンドウのライフサイクル、重ね順、モーダルダイアログ、キーボードルーティング。
category: 概念
order: 62
---

# ウィンドウマネージャー

ウィンドウマネージャーは OS レベルのウィンドウシステムを模擬し、デスクトップ体験の基盤となります。

## 構造

```text
WindowManager
     |
RemoteWindow
     |
Avalonia Control
```

## 責務

- ウィンドウの作成とクローズ
- 移動と 8 方向リサイズ
- フォーカスと重ね順
- 最小化、最大化、フルスクリーン
- タスクバー状態の同期
- オーナー単位でマスクするモーダルダイアログ

## モーダルダイアログ

`AppContext.ShowDialogAsync<TResult>(owner, title, contentFactory)` は、任意の戻り値型を返し、オーナーウィンドウのみをマスクする、再利用可能でネスト可能なモーダルの仕組みを提供します。

## ホストウィンドウ制御

デスクトップシェルはホストレベルのウィンドウ制御を実装しています。タイトルバーのドラッグ、8 方向リサイズ、最小化／最大化／クローズ、フルスクリーン、そして mstsc 風の接続バー（フルスクリーン切り替え、ピン留めと自動非表示、接続情報、接続の切断＝サインアウト）です。

## 関連ドキュメント

- [アプリケーションモデル](/docs/ja-JP/latest/concepts/application-model)
- [RelaxKonOS の概要](/docs/ja-JP/latest/concepts/overview)
