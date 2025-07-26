@echo off
:: 设置 UTF-8 编码
chcp 65001 >nul

:: 设置控制台标题
title AI探趣星船长 - 视频剪辑工具发布脚本

echo ========================================
echo    AI探趣星船长 - 视频剪辑工具发布脚本
echo ========================================
echo.

:: 设置发布目录
set PUBLISH_DIR=publish
set APP_NAME=AI探趣星船长-视频剪辑工具

echo 正在清理旧的发布文件...
if exist "%PUBLISH_DIR%" rmdir /s /q "%PUBLISH_DIR%"

echo.
echo 正在发布应用程序...
dotnet publish "src\VideoCutTool.WPF\VideoCutTool.WPF.csproj" -c Release -o "%PUBLISH_DIR%" --self-contained true -r win-x64

if %ERRORLEVEL% NEQ 0 (
    echo 发布失败！
    echo 请检查错误信息并重试。
    pause
    exit /b 1
)

echo.
echo 正在创建发布包...
cd "%PUBLISH_DIR%"

:: 创建应用程序目录
if not exist "%APP_NAME%" mkdir "%APP_NAME%"

:: 移动文件到应用程序目录
move "*.exe" "%APP_NAME%\" >nul 2>&1
move "*.dll" "%APP_NAME%\" >nul 2>&1
move "*.json" "%APP_NAME%\" >nul 2>&1
move "*.pdb" "%APP_NAME%\" >nul 2>&1

:: 创建README文件
echo # AI探趣星船长 - 视频剪辑工具 > "%APP_NAME%\README.txt"
echo. >> "%APP_NAME%\README.txt"
echo ## 使用说明 >> "%APP_NAME%\README.txt"
echo. >> "%APP_NAME%\README.txt"
echo 1. 双击 "VideoCutTool.WPF.exe" 启动程序 >> "%APP_NAME%\README.txt"
echo 2. 本软件为绿色免安装版本，可直接运行 >> "%APP_NAME%\README.txt"
echo 3. 支持视频导入、剪辑、分割、导出等功能 >> "%APP_NAME%\README.txt"
echo. >> "%APP_NAME%\README.txt"
echo ## 系统要求 >> "%APP_NAME%\README.txt"
echo - Windows 10 或更高版本 >> "%APP_NAME%\README.txt"
echo - 64位操作系统 >> "%APP_NAME%\README.txt"
echo - 至少 4GB 内存 >> "%APP_NAME%\README.txt"
echo. >> "%APP_NAME%\README.txt"
echo ## 功能特性 >> "%APP_NAME%\README.txt"
echo - 视频导入和预览 >> "%APP_NAME%\README.txt"
echo - 精确时间轴剪辑 >> "%APP_NAME%\README.txt"
echo - 视频片段分割 >> "%APP_NAME%\README.txt"
echo - 项目保存和加载 >> "%APP_NAME%\README.txt"
echo - 多格式视频导出 >> "%APP_NAME%\README.txt"
echo - Material Design 界面 >> "%APP_NAME%\README.txt"
echo. >> "%APP_NAME%\README.txt"
echo ## 注意事项 >> "%APP_NAME%\README.txt"
echo - 首次运行可能需要几秒钟启动时间 >> "%APP_NAME%\README.txt"
echo - 请确保系统已安装 FFmpeg（程序会自动检测） >> "%APP_NAME%\README.txt"
echo - 建议在 SSD 硬盘上运行以获得更好的性能 >> "%APP_NAME%\README.txt"
echo. >> "%APP_NAME%\README.txt"
echo Copyright © 2024 AI探趣星船长 >> "%APP_NAME%\README.txt"

:: 创建压缩包
echo 正在创建压缩包...
powershell -command "Compress-Archive -Path '%APP_NAME%' -DestinationPath '%APP_NAME%-绿色版.zip' -Force" >nul 2>&1

echo.
echo ========================================
echo 发布完成！
echo.
echo 发布文件位置：
echo - 应用程序目录: %PUBLISH_DIR%\%APP_NAME%\
echo - 压缩包: %PUBLISH_DIR%\%APP_NAME%-绿色版.zip
echo.
echo 您可以将整个 %APP_NAME% 文件夹复制到任何地方运行
echo ========================================
echo.
pause 