---
title: Docker 管理器
description: 管理宿主机的 Docker Engine：容器、镜像、Stack、网络与卷。
category: 应用程序
order: 28
---

# Docker 管理器

Docker 管理器（RemoteDocker）在桌面里管理**服务端宿主机**的 Docker Engine。

## 功能

- 引擎状态检测与安装引导
- 容器：查看、启动、停止、重启
- 镜像：列表与拉取
- Stack：Compose 校验、部署与停止
- 网络与卷：查看与管理

## 实现方式

服务端通过 `IDockerEngineService` 调用 `docker` CLI，通过 `IDockerComposeService` 处理 Compose 编排，端点位于 `/api/v1.0/docker/*`。

## 权限与安全

- 操作以服务进程在宿主 OS 上的身份执行
- 需要 Docker 权限的宿主（例如 `docker` 组或 root）才能完成变更类操作
- 不引入独立的凭据存储

## 相关文档

- [进程守护](/docs/zh-CN/latest/apps/process-guardian)
- [Web Server 管理器](/docs/zh-CN/latest/apps/file-services)
