---
title: Client / Server Architecture
description: How local rendering and remote services work together.
category: Concepts
order: 40
---
# Client / Server Architecture

RelaxKonOS follows a state-sync model. The client owns desktop UI, input, window management, and local application rendering. The server owns identities, workspace lifecycle, storage, synchronization, and remote runtime.

```text
Client UI and Window Manager <-> Protocol <-> Server Workspace and Services
```

The server never captures or generates desktop images.
