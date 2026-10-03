---
title: Application Model
description: How applications are declared, launched and wired into window and lifecycle management.
category: Concepts
order: 60
---

# Application Model

> This page describes current source capabilities, not the feature inventory of a particular package. See [release notes](/releases/0.1.2) for published artifacts and dates; linked implementation records track verification.

Applications in RelaxKonOS are **not ordinary executables**. The runtime assembles them and wires them into window management and lifecycle handling.

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

## Related documentation

- [Window manager](/docs/en-US/latest/concepts/window-manager)
- [Protocol and communication](/docs/en-US/latest/concepts/protocol)
