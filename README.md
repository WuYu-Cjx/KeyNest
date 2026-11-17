<div align="center">

<img src="assets/icon.png" width="128" alt="KeyNest icon" />

# 键巢 KeyNest

按下即见，快捷键归巢。
Every shortcut, back to its nest.

[![License](https://img.shields.io/badge/License-Apache--2.0-blue.svg)](https://github.com/WuYu-Cjx/KeyNest/blob/main/LICENSE)
![Platform](https://img.shields.io/badge/Platform-Windows%2010%20%7C%2011-0078D6.svg)
![.NET](https://img.shields.io/badge/.NET-8-512BD4.svg)
![Status](https://img.shields.io/badge/Status-Local--only-2ea44f.svg)

键巢是一款 Windows 本地快捷键提示工具。启用后按下 Ctrl、Shift、Alt、Win 中任意修饰键，悬浮窗立即显示包含当前组合的快捷键；松开全部修饰键立即隐藏。

KeyNest is a local Windows shortcut companion. Press any Ctrl, Shift, Alt, or Win modifier and a floating panel instantly shows matching shortcuts; release all modifiers to hide it.

[简体中文](#简体中文) | [English](#english)

</div>

---

## 简体中文

### 目录

- [功能特性](#功能特性)
- [安装](#安装)
- [使用教程](#使用教程)
- [快捷键触发规则](#快捷键触发规则)
- [隐私说明](#隐私说明)
- [常见问题](#常见问题)
- [免责声明](#免责声明)
- [许可证](#许可证)
- [贡献](#贡献)

### ⚡ 功能特性

- **任意修饰键触发**：按下 Ctrl、Shift、Alt、Win 中的任意一个或任意组合，即可唤出提示。
- **组合实时收窄**：每多按住一个修饰键，列表就自动收窄为同时包含这些键的快捷键。
- **松手立即隐藏**：松开全部修饰键后悬浮窗立刻消失，不需要额外点击或按键。
- **托盘常驻、默认关闭**：程序启动后只驻留在托盘，监听默认处于关闭状态，由你决定何时启用。
- **开机自启到托盘**：可选开机自动启动，启动后直接进入托盘，不弹出主窗口。
- **三种主题**：浅色、深色、跟随系统，切换后立即生效。
- **圆角悬浮窗不抢焦点**：提示窗口不激活、不夺取输入焦点，不打断当前正在进行的操作。
- **本地离线、只展示不执行**：不联网、不上传、不记录按键历史，只展示快捷键说明，默认不拦截系统按键。

### 🖼 截图

> 截图占位，发布前替换为实际截图。

| 浅色主题 | 深色主题 |
| :---: | :---: |
| <img src="assets/screenshot-light.png" width="420" alt="Light theme screenshot" /> | <img src="assets/screenshot-dark.png" width="420" alt="Dark theme screenshot" /> |

| 设置界面 |
| :---: |
| <img src="assets/screenshot-settings.png" width="640" alt="Settings screenshot" /> |

截图占位，发布前替换为实际截图：`assets/screenshot-light.png`、`assets/screenshot-dark.png`、`assets/screenshot-settings.png`。

### 📦 安装

#### 从 GitHub Releases 安装（推荐）

[![Download KeyNest](https://img.shields.io/badge/Download-KeyNest--Setup.exe-2ea44f?logo=windows)](https://github.com/WuYu-Cjx/KeyNest/releases)

1. 点击上方 **Download** 按钮，或在浏览器打开 [github.com/WuYu-Cjx/KeyNest/releases](https://github.com/WuYu-Cjx/KeyNest/releases)，进入 Releases 发布页。
2. 在最新版本下方的 Assets 列表中，点击 `KeyNest-Setup-<版本号>.exe` 下载安装包。
3. 双击运行安装包，按向导选择安装目录，并决定是否为当前用户创建桌面与开始菜单快捷方式。
4. 安装向导中可选择“开机启动到托盘”。该项**默认不勾选**；即使勾选，程序启动后的监听状态仍**默认关闭**。
5. 完成安装并启动程序，键巢会常驻托盘；右键托盘图标选择“启用”，即可开始使用。

#### 从源码构建

需要 Windows 10 / 11、Visual Studio 2022（含 .NET 桌面开发工作负载）与 .NET 8 SDK。

```powershell
git clone https://github.com/WuYu-Cjx/KeyNest.git
cd KeyNest
dotnet build KeyNest.sln -c Release
```

也可以直接用 Visual Studio 2022 打开 `KeyNest.sln` 后生成解决方案。仓库内的 `build/package.ps1` 用于打包安装程序，产物输出到 `dist/` 目录。

### 🕹 使用教程

1. 启动键巢，程序常驻系统托盘，此时监听为**关闭**状态。
2. 在托盘图标上右键，选择“启用”，开始监听修饰键。
3. 按下任意修饰键，例如 Ctrl，悬浮窗立即弹出并列出所有包含 Ctrl 的快捷键。
4. 继续按住 Shift，列表收窄为同时包含 Ctrl 与 Shift 的组合。
5. 松开 Shift，列表重新扩展到只包含 Ctrl 的组合。
6. 松开全部修饰键，悬浮窗立即隐藏。
7. 需要暂时停止时，在托盘菜单中选择“停用”；不再使用时选择“退出”。

设置项说明：

| 设置项 | 说明 |
| --- | --- |
| 主题 | 浅色、深色、跟随系统，切换后立即生效。 |
| 语言 | 简体中文与英文，切换后立即生效，并写入本地配置。 |
| 透明度 | 调整悬浮窗的整体透明度。 |
| 位置 | 可拖动悬浮窗到合适位置，也可使用“重置位置”恢复默认。 |
| 接管单按 Win | 是否在只按 Win 键时也唤出提示，默认关闭。 |
| 包含无修饰键单键 | 是否在列表中包含 F1–F12 等无修饰键单键快捷键，默认关闭。 |

示例：按下组合与悬浮窗内容

| 按下的组合 | 悬浮窗展示内容 |
| --- | --- |
| 只按 `Ctrl` | 所有包含 Ctrl 的快捷键，按组合分组，完全匹配的 Ctrl 组排在最前 |
| `Ctrl` + `Shift` | 收窄为同时包含 Ctrl 与 Shift 的快捷键 |
| `Win` + `Alt` | 收窄为同时包含 Win 与 Alt 的快捷键 |
| 松开至只按 `Win` | 扩展回所有包含 Win 的快捷键 |
| 松开全部修饰键 | 悬浮窗立即隐藏 |

### ⌨️ 快捷键触发规则

设当前按下的修饰键集合为 P（Ctrl / Shift / Alt / Win 的子集），快捷键自身包含的修饰键集合为 K。

- 当 P 非空时，悬浮窗展示所有满足 `(K & P) == P` 的快捷键，即 K 必须完整包含 P 中的每一个修饰键。
- 完全匹配优先：与 P 完全相同的分组排在最前，多余修饰键越少的分组越靠前。
- 按组合分组：同一修饰键组合的快捷键归入同一分组，分组内按分类与名称排序。
- 松开全部修饰键时立即隐藏，不做任何延迟。
- 默认不拦截系统按键；是否接管单按 Win 为可选项，默认关闭。
- 无修饰键单键快捷键默认不单独触发，需要通过设置显式开启。

### 🔒 隐私说明

- **不联网**：程序不发起任何网络请求，不进行更新检查与遥测。
- **不上传**：不存在任何数据上报通道，也没有账号体系。
- **不记录按键历史**：不写入按键序列、不统计使用频率、不生成画像。
- **不保存输入内容**：只判断修饰键状态，不读取字符输入与剪贴板内容。
- **只保存本地配置**：主题、语言、透明度、窗口位置、是否开机自启等设置写入本机用户目录下的配置文件。

### ❓ 常见问题

<details>
<summary>单按 Win 键会打开开始菜单，会和键巢冲突吗？</summary>

默认不会。键巢默认不接管单按 Win，也不会拦截系统按键，因此开始菜单仍按系统原有方式响应。只有你在设置中显式开启“接管单按 Win”后，才由键巢响应单按 Win。即使是内核级系统行为，不同系统版本下的表现也可能略有差异。
</details>

<details>
<summary>安装时被杀毒软件误报怎么办？</summary>

未签名的本地工具被启发式引擎误报是常见现象。建议先确认安装包来自本仓库的 Releases 页面，并核对发布说明中的校验信息；如有需要，可以选择添加信任或提交误报样本给杀毒软件厂商。作者也会按仓库内 `docs/SIGNING.txt` 的说明推进代码签名，以降低误报率。
</details>

<details>
<summary>支持哪些 Windows 版本？</summary>

面向 Windows 10 与 Windows 11（64 位）。更早的系统版本未做适配与验证，不保证可用。
</details>

<details>
<summary>键巢会记录我的按键吗？</summary>

不会。程序只读取修饰键的按下与松开状态用于即时筛选，不保存按键历史、输入内容或使用频率，也不上传任何数据。
</details>

<details>
<summary>可以用于商业环境吗？</summary>

可以。代码以 Apache-2.0 许可证发布，允许商用，但须遵守许可证的署名与声明要求。项目名称、Logo 与图标保留商标权与版权，不随代码授权，二次分发时请勿使用相同名称与图标造成混淆。
</details>

<details>
<summary>卸载后会保留我的配置吗？</summary>

卸载程序默认提供“保留配置文件”选项，勾选后主题、语言与窗口位置等设置会被保留，便于重新安装后继续使用；不勾选则一并清理。
</details>

### ⚠️ 免责声明

- 本项目不是微软官方项目，与 Microsoft Corporation 无任何关联、赞助或背书关系。
- 快捷键展示内容仅供学习与参考，不保证在所有系统版本、输入法、全屏游戏、远程桌面、UAC 安全桌面等环境下完全一致。
- 键巢只展示快捷键说明，不执行任何快捷键，也不会代为触发系统功能。
- 使用本软件的风险由使用者自行承担；请在遵守当地法律与所在平台规则的前提下使用。
- 项目名称、Logo 与图标保留商标权与版权，不随代码许可证授权。

### 📄 许可证

代码以 [Apache-2.0](https://github.com/WuYu-Cjx/KeyNest/blob/main/LICENSE) 许可证发布，版权归作者所有（Copyright 2026 键巢 KeyNest）。项目名称“键巢 / KeyNest”、Logo 与图标保留商标权与版权，不随代码授权。第三方依赖及其许可证见 [THIRD-PARTY-NOTICES.txt](https://github.com/WuYu-Cjx/KeyNest/blob/main/docs/THIRD-PARTY-NOTICES.txt)。

### 🤝 贡献

键巢由作者个人独立开发与维护，并以 Apache-2.0 许可证开源。

- 欢迎提交 Issue 与 Pull Request；所有改动由作者审核后合入。
- 项目不设 CLA：提交 PR 即表示你同意以 Apache-2.0 许可证授权该贡献，并保留你对自己贡献的署名。
- 请勿提交来源不明、许可证不兼容（如 GPL/AGPL）的代码、图标、字体或文案。
- 新增或修改界面文案时，请同时补齐简体中文与英文两种资源。

[⬆ 返回顶部](#简体中文)

---

## English

### Table of Contents

- [Features](#features)
- [Installation](#installation)
- [Usage](#usage)
- [Trigger Rules](#trigger-rules)
- [Privacy](#privacy)
- [FAQ](#faq)
- [Disclaimer](#disclaimer)
- [License](#license)
- [Contributing](#contributing)

### ⚡ Features

- **Any modifier triggers it**: press Ctrl, Shift, Alt, or Win — alone or in any combination — and the panel appears.
- **Live narrowing**: each additional modifier held instantly narrows the list to shortcuts that contain all of them.
- **Hide on release**: the panel disappears the moment every modifier is released, with no extra click.
- **Tray-first, off by default**: KeyNest starts in the notification area with listening disabled, and you decide when to turn it on.
- **Launch at sign-in to tray**: optionally start with Windows and land directly in the tray, with no main window.
- **Three themes**: light, dark, and follow system, applied immediately.
- **Rounded panel that never steals focus**: the overlay does not activate or grab keyboard focus, so your current task keeps running.
- **Local, offline, display-only**: no network, no uploads, no keystroke history; it only shows descriptions and does not intercept system keys by default.

### 🖼 Screenshots

> Placeholder — replace with real screenshots before release.

| Light theme | Dark theme |
| :---: | :---: |
| <img src="assets/screenshot-light.png" width="420" alt="Light theme screenshot" /> | <img src="assets/screenshot-dark.png" width="420" alt="Dark theme screenshot" /> |

| Settings |
| :---: |
| <img src="assets/screenshot-settings.png" width="640" alt="Settings screenshot" /> |

Placeholder — replace with real screenshots before release: `assets/screenshot-light.png`, `assets/screenshot-dark.png`, `assets/screenshot-settings.png`.

### 📦 Installation

#### From GitHub Releases (Recommended)

[![Download KeyNest](https://img.shields.io/badge/Download-KeyNest--Setup.exe-2ea44f?logo=windows)](https://github.com/WuYu-Cjx/KeyNest/releases)

1. Click the **Download** button above, or open [github.com/WuYu-Cjx/KeyNest/releases](https://github.com/WuYu-Cjx/KeyNest/releases) in your browser to reach the Releases page.
2. Under the latest release, click `KeyNest-Setup-<版本号>.exe` in the Assets list to download the installer.
3. Run the installer, choose the installation directory, and decide whether to create desktop and Start menu shortcuts for the current user.
4. The wizard offers "Start with Windows, minimized to tray". It is **unchecked by default**, and even when enabled, the listener still starts **disabled**.
5. Finish the installation and launch KeyNest. It stays in the tray; right-click the tray icon and choose "Enable" to start.

#### Build from Source

Requirements: Windows 10 / 11, Visual Studio 2022 with the .NET desktop development workload, and the .NET 8 SDK.

```powershell
git clone https://github.com/WuYu-Cjx/KeyNest.git
cd KeyNest
dotnet build KeyNest.sln -c Release
```

You can also open `KeyNest.sln` in Visual Studio 2022 and build the solution there. The bundled `build/package.ps1` script produces the installer package under `dist/`.

### 🕹 Usage

1. Launch KeyNest. It runs in the notification area with listening **off**.
2. Right-click the tray icon and choose "Enable" to start listening for modifiers.
3. Press any modifier, for example Ctrl. The panel appears immediately and lists every shortcut that includes Ctrl.
4. Keep holding Shift. The list narrows to shortcuts that include both Ctrl and Shift.
5. Release Shift. The list expands again to Ctrl-only combinations.
6. Release all modifiers. The panel hides immediately.
7. To pause, choose "Disable" from the tray menu; to stop entirely, choose "Exit".

Settings:

| Setting | Description |
| --- | --- |
| Theme | Light, dark, or follow system; applied immediately. |
| Language | Simplified Chinese or English; applied immediately and saved to local configuration. |
| Opacity | Adjusts the overall transparency of the floating panel. |
| Position | Drag the panel where you want it, or use "Reset position" to restore the default. |
| Handle single Win press | Whether a lone Win press also opens the panel. Off by default. |
| Include unmodified keys | Whether to include single-key shortcuts such as F1–F12. Off by default. |

Example: pressed combination vs. panel content

| Pressed combination | What the panel shows |
| --- | --- |
| `Ctrl` only | Every shortcut containing Ctrl, grouped by combination, with the exact Ctrl group first |
| `Ctrl` + `Shift` | Narrows to shortcuts containing both Ctrl and Shift |
| `Win` + `Alt` | Narrows to shortcuts containing both Win and Alt |
| Release back to `Win` only | Expands to every shortcut containing Win |
| Release all modifiers | The panel hides immediately |

### ⌨️ Trigger Rules

Let P be the set of modifiers currently held (a subset of Ctrl / Shift / Alt / Win) and K the set of modifiers a shortcut requires.

- When P is not empty, the panel shows every shortcut satisfying `(K & P) == P`, meaning K must contain each modifier in P.
- Exact matches come first: the group identical to P is listed at the top, and groups with fewer extra modifiers rank higher.
- Shortcuts are grouped by combination; within a group they are ordered by category and name.
- Releasing all modifiers hides the panel immediately, with no delay.
- System keys are not intercepted by default; handling a lone Win press is opt-in and disabled by default.
- Single-key shortcuts without modifiers never trigger on their own unless explicitly enabled in settings.

### 🔒 Privacy

- **No network**: the application makes no outbound requests, with no update checks and no telemetry.
- **No uploads**: there is no reporting channel and no account system.
- **No keystroke history**: no key sequences are stored, no frequency statistics are collected, no profiles are built.
- **No input content**: only modifier state is inspected; typed characters and clipboard contents are never read.
- **Local configuration only**: theme, language, opacity, window position, and autostart preferences are stored in a per-user file on your machine.

### ❓ FAQ

<details>
<summary>A single Win press opens the Start menu — will that conflict with KeyNest?</summary>

No, not by default. KeyNest does not take over a lone Win press and does not intercept system keys, so the Start menu keeps responding the way Windows normally does. Only after you explicitly enable "Handle single Win press" does KeyNest respond to it. Even then, behavior may vary slightly across Windows versions because of kernel-level shell handling.
</details>

<details>
<summary>My antivirus flags the installer. What should I do?</summary>

Heuristic false positives are common for unsigned local tools. First confirm the installer came from this repository's Releases page and verify the checksum listed in the release notes; if needed, add a trust exception or submit the sample to your vendor as a false positive. The author also follows `docs/SIGNING.txt` to code-sign the binaries and reduce the false-positive rate.
</details>

<details>
<summary>Which Windows versions are supported?</summary>

Windows 10 and Windows 11, 64-bit. Older versions are neither adapted nor tested, so they are not guaranteed to work.
</details>

<details>
<summary>Does KeyNest record my keystrokes?</summary>

No. It only reads modifier press and release states to filter the list in real time. It stores no keystroke history, input content, or usage frequency, and uploads nothing.
</details>

<details>
<summary>Can I use it commercially?</summary>

Yes. The code is released under Apache-2.0, which permits commercial use as long as you comply with its attribution and notice requirements. The project name, logo, and icon remain protected by trademark and copyright and are not licensed with the code, so do not redistribute under the same name or icon in a way that causes confusion.
</details>

<details>
<summary>Does uninstalling keep my configuration?</summary>

The uninstaller offers a "keep configuration" option, checked by default in the wizard. When selected, theme, language, and window position survive the uninstall so a later reinstall picks up where you left off; clear it to remove everything.
</details>

### ⚠️ Disclaimer

- This is not an official Microsoft project and has no affiliation, sponsorship, or endorsement from Microsoft Corporation.
- Shortcut descriptions are provided for learning and reference only and may not match every Windows build, IME, fullscreen game, Remote Desktop session, or UAC secure desktop.
- KeyNest only displays shortcut descriptions. It does not execute shortcuts or trigger system features on your behalf.
- You use this software at your own risk, and you are responsible for complying with local laws and platform rules.
- The project name, logo, and icon remain protected by trademark and copyright and are not licensed with the code.

### 📄 License

The code is licensed under [Apache-2.0](https://github.com/WuYu-Cjx/KeyNest/blob/main/LICENSE), with copyright held by the author (Copyright 2026 键巢 KeyNest). The "键巢 / KeyNest" name, logo, and icon remain protected by trademark and copyright and are not licensed with the code. Third-party dependencies and their licenses are listed in [THIRD-PARTY-NOTICES.txt](https://github.com/WuYu-Cjx/KeyNest/blob/main/docs/THIRD-PARTY-NOTICES.txt).

### 🤝 Contributing

KeyNest is developed and maintained by a single author and released under Apache-2.0.

- Issues and pull requests are welcome; every change is reviewed and merged by the author.
- There is no CLA: by submitting a pull request you agree to license your contribution under Apache-2.0, and you keep credit for it.
- Do not submit code, icons, fonts, or text of unknown origin or under incompatible licenses such as GPL/AGPL.
- When you add or change UI text, add both Simplified Chinese and English resources.

[⬆ Back to top](#english)

---

<div align="center">

Made with care for Windows shortcut lovers.
为 Windows 快捷键爱好者用心制作。

[⬆ 返回顶部 / Back to top](#简体中文)

</div>
