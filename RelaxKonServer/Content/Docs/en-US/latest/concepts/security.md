---
title: Security Model
description: Identity, permissions and the elevation boundary in RelaxKonOS.
category: Concepts
order: 64
---

# Security Model

RelaxKonOS does not rebuild an identity and permission system. It **delegates identity and file permissions to the host operating system**.

## Three themes

### 1. Identity delegated to the host

- Windows: the native `LogonUser` API, supporting local and domain accounts
- Linux: PAM with NSS
- RelaxKonOS issues its own token only after the host confirms the credentials, and **never stores passwords**

### 2. Permissions inherited from the host

- File operations run as the signed-in user on the host OS
- There is no separate ACL layer
- Out-of-scope requests are refused by the host OS

### 3. Elevation isolated

- Privileged operations run through a dedicated helper process
- The contract is narrow and audited
- Destructive actions require explicit confirmation
- Existing host mechanisms (sudo, UFW, systemd) are reused where possible

## Defence in depth

| Layer | Measures |
| --- | --- |
| Transport | HTTPS, JWT |
| Protocol | Explicit contracts, input validation |
| Server | Path normalisation, directory traversal protection, unified error handling |
| Rendering | Markdown escaping; embedded HTML is not trusted by default |

## Related documentation

- [Persistence](/docs/en-US/latest/concepts/persistence)
- [Sessions and devices](/docs/en-US/latest/concepts/session)
