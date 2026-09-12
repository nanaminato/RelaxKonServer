---
title: Port Forwarding
description: Create loopback-only SSH tunnels to reach ports on the server side.
category: Applications
order: 34
---

# Port Forwarding

Port Forwarding creates a local SSH forward that maps a service on the server side to `127.0.0.1` on your device.

## Features

- Binds the requested port when free, otherwise picks an available loopback port
- Returns the actual usable local link
- Lists, updates and stops running tunnels
- Can be requested by the browser or other first-party services

## How to use it

1. Open Port Forwarding
2. Choose the target host and remote port
3. Copy the generated `http://127.0.0.1:<port>` link
4. Open it in the browser

## Relationship to the workspace

SSH settings and running tunnels are stored **on the client only** and are not synchronised to the workspace. That is deliberate: a tunnel describes the network path from this device to the server.

## Security

- Tunnels bind to `127.0.0.1` only and are never exposed to the LAN
- Host SSH credentials are used; RelaxKonOS stores no additional keys

## Related documentation

- [Browser](/docs/en-US/latest/apps/browser)
- [Security model](/docs/en-US/latest/concepts/security)
