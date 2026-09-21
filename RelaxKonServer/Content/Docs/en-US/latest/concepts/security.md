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

## Both permission layers must hold

Host-facing management features ([Docker Manager](/docs/en-US/latest/apps/docker), [Proxy Manager](/docs/en-US/latest/apps/proxy-manager), [Web Server Manager](/docs/en-US/latest/apps/web-server-manager), [Certificate Manager](/docs/en-US/latest/apps/certificate-manager)) are constrained by two layers at once:

```text
RelaxKonOS authorization (role / permission / authenticated session)
              +
Host OS privilege (service account rights, privileged helper)
```

A component running with high privilege **does not** mean any RelaxKonOS user may control it. Conversely, a user holding a permission does not mean the server has host privilege available.

## The boundary of the privileged helper

- The server **does not run as root or administrator**; every operation needing host privilege is handed to the helper.
- The helper accepts only **fixed, strongly typed** actions. It offers no "run any command as root" style entry point and accepts no arbitrary executable or arguments.
- A policy file written at install time decides what the service account may do, and the helper directory and policy files are not writable by that account.
- Everyday start and stop **should not trigger elevation repeatedly**: installation, authorization and the definition of privileged actions happen once, and later routine toggles need no user approval.

## The consequences of host authorization are explicit

- Capabilities needing host privilege are **off by default**. Docker is the clearest example: control of the daemon socket is close to root privilege, so deployment does not grant it automatically and you must opt in explicitly at install time (see [Installation](/docs/en-US/latest/getting-started/installation)).
- When privilege is missing a feature returns a **stable problem code** and explains why, and the UI asks you to redeploy or restart with more privilege instead of failing silently.
- When the helper is missing, uninstalled or refuses a call, the affected feature fails with a problem code; **running workloads are not interrupted** and saved configuration is not rolled back, so you can fix the cause and retry.
- The client **shows localized explanations only**: it never collects sudo, administrator or service-account passwords, and never turns the API into an arbitrary command elevation channel.

## Defence in depth

| Layer | Measures |
| --- | --- |
| Transport | HTTPS, JWT |
| Protocol | Explicit contracts, input validation |
| Server | Path normalisation, directory traversal protection, unified error handling |
| Execution | Structured arguments instead of shell concatenation, confirmation for destructive actions, operation auditing |
| Rendering | Markdown escaping; embedded HTML is not trusted by default |

## Secrets and logs

- Passwords, private keys, account keys, controller secrets, subscription URLs and proxy passwords **never reach logs, audit records or error details**.
- The operator-facing exception: a field that must round-trip through a form, such as a proxy address, is returned unchanged — otherwise one round trip would rewrite real credentials as a mask. Masking then applies only to logs and diagnostics aimed at other readers.
- Secrets that must be stored are encrypted with the server's data protection mechanism, and the API reports only a version or a reference.

## Related documentation

- [Installation](/docs/en-US/latest/getting-started/installation) (host authorization and the privileged helper)
- [Persistence](/docs/en-US/latest/concepts/persistence)
- [Sessions and devices](/docs/en-US/latest/concepts/session)
