# zClock

Windows 上的 PowerPoint 倒计时与局域网提词器。幻灯片备注可以显示在电脑备用屏幕，也可以通过手机、平板浏览器扫码查看。

## 功能

- 开始放映后自动计时，支持暂停、重置、超时红色显示和时间设置。
- 同步当前页标题、备注、页码和倒计时。
- 检测非放映屏幕，自动显示可移动、缩放、置顶的提词窗口。
- 手机扫码连接；网页适配电脑和移动设备，支持字号、主题、镜像、自动滚动和断线重连。
- 默认只共享当前页备注；可在设置中开启全部备注预览。
- 首次启用局域网服务可申请 Windows 管理员授权以添加限定范围的防火墙规则；支持关闭和手动重试。
- 重置连接密钥可使旧连接地址和二维码失效。

## 使用

需要 Windows、.NET Framework 4.6 或更新版本、桌面版 Microsoft PowerPoint。Microsoft Office 不随本项目提供；暂不支持 WPS。手机与电脑须处于可互通的同一 IPv4 子网。

启动 `bin\Release\zClock.exe`，右击时钟或托盘图标，选择 **提词器 → 手机扫码连接**。首次授权需要你确认 Windows 的 UAC 提示。

完整操作说明见 [提词器使用说明](提词器使用说明.md)，数据与网络边界见 [隐私说明](PRIVACY.md)。

## 从源码构建

安装 Visual Studio 的“.NET 桌面开发”工作负载（包含 .NET Framework SDK / ResGen）及 NuGet CLI。PowerPoint 仅在运行联动功能及联动测试时需要，不是编译依赖。

```powershell
nuget restore zClock.sln -NonInteractive
powershell -ExecutionPolicy Bypass -File .\build.ps1
powershell -ExecutionPolicy Bypass -File .\tests\Run-Checks.ps1
```

可以在 Visual Studio 打开 `zClock.sln`，还原包后构建。该方式需要安装 .NET Framework 4.6 targeting pack；命令行脚本使用本机 Framework 编译器。

进行真实 PowerPoint 联动测试：

```powershell
powershell -ExecutionPolicy Bypass -File .\tests\Run-Checks.ps1 -PowerPoint
```

联动测试会创建并关闭临时演示文稿；发现已有 PowerPoint 进程时拒绝运行，避免影响现有工作。自动化测试不会申请实际 UAC 授权或修改防火墙。

## 已知限制

- 网页使用局域网 HTTP，没有 TLS 加密，只适合可信网络。知道完整地址且能访问服务的设备可读取已共享内容。
- 备注以纯文本显示，不转换备注图片、墨迹或富文本样式。
- 相邻页预览需要显式开启“允许网页预览全部备注”；当前页优先，其余页在后台分批读取。
- 多屏和手机浏览器能力受实际设备影响；没有安全备用屏幕时不自动投放备注。手动打开窗口时由使用者选择位置。
- 默认端口为 `18765`；访客网络隔离、组织安全策略和第三方防火墙可能影响连接。

## 许可证

项目源码采用 **GNU GPL v3.0 only（GPL-3.0-only）**，见 [LICENSE](LICENSE)。第三方 QRCoder 保留 MIT 许可证，见 [第三方声明](THIRD_PARTY_NOTICES.md)。不分发 Microsoft Office、Office 互操作程序集或演示文稿。
