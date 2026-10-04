---
title: 防火墙
description: 管理 Linux UFW 与 Windows Defender Firewall 的状态、默认策略及规则。
category: 应用程序
order: 32
---

# 防火墙

防火墙应用支持 Linux 的 UFW，以及 Windows 10/11 和 Windows Server 的 Windows Defender Firewall。Windows 系统模式与 Windows 10/11 个人模式均通过特权 Helper 管理。

## 功能

- 读取防火墙启用状态
- 查看带编号的规则列表
- 修改启用状态与默认策略
- 添加或删除经过结构化校验的规则

## 使用方式

打开防火墙应用查看当前状态。修改启用状态、默认策略或规则时，Linux 与 Windows 都使用统一的宿主管理员授权；已验证的管理员无需重复确认，其他用户通过授权窗口验证管理员。

## 权限与安全

- Linux 使用 UFW；Windows 使用原生防火墙接口
- root 会话无需再次验证
- 其他会话使用统一的宿主管理员临时授权
- 规则经过结构化校验，避免任意命令注入

## Windows 防火墙

- 读取状态，启用或停用防火墙，设置默认入站与出站策略。
- 启停和默认策略同时应用于**域、专用、公用**三个网络配置；不同配置的默认策略不一致时，统一策略显示为空。
- 新增、编辑和删除本应用管理的规则；其他应用、系统、组策略以及安装助手创建的规则不在此列表中。
- Windows 支持允许／阻止、入站／出站、TCP／UDP／任意协议、IP/CIDR 和端口范围。任意协议必须使用任意端口；端口表示入站本地端口或出站远程端口。
- Windows 不提供 UFW 的 reject 与 limit。规则默认应用于三个网络配置，来源和目标应填写需要的范围。
- 修改需经统一宿主管理员授权，并由 Helper 调用 Windows 原生接口；Helper 不可用或组策略禁止本地修改时返回错误。

更改可能中断当前管理连接。其他 Windows 规则可由管理员使用 `wf.msc` 管理。个人模式安装向导的局域网放行规则仍只允许 Domain/Private 的 LocalSubnet，与本应用规则分开管理。

## 相关文档

- [安全模型](/docs/zh-CN/latest/concepts/security)
- [端口转发](/docs/zh-CN/latest/apps/port-forwarding)
