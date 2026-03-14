# PingTools

PingTools 是一个基于 WinForms 构建的 Windows 桌面网络诊断工具，主要用于对目标 IP 或域名执行可视化 Ping 探测，观察延迟、丢包、波动和链路状态，适合日常网络排查以及网络游戏延迟检测场景。

## 项目用途

这个项目的目标不是只做一个简单的 `ping` 命令外壳，而是提供一个更直观的图形化面板，用于：

- 查看目标的实时延迟变化
- 统计平均延迟、峰值延迟、丢包率
- 根据游戏场景评估链路状态
- 快速对本地回环地址、默认网关、常见公网目标发起探测
- 查看本机网卡、网关和 DNS 信息

## 当前功能

- 支持输入 IP 地址或域名并执行 Ping 探测
- 支持探测次数、超时值、间隔值配置
- 支持 `0` 或负数表示无限次探测
- 支持开始、停止、清空数据
- 支持实时结果流展示
- 支持延迟波形显示
- 支持链路状态判定
- 支持快捷键
  - `Enter`：开始探测
  - `Esc`：停止探测
  - `Delete`：清空数据
- 支持工具箱窗口
  - Ping 本地回环地址
  - Ping 默认网关
  - Ping 百度
  - Ping QQ
  - 查看网卡、网关、DNS 信息
  - 打开系统网络适配器和网络设置页面

## 链路状态判定逻辑

项目当前使用偏“游戏网络质量”导向的规则来判断链路状态：

- 不可达、主机不可达、网络不可达、路由异常、其他异常：`不可达`
- 丢包率大于等于 `5%`：`链路异常`
- 丢包率大于等于 `2%`：`波动`
- 在丢包正常时：
  - 平均延迟小于等于 `70ms`：优先判为 `优良`
  - 平均延迟小于等于 `120ms`：优先判为 `稳定`
  - 平均延迟大于 `120ms`：`偏高`
- 另外会结合最近 20 次成功样本的波动范围，对状态做波动修正

## 运行环境

- 开发框架：`.NET 8`
- UI 技术：`WinForms`
- 目标平台：Windows

注意：

- 项目不支持 Windows 7
- 自包含单文件发布可以在大多数较新的 Windows 环境直接运行，无需额外安装 .NET Runtime

## 开发与运行

在项目根目录执行：

```powershell
dotnet build
dotnet run
```

## 发布

如果需要打包为“自带 .NET、单文件 exe”的形式，可以使用：

```powershell
dotnet publish PingTools.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:EnableCompressionInSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true /p:DebugType=None /p:DebugSymbols=false
```

## 目录说明

- `MainWindow.cs`
  - 主探测界面与核心 Ping 逻辑
- `MainWindow.Designer.cs`
  - 主界面设计器代码
- `MainWindow.resx`
  - 主界面资源文件
- `ToolWindow.cs`
  - 工具箱窗口
- `PingTools.csproj`
  - 项目配置
- `app.ico`
  - 应用图标

## 项目定位

PingTools 当前更适合作为一个轻量但实用的本地网络诊断工具，重点在于：

- 图形化展示清晰
- 网络游戏场景友好
- 常用诊断目标切换方便
- 适合后续继续扩展 DNS、路由、带宽、链路质量等功能
