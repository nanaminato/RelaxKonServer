---
title: Connect to your first server
description: Choose a connection and enter the RelaxKonOS desktop.
category: Getting Started
order: 11
---

# Connect to your first server

These steps use the desktop client and a RelaxKonOS Server workspace. Availability depends on client and server versions, installation mode, and account permissions.

## 1. Prepare the client and server

Get a suitable client from [Downloads](/downloads). Have your server address and account ready. To install a server, read [Installation](/docs/en-US/latest/getting-started/installation) or [Linux user mode](/docs/en-US/latest/getting-started/user-mode).

![Screenshot: Prepare the client and server](/assets/tutorials/en-US/tutorial-connect-1.svg)

## 2. Choose a connection

Open the client and choose RelaxKonOS Server for a full workspace. A standalone SSH connection opens a compact desktop for terminal, SFTP, and server maintenance. Choose the mode that fits your task.

![Screenshot: Choose a connection](/assets/tutorials/en-US/tutorial-connect-2.svg)

## 3. Enter connection details

Enter the Server address and credentials. Linux user mode listens on loopback by default: choose the SSH tunnel option on the Server login page and enter the SSH host, port, account, and server-side address, such as http://127.0.0.1:5000. Verify the host key fingerprint on the first SSH connection.

![Screenshot: Enter connection details](/assets/tutorials/en-US/tutorial-connect-3.svg)

## 4. Sign in and confirm the desktop

Confirm that the desktop and Start menu appear. SSH and Server credentials are separate; sharing credentials requires an explicit choice and password authentication. If login fails, check the address, port, account, and server status. See [Login and account security](/docs/en-US/latest/getting-started/login).

![Screenshot: Sign in and confirm the desktop](/assets/tutorials/en-US/tutorial-connect-4.svg)

[Next tutorial：Get to know the desktop and windows](/docs/en-US/latest/getting-started/tutorial-desktop)
