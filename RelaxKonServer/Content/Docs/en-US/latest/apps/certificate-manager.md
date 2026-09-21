---
title: Certificate Manager
description: Local certificate lifecycle management: ACME issuance, deployment and automatic renewal.
category: Applications
order: 43
---

# Certificate Manager

Certificate Manager is a **local certificate lifecycle manager**. RelaxKonOS is itself the ACME client and does not use controller/agent architectures, multi-server scheduling or cross-server private key distribution.

## Overview

Three principles shape the whole module:

1. **Private keys are generated and stored on this host only** and are never returned to the client.
2. **Issuance and deployment are separate**: the certificate manager does not know about Nginx, IIS or reverse proxies; deployment is handled by dedicated deployers.
3. **RelaxKonOS does not need to hold port 80 permanently.**

## When to use it

- The server is reachable from the internet and RelaxKonOS' own HTTPS endpoint should use a trusted certificate.
- An existing web server already holds port 80, but validation should still complete without taking that port over.
- Certificates should renew automatically instead of being replaced by hand every year.

## Current capabilities

- ACME v2 issuance where the authority is configured through a directory URL, with no hard-coded CA.
- Validation methods: automatic, HTTP-01 (temporary listener or web root) and DNS-01. The temporary listener is used only while port 80 is free, routes the challenge path only, and releases deterministically once issuance completes, is cancelled or times out.
- Web root mode: the challenge file is written and read back for verification, then cleaned up after validation.
- Certificate storage as a file store using PEM as the canonical format, with the same model on Windows and Linux.
- Key algorithms: RSA and ECDSA.
- Deployment target: RelaxKonOS' own HTTPS endpoint (Kestrel).
- Automatic renewal driven by the renewal information (ARI) the ACME authority provides, with certificate expiry as a fallback.
- Management UI with an overview and a certificate list; the overview shows managed count, refresh state and the issuance entry point, and the certificate page offers deploy, renew, revoke and delete for the current selection.
- Issuance dialog: domain, contact email, validation type, key algorithm and public reachability confirmation sit inside a scrollable content area together with the preflight result and issuance progress.

## Permissions and security

- Operations that change host state (issue, renew, delete, import, deploy, listen on port 80, change the HTTPS binding) run only while RelaxKonOS has administrator rights, and report stable problem codes when it does not.
- The client shows localized explanations only; it never collects or forwards sudo, UAC or service-account passwords, and never turns the API into an arbitrary command elevation channel.
- **Private keys, account keys, CSRs and full CA responses appear in no API response, log, audit record or error detail.** The normal API returns the domain, issuer, serial number, validity window, status, thumbprint and renewal state.
- Preflight returns a structured result: domain normalization and duplicate SAN checks, A and AAAA resolution, port 80 listening rights and occupation, firewalls and upstream proxies. Preflight never fabricates a "publicly reachable" conclusion.
- The API returns an operation ID and stage for long tasks; cancelling or disconnecting does not lose server-side state.

## Platform differences

- Target platforms are **Ubuntu 24.04 LTS** and **Windows Server 2016 or later**.
- On Linux the private key directory is restricted to the service account; on Windows filesystem ACLs limit it to the service account and the system.
- The certificate core does not depend on the Windows certificate store, so one platform's store does not become a global dependency.

## Known limitations

- **Target platform acceptance is outstanding**: the core and the basic management UI are implemented, but real authorities, listening behaviour and file permissions still have to be verified on the target hosts.
- **DNS-01 and wildcard certificates are not implemented** and belong to a later stage, so an environment that cannot expose port 80 at all cannot complete validation today.
- **Nginx, IIS and Apache deployment integration is not implemented**; the only deployment target today is RelaxKonOS' own HTTPS endpoint.
- A free port 80 is not a guarantee that validation will succeed; when DNS, NAT, CDN or upstream proxy state cannot be determined the UI marks it as requiring administrator confirmation.

## Related documentation

- [Web Server Manager](/docs/en-US/latest/apps/web-server-manager)
- [Security model](/docs/en-US/latest/concepts/security)
- [Application Deployments](/docs/en-US/latest/apps/application-deployments)
