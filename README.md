# 视频剪辑工具 (Video Cut Tool)

## 项目概述

一个基于Windows系统的桌面视频剪辑工具，支持对单个视频进行精确的时间轴编辑、分割、删除和导出操作。采用现代化的WPF + Material Design技术栈，提供专业级的视频编辑体验。

## 🎯 当前实现状态

### ✅ 已完成的核心功能

#### 1. **视频导入与预览**
- ✅ 支持文件选择对话框导入视频文件
- ✅ 支持格式：MP4, AVI, MOV, MKV, WebM等FFmpeg支持的格式
- ✅ 实时视频预览播放（MediaElement集成）
- ✅ 显示视频基本信息（分辨率、帧率、时长、文件大小）
- ✅ 视频元数据自动解析（使用FFmpeg/FFprobe）

#### 2. **时间轴编辑系统**
- ✅ 可视化时间轴显示（自定义TimelineControl）
- ✅ 时间点精确定位（支持毫秒级精度）
- ✅ 当前播放位置指示器（可拖拽播放头）
- ✅ 时间轴缩放功能（1x-100x，支持按钮和滑块控制）
- ✅ 时间标尺显示（分钟:秒格式）
- ✅ 视频缩略图生成和显示
- ✅ 音频波形可视化
- ✅ 切分点标记和管理

#### 3. **视频分割与编辑**
- ✅ 在指定时间点分割视频（播放头位置分割）
- ✅ 时间轴片段管理（TimelineSegment模型）
- ✅ 片段选择和播放功能
- ✅ 实时预览分割效果
- ✅ 片段重命名和管理

#### 4. **操作历史管理**
- ✅ 撤销/重做功能（基于Action栈实现）
- ✅ 操作历史记录
- ✅ 快捷键支持（S-分割，Del-删除，Ctrl+Z-撤销等）
- ✅ 命令状态管理（CanUndo, CanRedo）

#### 5. **项目文件管理**
- ✅ 项目保存功能（JSON格式）
- ✅ 项目加载功能
- ✅ 项目信息管理（ProjectInfo模型）
- ✅ 最近导出记录管理

#### 6. **导出功能**
- ✅ 多格式导出支持（MP4, AVI等）
- ✅ 质量设置（分辨率、帧率、比特率）
- ✅ 高级导出设置对话框
- ✅ 导出进度显示
- ✅ 导出历史记录

#### 7. **用户界面特性**
- ✅ Material Design风格界面
- ✅ 深色主题支持
- ✅ 响应式布局设计
- ✅ 自定义窗口控制（最小化、关闭）
- ✅ 可调整时间轴高度
- ✅ 现代化滚动条样式

## 🏗️ 技术架构

### 核心技术栈

#### 框架与平台
- **.NET 8.0 + WPF**: 现代化桌面应用框架
- **Windows 10/11**: 目标平台

#### 界面库
- **MaterialDesignInXamlToolkit 4.9.0**: Material Design风格界面
- **MaterialDesignColors 2.1.4**: 主题色彩支持

#### MVVM框架
- **CommunityToolkit.Mvvm 8.2.2**: 现代化MVVM框架
- **ObservableObject**: 属性通知基类
- **RelayCommand**: 命令绑定支持

#### 依赖注入
- **Microsoft.Extensions.DependencyInjection 8.0.0**: 依赖注入容器
- **Microsoft.Extensions.Hosting 8.0.0**: 应用程序主机

#### 视频处理
- **FFmpeg.AutoGen 6.1.0**: FFmpeg视频处理库
- **LibVLCSharp 3.8.2**: VLC媒体播放器集成

#### 日志系统
- **Serilog 3.1.1**: 结构化日志框架
- **Serilog.Extensions.Hosting 8.0.0**: 主机集成
- **Serilog.Sinks.File 5.0.0**: 文件日志
- **Serilog.Sinks.Debug 2.0.0**: 调试日志

