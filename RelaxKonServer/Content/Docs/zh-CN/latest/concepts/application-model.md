---
title: 应用模型
description: 应用如何声明、启动并接入窗口与生命周期管理。
category: 概念
order: 60
---

# 应用模型

> 本页对照当前源码说明能力，不是某个发布包的功能清单。已发布产物与日期见[发行说明](/releases/0.1.2)，功能验收状态以所链接的实现记录为准。

RelaxKonOS 中的应用**不是普通可执行文件**。它们由运行时（Runtime）装配，并接入统一的窗口管理与生命周期。

## 应用的结构

```text
Application Package
├── Manifest          # 标识、名称、图标、实例策略、扩展名声明
├── UI                # 视图
├── Logic             # 逻辑
├── State Manager     # 状态
└── Remote Connector  # 与服务端的连接
```

## 接入方式

内置应用可继承 RemoteApplicationBase 并实现 Manifest/Activate(AppContext)。桌面 .roapp 则必须实现 IExternalRemoteApplication，通过 ActivateAsync(IExternalAppContext) 接收受限能力，不获得宿主 IServiceProvider。下面是当前扩展入口的最小示例；需引用匹配当前版本的 SDK 与 Avalonia。Android 不执行桌面程序集。

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

## 启动流程

```text
桌面图标 / 开始菜单
      |
ApplicationManager.Launch
      |
创建 AppContext
      |
Built-in Activate / Package ActivateAsync
      |
WindowManager.Create
      |
RemoteWindow（Avalonia 控件）
```

## 实例策略

`ApplicationManifest.InstancePolicy` 决定应用是单窗口还是多窗口。设置、任务管理器、端口转发、防火墙、进程守护、Docker 为单窗口；记事本与代码编辑器支持多窗口。

## 应用能力

- 窗口 API：`ShowWindow`
- 模态对话框：`ShowDialogAsync<TResult>`（可嵌套、任意结果类型）
- 能力与私有配置：`/api/v1.0/capabilities` 与 App Settings
- 应用启动 URI：受控的 `relaxkonos://` 路由

## 相关文档

- [窗口管理器](/docs/zh-CN/latest/concepts/window-manager)
- [远程服务](/docs/zh-CN/latest/concepts/protocol)
