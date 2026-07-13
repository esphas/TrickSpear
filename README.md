# 花枪 Trick Spear

花式舞矛，灵感来自于 Trick Saber。

![preview01](static/preview01.gif)

[Steam 创意工坊](https://steamcommunity.com/sharedfiles/filedetails/?id=3759869025)

## 功能介绍

需先在键位设置中配置花枪键，默认键盘为 Q 键、手柄为 LB 键。

在游戏中可以按下花枪键施展花枪动作。长按会持续施展。

模组设置中提供了以下功能选项:

 - 自动花枪: 在特定行动时，自动施展一次花枪。目前支持滑行、快速转身、翻滚、后空翻。
 - [战斗] 小型物体交互: 旋转时，矛头可击落悬挂物、扫开轻型物体。
 - [战斗] 格挡: 每次旋转开始时，可以格挡并弹开远程攻击。默认判定窗口为 5 帧。

## 开发构建

需要在 `lib/` 中补充依赖文件: 
 - 来自 [Improved Input Config](https://github.com/zombieseatflesh7/improved-input-config) 的 `ImprovedInput.dll` 和 `ImprovedInput.xml`
 - 来自 [Rain Meadow](https://github.com/henpemaz/Rain-Meadow) 的 `Rain Meadow.dll`

在 `GamePaths.local.props` 中配置 `RWDir` 指向 Rain World 安装目录。

```powershell
.\build.ps1          # 编译并将模组打包到 dist/trick_spear/
.\dev.ps1 link       # 把 dist/trick_spear/ 链接到游戏本地 mod 目录下
# 开发期间修改代码后重新运行 build.ps1
.\dev.ps1 unlink     # 开发结束后解除链接
```

### 代码结构

| 目录 | 职责 |
|------|------|
| `Plugin/` | Mod 生命周期与选项 |
| `Core/` | 核心处理 |
| `Input/` | 输入控制 |
| `Logic/` | 主要业务逻辑 |
| `Pose/` | 姿态控制 |
| `Moves/` | 各姿态下的动作招式 |
| `Visual/` | 程序动画 |
| `Presentation/` | 音效与视效 |
| `Combat/` | 战斗交互 |
| `Network/` | 联机处理 |