#### 配置管理
- **Microsoft.Extensions.Configuration 8.0.0**: 配置管理
- **Microsoft.Extensions.Configuration.Json 8.0.0**: JSON配置支持

### 架构模式

#### MVVM架构
- **View层**: XAML界面定义（MainWindow, TimelineControl等）
- **ViewModel层**: 业务逻辑和状态管理（MainWindowViewModel, TimelineControlViewModel）
- **Model层**: 数据模型（VideoInfo, TimelineSegment, ProjectInfo等）

#### 服务层架构
- **IVideoService**: 视频处理服务接口
- **VideoService**: FFmpeg视频处理实现
- **IFileDialogService**: 文件对话框服务
- **IProjectService**: 项目文件管理服务

#### 依赖注入
- 使用Microsoft.Extensions.DependencyInjection进行服务注册
- 支持单例和瞬态服务生命周期
- 自动配置和日志集成

## 📁 项目结构

```
video_cut_tool/
├── VideoCutTool.sln                    # 解决方案文件
├── appsettings.json                    # 应用程序配置
├── PHASE1_SUMMARY.md                   # 第一阶段开发总结
├── src/
│   ├── VideoCutTool.WPF/               # WPF主项目
│   │   ├── App.xaml                    # 应用程序入口
│   │   ├── App.xaml.cs                 # 应用程序代码（DI配置）
│   │   ├── VideoCutTool.WPF.csproj     # 项目文件
│   │   ├── Views/                      # 视图层
│   │   │   ├── MainWindow.xaml         # 主窗口视图
│   │   │   ├── MainWindow.xaml.cs      # 主窗口代码
│   │   │   ├── AdvancedSettingsWindow.xaml # 高级设置对话框
│   │   │   └── Controls/               # 自定义控件
│   │   │       ├── TimelineControl.xaml # 时间轴控件
│   │   │       └── TimelineControl.xaml.cs # 时间轴控件代码
│   │   ├── ViewModels/                 # 视图模型层
│   │   │   ├── MainWindowViewModel.cs  # 主窗口视图模型
│   │   │   ├── TimelineControlViewModel.cs # 时间轴视图模型
│   │   │   └── AdvancedSettingsViewModel.cs # 高级设置视图模型
│   │   ├── Models/                     # 模型层
│   │   │   ├── VideoInfo.cs           # 视频信息模型
│   │   │   ├── TimelineSegment.cs     # 时间轴片段模型
│   │   │   ├── ProjectInfo.cs         # 项目信息模型
│   │   │   ├── ProjectFile.cs         # 项目文件模型
│   │   │   ├── ExportSettings.cs      # 导出设置模型
│   │   │   └── RecentExport.cs        # 最近导出记录模型
│   │   ├── Services/                   # 服务层
│   │   │   ├── IVideoService.cs       # 视频服务接口
│   │   │   ├── VideoService.cs        # 视频服务实现
│   │   │   ├── IFileDialogService.cs  # 文件对话框服务接口
│   │   │   ├── FileDialogService.cs   # 文件对话框服务实现
│   │   │   ├── IProjectService.cs     # 项目服务接口
│   │   │   └── ProjectService.cs      # 项目服务实现
│   │   ├── Converters/                 # 值转换器
│   │   │   ├── VolumeConverter.cs     # 音量转换器
│   │   │   └── SelectedBackgroundConverter.cs # 选中背景转换器
│   │   └── Resources/                  # 资源文件
│   │       ├── Styles/                # 样式文件
│   │       ├── Themes/                # 主题文件
│   │       └── Icons/                 # 图标文件
│   ├── VideoCutTool.Core/              # 核心业务逻辑
│   └── VideoCutTool.Infrastructure/    # 基础设施层
├── logs/                               # 日志文件目录
└── README.md                           # 项目文档
```

## 🎨 界面设计

### 主窗口布局
- **顶部工具栏**: 标题栏、窗口控制按钮、主要操作按钮
- **左侧面板**: 编辑工具、视频信息、快捷键说明
- **中央区域**: 视频播放器、播放控制、时间轴控件
- **右侧面板**: 导出设置、项目信息、最近导出记录
- **底部状态栏**: 状态信息和进度显示

