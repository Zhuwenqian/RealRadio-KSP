# RealRadio

Kerbal Space Program 1.12.5 模组开发环境。

## 目录结构

```
RealRadio/
├── KSP DLL/                    # KSP 与 Unity 程序集目录
├── GameData/
│   └── RealRadio/
│       ├── Plugins/            # 编译后的 DLL 会自动复制到这里
│       └── RealRadio.version   # KSP-AVC 版本文件
├── Properties/
│   └── AssemblyInfo.cs         # 程序集元数据
├── Source/
│   └── RealRadioMod.cs         # 模组入口类
├── readme/
│   └── 功能更新.md              # 功能更新日志
├── RealRadio.csproj            # MSBuild 项目文件
└── RealRadio.sln               # Visual Studio 解决方案文件
```

## 编译说明

### 使用 Visual Studio

1. 打开 `RealRadio.sln`。
2. 选择 `Debug` 或 `Release` 配置。
3. 按 `Ctrl+Shift+B` 编译。
4. 编译成功后，`RealRadio.dll` 会自动复制到 `GameData/RealRadio/Plugins/`。

### 使用命令行（MSBuild）

```powershell
msbuild RealRadio.sln /p:Configuration=Release
```

### 使用命令行（dotnet）

```powershell
dotnet build RealRadio.sln -c Release
```

## 安装到 KSP

将 `GameData/RealRadio/` 文件夹复制到 KSP 安装目录的 `GameData/` 下即可。

## 版本

- 模组版本：0.1.0.0
- 目标 KSP 版本：1.12.5
- 目标框架：.NET Framework 4.7.2
