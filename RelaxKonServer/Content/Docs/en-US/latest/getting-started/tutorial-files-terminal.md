---
title: Your first file and terminal workflow
description: Browse server files and identify your terminal host with read-only commands.
category: Getting Started
order: 17
---

# Your first file and terminal workflow

These steps use the desktop client and a RelaxKonOS Server workspace. Availability depends on client and server versions, installation mode, and account permissions.

## 1. Browse server directories

Open File Manager and enter your user directory. Double-click folders and check the address bar. User mode is limited to the current Unix home. Check the account and path if access is denied.

![Screenshot: Browse server directories](/assets/tutorials/en-US/tutorial-files-terminal-1.svg)

*Screenshot: Browse server directories*

## 2. Create a practice folder

Create a folder such as relaxkon-tutorial in a writable directory, then try renaming it. Use your own practice files and review the target before confirming deletion. See [File transfers](/docs/en-US/latest/apps/file-transfers) for uploads and downloads.

![Screenshot: Create a practice folder](/assets/tutorials/en-US/tutorial-files-terminal-2.svg)

*Screenshot: Create a practice folder*

## 3. Identify the terminal host

Open Terminal from Start. In a Linux shell, run pwd, whoami, and uname -a separately. In Windows Command Prompt, use cd, whoami, and ver. These read-only commands identify the server, account, and current directory.

![Screenshot: Identify the terminal host](/assets/tutorials/en-US/tutorial-files-terminal-3.svg)

*Screenshot: Identify the terminal host*

## 4. Understand files and sessions

Both apps operate on server resources. The terminal directory does not automatically follow File Manager. Server terminal sessions can survive a brief disconnect; server restarts and explicitly ended sessions need separate checks, so do not assume tasks run forever. See [Terminal](/docs/en-US/latest/apps/terminal).

![Screenshot: Understand files and sessions](/assets/tutorials/en-US/tutorial-files-terminal-4.svg)

*Screenshot: Understand files and sessions*
