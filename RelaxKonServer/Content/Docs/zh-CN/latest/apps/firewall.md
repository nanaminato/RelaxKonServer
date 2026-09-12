---
title: 防火墙
description: 管理 Linux 服务端宿主的 UFW 防火墙状态、默认策略与规则。
category: 应用程序
order: 32
---

# 防火墙

防火墙应用管理 **Linux 服务端宿主**上的 UFW（Uncomplicated Firewall）。

## 功能

- 读取防火墙启用状态
- 查看带编号的规则列表
- 修改启用状态与默认策略
- 添加或删除经过结构化校验的规则

## 使用方式

打开防火墙应用查看当前状态。修改启用状态、默认策略或规则时，非 root 用户需要以**自身密码**通过 PAM 进行一次确认。

## 权限与安全

- Linux 专用；Windows Server 上不显示此应用
- root 会话无需再次验证
- 其他用户每次变更都需 PAM 一次性确认
- 规则经过结构化校验，避免任意命令注入

## 相关文档

- [安全模型](/docs/zh-CN/latest/concepts/security)
- [端口转发](/docs/zh-CN/latest/apps/port-forwarding)
