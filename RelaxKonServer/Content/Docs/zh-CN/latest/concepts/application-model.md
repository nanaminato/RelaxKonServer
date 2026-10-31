---
title: 应用模型
description: 应用如何声明、启动并接入窗口与生命周期管理。
category: 概念
order: 60
---

# 应用模型

> 本页说明当前源码能力；发布包内容见[发行说明](/releases/0.1.2)，验收状态见相关实现文档。

RelaxKonOS 应用由运行时（Runtime）加载，统一管理窗口和生命周期。

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

## 组件依赖与启动门禁

部分内置应用需要宿主组件才能工作：Web 服务器需要 Nginx，Docker 管理器需要容器引擎，隧道需要 frpc 或 frps，代理需要对应的运行时核心，文件服务需要 Samba，Git 相关工作需要 Git。

- 依赖由**组件目录**声明，不由应用自己拼组件 ID：应用只声明关联哪些组件，启动时统一读取组件状态。
- 同一应用关联多个组件时，这些组件是**可替代**关系；隧道管理器的 frpc 与 frps 任一已安装即可打开。
- 未安装、待恢复或正在执行时，应用在进入前提示并提供前往组件中心的入口；状态未知或没有读取权限时既不当成未安装，也不诱导安装。
- 宿主声明该平台不提供某组件时，应用被视为「无需组件」并允许打开，平台差异不被误判为缺件。
- 安装、更新与卸载只在[组件中心](/docs/zh-CN/latest/apps/component-center)发生；应用内只保留业务配置与服务操作。

## 相关文档

- [窗口管理器](/docs/zh-CN/latest/concepts/window-manager)
- [远程服务](/docs/zh-CN/latest/concepts/protocol)
