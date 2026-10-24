# 再再启程工具快捷键 Mod

添加按下快捷键使用工具的功能。
**注意**：我只测试了游戏后期所有工具都为最高级下的情况。

## 安装依赖

### BepInEx 5（必须）

`BepInEx 5`项目地址：[BepInEx 5](https://github.com/BepInEx/BepInEx/)。

1. 打开 Releases 页面，选择 **5.4** 版本，下载 `BepInEx_win_x64_5.4.x.zip`（不确定是否兼容其他 5.x 版本）。
2. 解压，把里面的文件全部放到游戏根目录（和 `再再启程.exe` 放在同一个文件夹）。放好后应该能看到 `BepInEx` 文件夹、`winhttp.dll` 和 `doorstop_config.ini`。
3. 启动一次游戏再退出。游戏目录里出现 `BepInEx\LogOutput.log`，就说明 BepInEx 装好了。
4. 打开 `BepInEx\config\BepInEx.cfg`，把 `[Chainloader]` 下的 `HideManagerGameObject` 改成 `true`。这个游戏会清掉可见的 BepInEx 管理器，不改的话插件虽然显示加载成功，但按 `F1` 和数字键都不会有反应。

### ConfigurationManager（游戏内菜单，推荐）

可以在游戏里按 `F1` 打开设置菜单，并在里面修改本 mod 的快捷键。不装也能用，只是要手动改 `<游戏根目录>\BepInEx\config\com.warspite102.onewaytickets.toolshortcutmod.cfg`。

1. 打开 [ConfigurationManager 的 Releases 页面](https://github.com/BepInEx/BepInEx.ConfigurationManager/releases)，下载文件名带 `BepInEx5` 的 zip，比如 `BepInEx.ConfigurationManager_BepInEx5_v19.1.zip`，不要下 IL2CPP 版。
2. 解压到游戏根目录，最终应该是 `BepInEx\plugins\ConfigurationManager\ConfigurationManager.dll`。
3. 进游戏按 `F1`，能看到插件列表就说明装好了。

## 使用方法

1. 从本项目的 Releases 页面下载 `OneWayTicket.ToolShortcutMod.dll`，放到 `<游戏目录>\BepInEx\plugins\ToolShortcutMod\`。
2. 启动游戏，进入关卡后按主键盘的数字键（不是小键盘）切换工具：

| 按键 | 工具 |
| --- | --- |
| `1` | 维修 |
| `2` | 灭火 |
| `3` | 清理 |
| `4` | 切割 |
| `5` | 工头 |

### 改快捷键

**ConfigurationManager**：
1. 进游戏按 `F1`，找到 `Tool Shortcut Mod`
2. 展开“工具快捷键”，修改对应按键

**手动**：
1. 打开 `BepInEx\config\com.warspite102.onewaytickets.toolshortcutmod.cfg`
2. 手动修改键，比如： `维修 = Alpha1` 里的 `Alpha1` 改成别的键名（比如 `F1`、`Q`），每个按键的名称见 [KeyCode文档](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/KeyCode.html)

## 编译（用户不用管）

### 1. 环境

- 安装 [.NET 8 SDK](https://dotnet.microsoft.com/download) 或更新的版本
- 游戏《再再启程 / One Way Tickets》
- BepInEx 5（安装方法见上一节）

### 2. 编译

在项目根目录打开 PowerShell，运行：

```powershell
dotnet build -c Release -p:GameDir="<游戏根目录>"
```
比如：
```powershell
dotnet build -c Release -p:GameDir="E:\SteamLibrary\steamapps\common\再再启程 One Way Tickets"
```

### 3. 结果

dll 文件会放在：

```
bin\Release\netstandard2.1\OneWayTicket.ToolShortcutMod.dll
```

把这个 dll 文件复制到：

```
<游戏根目录>\BepInEx\plugins\ToolShortcutMod\
```
