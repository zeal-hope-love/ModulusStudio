# 文字图片取模软件

> 这是 AI 写的软件，这份文档也是 AI 写的。

把文字或图片转换成点阵字模数据，用于 STM32 等单片机驱动 TFT 屏幕。

## 功能

- 文字取模：字体、字号、加粗/斜体/下划线、像素偏移
- 四种取模方式：列行式、行列式、逐列式、逐行式（带扫描动画预览）
- 阴码/阳码、顺向/逆向
- C51、A51 输出格式，模板可编辑（也支持自定义模板）
- 图片取模：BMP / JPG / PNG / GIF / ICO 等，支持裁剪、阈值、反色、扫描方向
- WinUI 3 界面，浅色 Mica 背景
- 从 GitHub Releases 自动检查并更新

## 运行

直接运行 `publish` 目录里的 `ModulusStudio.App.WinUI.exe`。单文件、自带运行时，双击即可使用，不需要安装 .NET。

## 构建

需要 .NET 10 SDK。

```powershell
dotnet build src\ModulusStudio.App.WinUI\ModulusStudio.App.WinUI.csproj -p:Platform=x64
```

发布单文件：

```powershell
dotnet publish src\ModulusStudio.App.WinUI\ModulusStudio.App.WinUI.csproj -c Release -r win-x64 --self-contained true -p:Platform=x64 -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None -o publish
```

## 来源

基于原版「文字取模软件」和「图片取模软件」重写，界面改用 WinUI 3。
