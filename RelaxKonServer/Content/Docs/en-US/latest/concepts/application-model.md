---
title: Application Model
description: How applications are declared, launched and wired into window and lifecycle management.
category: Concepts
order: 60
---

# Application Model

> This page covers current source capabilities. See [release notes](/releases/0.1.2) for package contents and the linked implementation documents for verification status.

The RelaxKonOS runtime loads applications and manages their windows and lifecycle.

## Structure

```text
Application Package
├── Manifest          # identity, name, icon, instance policy, extension declarations
├── UI                # views
├── Logic             # behaviour
├── State Manager     # state
└── Remote Connector  # connection to the server
```

## How to integrate

Built-in apps may inherit RemoteApplicationBase with Manifest/Activate(AppContext). Desktop .roapp packages implement IExternalRemoteApplication and receive scoped capabilities through ActivateAsync(IExternalAppContext), without the host IServiceProvider. This minimal current entry point requires matching SDK and Avalonia references. Android does not execute desktop assemblies.

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

## Launch flow

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

## Instance policy

`ApplicationManifest.InstancePolicy` decides whether an application is single-window or multi-window. Settings, Task Manager, Port Forwarding, Firewall, Process Guardian and Docker are single-window; Notepad and the code editor allow multiple windows.

## Capabilities

- Window API: `ShowWindow`
- Modal dialogs: `ShowDialogAsync<TResult>` (nestable, any result type)
- Capabilities and private settings: `/api/v1.0/capabilities` and App Settings
- Launch URIs through validated `relaxkonos://` routes

## Component dependencies and launch gating

Some built-in applications need a host component: Web Server needs Nginx, Docker Manager needs a container engine, Tunnels needs frpc or frps, Proxy needs its runtime core, File Services needs Samba, and Git work needs Git.

- Dependencies are declared by the **component catalog**, not guessed by each application: an application states which components it relates to, and the launch check reads their state through one route.
- When one application relates to several components, they are **alternatives**; Tunnel Manager opens with either frpc or frps installed.
- When a component is missing, awaiting recovery or busy, the application warns before opening and offers a jump to the Component Center. An unknown state or a missing permission is never treated as "not installed" and never used to push an install.
- If the host declares a component unsupported on that platform, the application treats it as "not required" and opens, so a platform difference is not mistaken for a missing dependency.
- Install, update and uninstall only happen in the [Component Center](/docs/en-US/latest/apps/component-center); applications keep business configuration and service operations.

## Related documentation

- [Window manager](/docs/en-US/latest/concepts/window-manager)
- [Protocol and communication](/docs/en-US/latest/concepts/protocol)
