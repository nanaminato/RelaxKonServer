---
title: App Installer
description: Install and update .roapp application packages after the user reviews them.
category: Applications
order: 38
---

# App Installer

App Installer installs and updates RelaxKonOS application packages. It brings external applications into the same runtime, window management and permission model as built-in applications while **keeping the user's review step**.

## Overview

An application package is an archive holding a manifest and assemblies, and optionally its own language resources and icons. The installer safely unpacks it, installs it into a versioned directory and registers it in the shared package catalogue, after which the shell discovers the application.

## When to use it

- Distributing an application you built: pack it into an application package with the developer CLI and hand it to the installer.
- Installing an external desktop shell to change the default desktop presentation (it runs only inside the RelaxKonOS main window and **does not and cannot replace the host operating system's desktop**).
- Opening an application package straight from File Manager and letting the installer handle it.

## Current capabilities

- Install from a package chosen on this machine, or pick one from the server filesystem (it is staged locally first so you can review it).
- Install and update: a new version of the same application installs into a versioned directory rather than overwriting in place.
- File Manager integration: double-clicking a package routes it to the installer as "open with".
- Packages carry their own localisation files and follow the workspace language.
- The package catalogue is the only source of application discovery, and the installer maintains it.

## Permissions and security

- **Packages in `v2` are neither signed nor sandboxed.** The installer only unpacks safely and registers; it **does not** verify where a package came from or limit what it may do. Install only packages you trust or built yourself.
- The manifest declares the permission model version and the package type; a missing or incompatible package is never activated.
- Archive extraction is bounded and rejects out-of-scope paths and other unsafe entries.
- Applications request operations through the context the shell provides instead of reaching for the service container, the authentication session or the remote file API directly.

## Platform differences

The installer and the package format are identical on Windows and Linux; the differences lie in how an application itself uses host capabilities.

## Known limitations

- **Installed does not mean trusted**: with no signing and no sandbox, installing a package is equivalent to trusting its author. The UI does not present "installed" as "reviewed".
- A desktop shell package is loaded only after the user explicitly selects it on the personalisation page; when it is incompatible or fails to initialise, the current desktop stays usable and falls back to the default one, without overwriting the cross-device selection intent in the workspace.
- A missing, incompatible, disabled or timing-out package never makes the desktop unusable.

## Related documentation

- [Code Editor](/docs/en-US/latest/apps/code-editor)
- [Settings](/docs/en-US/latest/apps/settings)
- [Application model](/docs/en-US/latest/concepts/application-model)
