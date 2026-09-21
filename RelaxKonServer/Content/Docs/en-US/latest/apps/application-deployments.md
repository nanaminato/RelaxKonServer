---
title: Application Deployments
description: Deploy images, Java, .NET and Python projects as containerized applications on the host, with revisions and rollback.
category: Applications
order: 37
---

# Application Deployments

Application Deployments is a built-in application **separate from Docker Manager**: Docker Manager operates the engine and container primitives, while Application Deployments treats an application as a long-lived entity to publish, observe and roll back.

## Overview

A deployment has a **desired state** (which revision to run, which configuration, which port) and an **actual state** (the container the engine really runs, plus its readiness result). When the two disagree the UI reports drift and explains whether it comes from a missing container, an external modification or an unowned resource.

## When to use it

- Deliver an image or a build artifact to a long-running host without signing in to run shell commands by hand.
- Keep previous revisions and fall back to the last one when a new version fails its readiness check.
- Watch the deployment process and live container logs without handing the host's Docker credentials to the client.

## Supported sources

| Source | Description |
| --- | --- |
| Container image | Name an image reference and tag; the server pins the image identity before creating the container |
| Java | Upload a JAR artifact; entry point and arguments are validated against the template |
| .NET | Upload a Web or Worker publish output; self-contained publishing is optional |
| Python | Upload a project with locked dependencies; the build happens inside the image |

Templates declare whether they need an archive, whether they need an image reference, whether they support self-contained publishing and their default container port, so entry point and argument validation happens before deployment.

## Current capabilities

- Multi-step deployment wizard covering source, artifact, entry point, ports, resources and proxy routing in one flow.
- Revisions and operation records: every publish creates an immutable revision, and operations stay traceable.
- Readiness checks: HTTP and process level; a candidate that is not ready never takes over traffic.
- Version rollback that reuses an already published revision instead of rebuilding.
- Live log streaming during deployment.
- Upload progress with transferred bytes, percentage and rate, plus cancellation.
- Image version selection from the tags available in the registry.
- Optional proxy routing: one root-path route per application, validated before the site configuration is saved.

## Permissions and security

- Read and manage operations are governed by separate policies; mutating requests require the host feature to be enabled.
- Long operations carry an idempotency key, so a retry cannot produce a second side effect.
- Changes to the same application are mutually exclusive, while different applications may run in parallel.
- Audit records the operator, target, result and operation ID only — never credentials.
- Secrets are encrypted with the server's data protection mechanism, and the API reports a version number only.
- Uploads are validated by entry count, expanded size and path boundary; out-of-scope paths and symbolic links are rejected.

## Platform differences

The deployment target is a **host running Docker Engine**, so this is primarily a Linux server feature; Windows is a suitable client and development environment.

## Known limitations

> **Current status: implemented, not yet verified.** Every implementation item compiles and passes static validation, but runtime behaviour has not been accepted on a real Docker Engine, so this page must not be read as "production ready".

- **Real engine acceptance is outstanding**: image pull, build, create, readiness, rollback and fault injection have not run on a host with Docker.
- **Single-service Compose project deployment is not implemented**: deployments are expressed with container primitives, so a Compose project cannot be submitted directly from the wizard.
- **No overall percentage is promised**: the build stage reports progress from the work in the current stage and shows unknown where there is no reliable denominator instead of inventing a number.
- It is not a continuous integration system: there is no source build, repository hook or pipeline definition.

## Related documentation

- [Docker Manager](/docs/en-US/latest/apps/docker)
- [Web Server Manager](/docs/en-US/latest/apps/web-server-manager)
- [Certificate Manager](/docs/en-US/latest/apps/certificate-manager)
