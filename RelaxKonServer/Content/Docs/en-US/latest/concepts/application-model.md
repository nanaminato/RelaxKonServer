---
title: Application Model
description: How applications are declared, launched and wired into window and lifecycle management.
category: Concepts
order: 60
---

# Application Model

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

## Launch flow

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
