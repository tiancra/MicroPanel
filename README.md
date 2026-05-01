# MicroPanel

<div align="center">

![Platform](https://img.shields.io/badge/Platform-Windows%20%7C%20Android-blue)
![Framework](https://img.shields.io/badge/Framework-Avalonia%20UI%20%7C%20.NET%20MAUI-purple)
![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)
[![License](https://img.shields.io/badge/License-GPL--3.0-green)](./LICENSE)

**Yunzai-Bot 管理面板 - 桌面客户端与移动应用**

[English](./EN_README.md) | 简体中文

</div>

## 📖 项目简介

MicroPanel 是 [micro-plugin](https://github.com/V2233/micro-plugin) 的配套客户端项目，提供了 Windows 桌面客户端和移动端应用，用于便捷地管理多个 Yunzai 机器人服务器。

本项目包含两个子项目：

- **MicroPanel** - Windows 桌面客户端（Avalonia UI）
- **MicroPanelApp** - 跨平台移动应用（.NET MAUI）

## ✨ 功能特性

### 🖥️ 桌面客户端 (Windows)

- **多服务器管理** - 同时管理多个 Yunzai 服务器
- **实时状态监控** - CPU、内存、磁盘使用情况
- **快速连接** - 一键打开 Web 管理面板
- **多用户支持** - 为每个服务器配置多个账号
- **离线提醒** - 服务器状态异常通知
- **深色/浅色主题** - 支持主题切换

### 📱 移动应用 (Android/iOS)

- **服务器列表** - 查看所有服务器状态
- **状态概览** - 实时监控系统资源
- **快速访问** - 内置浏览器访问面板
- **推送通知** - 服务器离线提醒
- **多平台支持** - Android、iOS、Windows

## 🚀 快速开始

### 环境要求

- **桌面客户端**: Windows 10 或更高版本
- **移动应用**: Android 8.0+ / iOS 15.0+
- **开发环境**: .NET 10 SDK

### 安装桌面客户端

#### 方式一：直接下载（推荐）

1. 访问 [GitHub Releases](https://github.com/V2233/micro-plugin/releases) 下载最新版本
2. 解压到任意目录
3. 运行 `MicroPanel.exe`

#### 方式二：自行编译

```bash
# 克隆仓库
git clone https://github.com/V2233/micro-plugin.git

# 进入桌面客户端目录
cd MicroPanel/MicroPanel

# 还原依赖
dotnet restore

# 编译发布版本
dotnet publish -c Release -r win-x64 --self-contained

# 运行
./bin/Release/net10.0/win-x64/publish/MicroPanel.exe
```

### 安装移动应用

#### Android

1. 下载 APK 文件（待发布）
2. 允许安装未知来源应用
3. 安装并打开应用

#### iOS

> 目前 iOS 版本正在开发中，敬请期待。

## 📖 使用指南

### 添加服务器

1. 打开 MicroPanel 应用
2. 点击 **"添加服务器"** 按钮
3. 填写服务器信息：
   - 服务器地址（如：`192.168.1.100:23306`）
   - 用户名
   - 密码
4. 点击 **"确定"** 完成添加

### 查看服务器状态

- 主界面以卡片形式展示所有服务器
- 显示 CPU、内存、磁盘使用率
- 绿色圆点表示在线，红色表示离线

### 打开 Web 面板

- 点击服务器卡片进入详情页
- 点击 **"打开面板"** 按钮
- 自动在内置浏览器或系统浏览器中打开

### 管理用户

1. 右键点击服务器卡片
2. 选择 **"管理用户"**
3. 添加、编辑或删除用户账号

## 🏗️ 项目结构

```
MicroPanel/
├── MicroPanel/                 # Windows 桌面客户端
│   ├── Assets/                 # 资源文件
│   ├── Converters/             # 值转换器
│   ├── Models/                 # 数据模型
│   ├── Services/               # 服务层
│   │   ├── ApiService.cs       # API 服务
│   │   ├── ServerManager.cs    # 服务器管理
│   │   └── ThemeService.cs     # 主题服务
│   ├── Views/                  # 视图层
│   │   ├── Controls/           # 自定义控件
│   │   ├── Pages/              # 页面
│   │   └── Windows/            # 窗口
│   ├── App.axaml               # 应用入口
│   ├── MainWindow.axaml        # 主窗口
│   └── MicroPanelAvalonia.csproj
│
├── MicroPanelApp/              # 移动应用 (.NET MAUI)
│   ├── Platforms/              # 平台特定代码
│   ├── MainPage.xaml           # 主页面
│   └── MicroPanelApp.csproj
│
├── docs/                       # 文档
│   ├── guide/                  # 使用指南
│   └── advanced/               # 高级功能
│
└── micro-plugin/               # 服务端插件（子模块）
```

## 🛠️ 开发指南

### 技术栈

- **桌面客户端**: Avalonia UI 11.3 + FluentAvalonia
- **移动应用**: .NET MAUI
- **目标框架**: .NET 10
- **语言**: C# 12

### 开发环境搭建

1. 安装 [.NET 10 SDK](https://dotnet.microsoft.com/download)
2. 安装 Visual Studio 2022 或 VS Code
3. 安装 Avalonia 扩展（VS Code）

### 运行桌面客户端

```bash
cd MicroPanel

dotnet restore
dotnet run
```

### 运行移动应用

```bash
cd MicroPanelApp

dotnet restore
dotnet build
```

## 📚 文档

- [快速开始](./docs/guide/getting-started.md)
- [安装指南](./docs/guide/installation.md)
- [使用教程](./docs/guide/usage.md)
- [API 参考](./docs/advanced/api.md)

## 🤝 贡献指南

欢迎提交 Issue 和 Pull Request！

1. Fork 本仓库
2. 创建你的特性分支 (`git checkout -b feature/AmazingFeature`)
3. 提交你的修改 (`git commit -m 'Add some AmazingFeature'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 打开一个 Pull Request

## 📄 许可证

本项目采用 [GPL-3.0](./LICENSE) 许可证开源。

## 🙏 鸣谢

- [Avalonia UI](https://avaloniaui.net/) - 跨平台 .NET UI 框架
- [FluentAvalonia](https://github.com/amwx/FluentAvalonia) - Fluent Design 主题
- [.NET MAUI](https://dotnet.microsoft.com/apps/maui) - 跨平台移动应用框架
- [micro-plugin](https://github.com/V2233/micro-plugin) - 服务端插件

## 📞 联系我们

- QQ 交流群: [397798018](http://qm.qq.com/cgi-bin/qm/qr?_wv=1027&k=6qeMfgydE5k8e_nTorXz0ywmahixBTFw)
- GitHub Issues: [V2233/micro-plugin](https://github.com/V2233/micro-plugin/issues)

---

<div align="center">

**Made with ❤️ by MicroPanel Team**

</div>
