---
title: 窗口管理器
description: 窗口生命周期、Z-Order、模态对话框与键盘路由。
category: 概念
order: 62
---

# 窗口管理器

窗口管理器（WindowManager）模拟操作系统级窗口系统，是桌面体验的基础。

## 结构

```text
WindowManager
     |
RemoteWindow
     |
Avalonia Control
```

## 职责

- 创建与关闭窗口
- 移动与 8 向缩放
- 焦点与 Z-Order
- 最小化 / 最大化 / 全屏
- 任务栏状态同步
- 模态对话框与 owner 局部遮罩

## 模态对话框

`AppContext.ShowDialogAsync<TResult>(owner, title, contentFactory)` 提供可复用、可嵌套的模态机制，返回任意结果类型，并在 owner 上显示局部遮罩，不影响其他窗口。

## 宿主窗口控制

桌面外壳实现了宿主窗口级控制：标题栏拖动、8 向缩放、最小化 / 最大化 / 关闭、全屏，以及 mstsc 风格的连接栏（全屏切换、固定与自动隐藏、连接信息、关闭连接＝登出）。

## 相关文档

- [应用模型](/docs/zh-CN/latest/concepts/application-model)
- [桌面体验](/docs/zh-CN/latest/concepts/overview)
