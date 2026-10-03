---
title: 文件上传与续传
description: 文件上传与续传
category: 应用程序
order: 13
---

# 文件上传与续传

> 本页对照当前源码说明能力，不是某个发布包的功能清单。已发布产物与日期见[发行说明](/releases/0.1.2)，功能验收状态以所链接的实现记录为准。

## 上传、续传与取消

桌面与 Android 的大文件上传使用分块会话，不把整个文件装入内存。客户端显示已传字节、进度、速率与取消；小文件可走有明确上限的单次请求。

1. 选择远端目标目录与本机源文件，确认冲突处理。
2. 服务端创建上传会话并报告分片大小与已确认偏移。
3. 网络中断后先查询服务端偏移，再继续上传；不能仅凭本机进度判定成功。恢复需要会话仍有效且源文件可用、未变更。
4. 所有字节收到后单独提交，暂存文件经同卷原子改名成为目标文件。上传到 100% 不等于提交已经完成。
5. 取消会放弃会话并清理暂存；超时或失效后按界面状态重新开始。

## 路径、权限与空间

权限不足不会显示为空目录。受保护目录使用单独的文件授权与受限 Helper 通道，不自动绕过宿主权限。授权过期需重新确认；用户模式仍限制在当前 Unix home。声明大小、可用磁盘与暂存预算都由服务端检查，不能把可续传理解为无限大小或无限保留。

## Android 与验收

Android 将不能随机读取的选中文档暂存为可续传来源，通过应用级传输与前台通知跟踪任务。进程停止、系统后台限制、源权限失效与真实网络切换的验收单独追踪；不承诺在任何后台条件下自动完成。

下载以流式方式落盘；本页的分块会话说明适用于上传，不意味着下载也使用同一会话协议。SSH 文件传输走 SFTP，不使用 Server 上传接口。

实现与测试：[共享上传契约](https://github.com/nanaminato/RelaxKonOS/blob/master/Shared/RelaxKonOS.Protocol/Files/UploadContracts.cs)、[文件上传记录](https://github.com/nanaminato/RelaxKonOS/blob/master/docs/architecture/RelaxKonOS.FileUpload.Design.md)、[Android 传输说明](https://github.com/nanaminato/RelaxKonOS/blob/master/Client/RelaxKonOS.Client.Android/docs/features/FileTransfers.md)。
