# Catdoku

猫咪主题的彩色 N 皇后消除解谜游戏（1500 关 + 关卡编辑器），Unity 工程。

- 玩法：在 N×N 棋盘上放置 N 只猫，满足每行/每列/每色块各 1 只，且猫不能相邻（含斜对角）
- 关卡：`Assets/Resources/Levels/` 下 `level_1.json` ~ `level_1500.json` + `Tutorial.json`
- 编辑器：Unity 菜单自带关卡编辑器，可导出 JSON
- 工程版本：Unity 6000.0.30f1（原模板为 6000.3.16f1）
- 广告：Google Mobile Ads + Unity Ads（激励视频）

## 构建

用 Unity 打开工程后，`Assets/Editor/AndroidBuild.cs` 提供批处理构建入口：

```
Unity.exe -batchmode -nographics -projectPath <工程目录> -buildTarget Android -executeMethod AndroidBuild.Build
```
