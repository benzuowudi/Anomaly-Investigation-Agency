# Legacy 归档说明

本目录保存 UI 架构调整过程中被替换掉的旧实现与资源，用于回溯参考。

- 位于 `Assets` 之外，**不参与 Unity 编译**。
- 每个文件都连同 `.meta` 一起移动/复制，GUID 不变；需要恢复时按原路径复制回 `Assets` 即可。
- 确认不再需要后，可整体删除本目录（`_acl_report` 是排查 Windows 权限问题时生成的诊断输出，也可删除）。

## 当前架构（2026-10 之后）

**UGUI 预制体窗口 + FairyGUI 内容**：

- `UIWindow : MonoBehaviour` 是窗口本体，预制体放在 `Assets/Res/Prefab`，由 `UIManager` 实例化后挂到 DontDestroy 下 Canvas 的 `m_BLayer / m_MLayer / m_TLayer`，隐藏时回收到 `m_Pool`；窗口全部由 `UIManager` 统一管理（不走 FairyGUI 的 Window / GRoot）。
- 预制体里的 FairyGUI `UIPanel` 负责渲染 GComponent 内容，`UIWindow.m_Content` 就是它；继承 `GComponent` 的界面类通过 `UIManager.RegisterContent<T>(包, 组件)` 注册后即可挂进对应窗口。
- FairyGUI 包走 Addressables：`<包名>_fui` + `<包名>_atlas0[_n]`，由 `FairyGUIManager` 按需加载；`UIManager.EnsureLoaded` 在实例化预制体前先把包加载好。
- 界面名 = 预制体 Addressables 地址（默认同名），常量集中在 `UIDefine`，不再有 UI 配置表。

## 已恢复到 Assets 的条目（Legacy 里保留了同 GUID 副本）

| 条目 | 说明 |
| --- | --- |
| `Assets/HotUpdate/Framework/Script/UI/UIWindow.cs`、`UIAnim.cs`、`Anim_*.cs` | UGUI 窗口基类与入场动画，已按混合架构扩展 `m_Content` |
| `Assets/HotUpdate/Framework/Script/Manager/UIManager.cs` | 预制体窗口 + 对象池 + 按需加载，已改为字符串界面名 |
| `Assets/HotUpdate/GameMain/Script/Game/Ground/UI/BackGroundUI.cs`、`ForeGroundUI.cs` | 两个混合预制体上的窗口脚本 |
| `Assets/Res/Prefab/BackGroundView.prefab` 等 4 个 | 已重新注册进 Addressables（Default Local Group） |

## 仍然只存在于 Legacy 的条目

| Legacy 路径 | 原路径 | 说明 |
| --- | --- | --- |
| `Assets/HotUpdate/GameMain/Script/Game/**/UI`、`**/Factory`、`ComponentUI/` 其余脚本 | 同名 | 旧 UGUI 业务窗口与工厂（CommandUI、RoleBaseUI、LotteryUI 等），原预制体在旧的 AB 包里，需要重新做预制体后再恢复 |
| `Assets/HotUpdate/Framework/Script/UI/IUIFactory.cs`、`DonotGetChild.cs` | 同名 | 旧的组件注入工厂与节点标记；当前框架改为自动补 `UIWindow`，不再需要 |
| `Assets/Editor/SetAssetsBundleName.cs` | 同名 | 旧的 AssetBundle 命名菜单（`.ui` / `.unity3d` / `.data` / `.material` / `.code` 后缀的来源） |
| `Assets/Editor/AddressablesMigration.cs` | 同名 | 旧的「AssetBundle 配置 → Addressables」迁移工具 |
| `Assets/StreamingAssets/AssetBundles/` | 同名 | 旧的 AB 构建产物（`commandroom.ui`、`background.unity3d` 等） |
| `Assets/Res/ComponentUI/` | 同名 | 旧 UGUI 组件预制体（Item、RoleAttribe_*、RoleHead） |
| `Assets/Res/Art/Texture/UI/` | 同名 | 旧 UGUI 界面美术，按 `.ui` 包分目录 |
| `Assets/HotUpdate/GameMain/Script/DataTable/GameData_UI/`、`GameType/UINameType.cs`、`UIType.cs`、`AnimType/UIEnterAnimType.cs` | 同名 | `ui` 配置表生成代码与 UI 枚举 |
| `DataTable/ui.xlsx` | 同名 | `ui` 配置表源文件（`__tables__.xlsx` 的 `GameData_UI.ui` 行、`__enums__.xlsx` 的 UI 枚举已同步移除） |

## 旧 `.ui` 后缀

只是 AssetBundle 名字的后缀（分类标记），不是文件扩展名，已随旧管线废弃。
