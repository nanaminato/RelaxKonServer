---
title: 事件与告警中心
description: 事件与告警中心
category: 应用程序
order: 53
---

# 事件与告警中心

> 本页说明当前源码能力；发布包内容见[发行说明](/releases/0.1.2)，验收状态见相关实现文档。

事件与告警中心已有持久事件账本、聚合告警、游标查询与基础处理 API，桌面已有最小只读摘要/列表。部署与 Compose 的终态信号有最小接入；事件中心故障不会把已经完成的部署回滚。

## 读取与处理

- 阅读告警时核对目标资源、状态、严重性、次数与最后观察时间；告警列表不是所有资源的实时健康证明。
- 确认、允许类型的人工关闭、期限抑制与解除抑制由独立 API 授权并审计，不能把读取权限当作处理权限。
- 实时 Hub 提供失效通知，权威详情需要重新读取 REST。最小桌面界面不代表已接入完整详情、处理控件与深链。

## 未交付范围

部署/Compose 的可靠持久重放、证书/Docker/隧道完整事件源、Guardian sequence/checkpoint、完整桌面交互和真实故障演练仍待完成。Android 运维观察和前台提示不等于可靠后台告警。中心目前不能作为唯一的生产监控或事件送达保证。

来源：[实施进度](https://github.com/nanaminato/RelaxKonOS/blob/master/docs/applications/RelaxKonOS.EventAlertCenter.Progress.md)、[Android 任务与恢复](https://github.com/nanaminato/RelaxKonOS/blob/master/Client/RelaxKonOS.Client.Android/docs/features/OperationsRecovery.md)。
