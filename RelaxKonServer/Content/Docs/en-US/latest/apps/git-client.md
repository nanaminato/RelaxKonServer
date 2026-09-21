---
title: Git Client
description: Version control for Git repositories already present in the server host filesystem, with credentials left entirely to the host.
category: Applications
order: 45
---

# Git Client

Git Client works with Git repositories that **already exist in the RelaxKonOS server filesystem**. The UI renders locally on the client, while repository state, commit history and branch lists come from the server calling `git` in real time.

## Overview

Working tree state, branches, history and diffs are **snapshots of the moment**: nothing is cached and no tables are created.

**Authentication is entirely the host's business.** `git` runs as the host OS process and reuses the host user and its permissions; RelaxKonOS does not store, proxy or collect credentials for Git remotes. When a push or pull needs credentials the host's own credential helper or SSH agent handles it. If credentials are missing `git` fails, the UI explains it and points you at configuring the host.

## When to use it

- A directory on the server is a Git working tree and you want to inspect state, commit and pull directly on it.
- You want to operate repositories without giving RelaxKonOS any Git credentials.
- A pull produces a merge conflict and you need to resolve it server-side.

## Current capabilities

| Feature | Description |
| --- | --- |
| Repository selection | Registered repository list, the current selection and switching |
| Working tree state | Staged, unstaged, untracked and conflicted files, current branch and ahead/behind counts |
| Branches | Local and remote branch lists, checkout, create and delete |
| Commit | Select the files to stage, enter a message and commit |
| Pull | Merge or rebase for the current branch; a branch that is not checked out may only be fast-forwarded safely |
| Push | Push the current or a specified local branch to a chosen remote and branch without checking it out first |
| History | Commit list and single-commit detail |
| Diff | Single-file diff for the working tree, the index or a given commit |
| Conflict resolution | Three-pane comparison, per-hunk selection, per-file staging and an independent continue or abort |
| Revert | Create a reverse commit for a selected commit |

## Permissions and security

- Every operation calls `git` as the **host OS process**, so out-of-scope access is refused by the host and no second access control layer is added.
- Commands are passed as structured arguments rather than joined into a shell string.
- The working directory is fixed to the repository path recorded at registration; file arguments must sit under that repository root, with no escaping.
- Dangerous operations need confirmation: deleting an unmerged branch, reverting, checking out over uncommitted changes, and unregistering a repository.
- Unregistering a repository **removes the registration record only and never deletes the repository directory on disk**.
- Audit records the operator, time, repository, action and result, and never credentials.

## Platform differences

`git` behaves the same on Windows and Linux; the only platform difference is executable discovery and the boot environment, so the server has a single implementation.

## Known limitations

This is the **first-stage scope**. The following are not available, and some UI entry points are placeholders that state clearly that they are unavailable:

- Cherry-pick, interactive rebase and commit reordering.
- Stash, deep submodule management and tag management.
- A built-in three-way merge editor (conflict resolution currently centres on per-hunk selection).
- Full multi-remote management.
- Force push and hard reset, which are not exposed for safety.
- Binary diffs return no patch body, and very large text diffs are truncated and marked as such.

## Related documentation

- [Code Editor](/docs/en-US/latest/apps/code-editor)
- [File Manager](/docs/en-US/latest/apps/file-manager)
- [Security model](/docs/en-US/latest/concepts/security)
