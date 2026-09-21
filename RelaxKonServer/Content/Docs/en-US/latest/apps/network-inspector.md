---
title: Network Inspector
description: A debug window owned by the desktop shell for inspecting RelaxKonOS client-to-server calls.
category: Applications
order: 49
---

# Network Inspector

Network Inspector is a **system debug window owned directly by the RelaxKonOS shell**. At very low resident cost it shows the calls that go from the **RelaxKonOS client to the RelaxKonOS server**. It follows the network panel of a developer tool but does not try to be a general packet capture tool.

It is **not a built-in application** and needs no installation, does not appear in the application list, and does not expose this capability to external applications.

## Overview

The trust boundary is "system window, host collection": the collector is a singleton in the client host that owns the memory limits, sanitization, classification and event subscriptions, and the inspector window only reads the already-sanitized event stream.

Recording is **off by default**. Collection only happens once developer mode is on and the user starts recording; stopping recording, leaving developer mode or signing out immediately clears the in-memory records.

## How to open it

| Entry point | Behaviour |
| --- | --- |
| Settings → Developer → Network Inspector | Shows status and the open action; when developer mode is off it explains why and disables the action |
| `Ctrl+Shift+I` | Handled by the main window's tunnel routing, independent of which child window has focus |

## What it covers

| Item | Included |
| --- | --- |
| RelaxKonOS REST calls (sign-in, settings, files, browser data, task manager, capability tokens and more) | Yes |
| Terminal hub connection, negotiation, call results and connection state | Yes |
| Downloads, uploads, media playback | Summary only, no body |
| Request and response bodies | Optional small preview, strictly sanitized and truncated |
| Website traffic in the web view, arbitrary outbound traffic from third-party components, server-side internal requests | No |
| Hub message frames, terminal input and output, underlying socket payloads | No |

"Every call" means every RelaxKonOS API call inside that controlled communication boundary, not all network traffic on the device.

## Current capabilities

- Filtering by text, status code, type and source.
- Details across overview, request, response and timeline; a media record shows only the result summary and safe headers.
- Retention in a ring buffer of a fixed entry count and estimated payload; whichever limit is reached first evicts the oldest entry. Nothing is written to disk or uploaded.
- Colour is an aid only, and a text status is always shown too, so themes and accessibility still work.

## Permissions and security

Starting a recording requires all of: developer mode on, the calling package installed, a locally approved permission, and an authenticated session. When any precondition stops holding, the collector stops, clears and notifies subscribers, so an already-open external window cannot keep reading old sensitive records after the permission is withdrawn.

- Authorization headers, cookies, headers and fields with sensitive names, and the matching query parameters in a URL are all replaced with a redacted placeholder.
- Media, binary and streaming responses are **never read or buffered**; only the method, sanitized path, success or failure, status code, duration and content type are recorded.
- Cancellations and network failures also become an event, keeping only the exception category and a bounded safe description rather than the raw exception text.

## Platform differences

There are none; behaviour depends only on the client host.

## Known limitations

- It does not capture traffic in the web view, traffic created by third-party components, or the traffic of other processes on the host.
- It does not record hub message frames or terminal bytes. It can therefore answer "was the connection established and did the call fail", but it cannot reconstruct terminal session content.
- It offers no request replay, editing or blocking, and exports no capture file.
- The terminal connection does not currently use automatic reconnection, so after a disconnect the UI only reports that the terminal must be reopened and shows no reconnect timeline.

## Related documentation

- [Settings](/docs/en-US/latest/apps/settings)
- [Protocol and Communication](/docs/en-US/latest/concepts/protocol)
- [Security model](/docs/en-US/latest/concepts/security)
