# 花枪 Trick Spear

花式舞矛，灵感来自于 Trick Saber。

![preview01](static/preview01.gif)

[Steam 创意工坊](https://steamcommunity.com/sharedfiles/filedetails/?id=3759869025)

## 构建

需要在 `lib/` 中补充依赖文件: 来自 [Improved Input Config](https://github.com/zombieseatflesh7/improved-input-config) 的 `ImprovedInput.dll` 和 `ImprovedInput.xml`。

## 代码结构

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
