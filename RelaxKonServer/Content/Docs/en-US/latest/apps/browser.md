---
title: Browser
description: Render pages with the client's native engine and persist bookmarks and history to the workspace.
category: Applications
order: 16
---

# Browser

The built-in browser (RemoteBrowser) is **not a remote browser**. Pages render through the client's own network and native engine.

## Overview

It is built on `NativeWebView` from `Avalonia.Controls.WebView`: WebView2 on Windows, WKWebView on macOS and WebKitGTK on Linux. Web traffic never passes through the server.

## Features

- Navigation: back, forward, refresh, stop, home and address bar
- Bookmarks: add, remove, double-click to navigate, clear all
- History: recorded automatically, double-click to navigate, delete one, clear all
- Browser preferences synchronised to the workspace (home page, where links open)

## How to use it

Browse normally. To reach a loopback address on the server, create a local tunnel with **Port Forwarding** first and open the resulting localhost link.

## What is synchronised

| Content | Synchronised |
| --- | --- |
| Bookmarks, history | Yes (isolated per user) |
| Home page, link target | Yes |
| Cookies, extension config | No |
| Page content | No, it uses the client network |

## Security

- Bookmarks and history persist through `/api/v1.0/browser/*`, isolated per user
- Local tunnels bind to `127.0.0.1` only
- Signed-out users are prompted to sign in

## Related documentation

- [Port forwarding](/docs/en-US/latest/apps/port-forwarding)
- [Workspace](/docs/en-US/latest/concepts/workspace)
