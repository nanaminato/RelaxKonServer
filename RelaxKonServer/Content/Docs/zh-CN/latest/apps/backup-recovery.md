---
title: 备份与恢复
description: 备份与恢复
category: 应用程序
order: 55
---

# 备份与恢复

> 本页对照当前源码说明能力，不是某个发布包的功能清单。已发布产物与日期见[发行说明](/releases/0.1.2)，功能验收状态以所链接的实现记录为准。

## 三种不同的操作

| 操作 | 当前边界 |
| --- | --- |
| 应用修订回滚 | 使用旧镜像/配置，不撤销数据卷或数据库的数据迁移 |
| 应用定义备份与恢复 | 服务端已有加密定义归档、清单校验、只读预检及恢复到新停机实例的路径 |
| 数据卷/数据库恢复 | 缺少对应一致性适配器和真实验证，不能宣称已经可恢复 |

## 定义恢复流程

1. 创建定义备份，等待重新读取、认证标签与摘要校验完成；只有 Verified 清单可以进入恢复流程。
2. 预检备份、密钥、来源/目标安装、当前对象版本与资源冲突。有阻止条件时不能提交。
3. 显式确认后创建一个新的、停止的应用定义。不会继承旧宿主端口或站点绑定，也不会覆盖原实例、数据卷或秘密。
4. 单独核对配置、补齐可用秘密和资源，再由用户确认启动或流量切换。

Android 已接入创建备份、清单和只读预检，尚未接入恢复提交 UI。备份能力需要明确配置可恢复密钥域；复制 secrets.json 或加密文件不等于跨安装可以解密。中断项不会自动重放写入。

来源：[共享恢复契约](https://github.com/nanaminato/RelaxKonOS/blob/master/docs/applications/RelaxKonOS.BackupRecovery.Contract.md)、[Android 恢复边界](https://github.com/nanaminato/RelaxKonOS/blob/master/Client/RelaxKonOS.Client.Android/docs/features/OperationsRecovery.md)。