### 时间轴控件特性
- **Grid布局**: 使用Grid替代Canvas嵌套，提高性能和可维护性
- **三层结构**: 时间标尺、视频缩略图轨道、音频波形轨道
- **播放头**: 可拖拽的播放位置指示器，支持顶部和底部拖拽手柄
- **缩放控制**: 1x-100x缩放，支持按钮和滑块控制
- **切分点**: 可视化切分点标记
- **缩略图缓存**: 本地缓存机制，提高性能

### 设计风格
- **Material Design**: Google Material Design设计语言
- **深色主题**: 专业的深色配色方案
- **响应式**: 支持窗口大小调整
- **现代化**: 扁平化设计，圆角元素

## 🔧 核心功能实现

### 视频处理
- **FFmpeg集成**: 使用FFmpeg进行视频信息解析、缩略图生成、视频分割
- **硬件加速**: 支持GPU加速编解码
- **异步处理**: 所有视频操作使用异步模式
- **进度报告**: 导出操作支持进度显示

### 时间轴系统
- **精确控制**: 毫秒级时间精度
- **可视化**: 时间标尺、缩略图、音频波形
- **交互性**: 拖拽播放头、点击定位、缩放控制
- **性能优化**: 虚拟化显示、缓存机制

### 项目管理
- **JSON格式**: 项目文件使用JSON格式存储
- **版本控制**: 项目文件版本管理
- **完整性**: 保存所有编辑状态和设置
- **兼容性**: 向前兼容设计

## 🚀 运行环境

### 系统要求
- **操作系统**: Windows 10/11 (64位)
- **.NET版本**: .NET 8.0 Runtime
- **内存**: 8GB+ RAM (推荐16GB)
- **显卡**: 支持硬件加速的显卡
- **存储**: 至少2GB可用磁盘空间

### 依赖软件
- **FFmpeg**: 需要安装FFmpeg并配置环境变量
- **Visual Studio 2022**: 开发环境（推荐）

### 安装步骤
1. 安装.NET 8.0 SDK
2. 安装FFmpeg并配置PATH环境变量
3. 克隆项目代码
4. 运行 `dotnet restore` 安装依赖
5. 运行 `dotnet build` 编译项目
6. 运行 `dotnet run` 启动应用

## 📋 开发计划

### 已完成阶段
- ✅ **第一阶段**: 基础框架搭建（项目结构、MVVM架构、UI设计）
- ✅ **第二阶段**: 视频处理核心（FFmpeg集成、视频导入、播放器）

### 当前阶段
- 🔄 **第三阶段**: 编辑功能实现（时间轴、分割、历史管理）
- 🔄 **第四阶段**: 导出与优化（多格式导出、性能优化、错误处理）

### 后续计划
- 📅 **第五阶段**: 高级功能（批量处理、插件系统、云端集成）
- 📅 **第六阶段**: 部署（安装包）

## 🎉 项目特色

### 技术优势
- **现代化架构**: 基于.NET 8.0和最新WPF技术
- **高性能**: 异步处理、硬件加速、内存优化
- **可扩展**: 模块化设计、依赖注入、插件架构
- **用户友好**: Material Design界面、直观操作

### 功能特色
- **专业级编辑**: 精确的时间轴控制、多格式支持
- **高效工作流**: 快捷键支持、撤销重做、项目管理
- **可视化编辑**: 缩略图预览、音频波形、实时反馈

## 🤝 贡献指南

欢迎提交Issue和Pull Request来改进这个项目。

### 开发规范
- 遵循MVVM架构模式
- 使用async/await进行异步操作
- 添加适当的日志记录
- 编写单元测试
- 遵循C#编码规范

## 📄 许可证

MIT License

---

**项目状态**: 开发中 | **最后更新**: 2025年7月24日 | **版本**: 1.0.0
