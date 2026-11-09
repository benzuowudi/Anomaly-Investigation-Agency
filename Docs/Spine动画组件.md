# 通用 Spine 动画组件

> 在 FairyGUI 界面里挂一个 Spine 角色并播放动画，做成一套**可复用**的组件：
> 换角色只换资源地址，换动画格式（以后接 DragonBones）只加一个实现类。
>
> 对应 `ui://Base/RoleAnimCom`（组件里只有一个 0,0 的 `graph` 挂点 `GameObjectGo`）。

**当前状态：已接入官方 spine-unity 运行时，跑通并实战验证过**（主菜单里挂角色出图，见文末截图）。

---

## 1. 文件清单

| 文件 | 作用 |
| --- | --- |
| `Assets/HotUpdate/Framework/Script/UI/Anim/UIAnimDefine.cs` | 自适应方式 `UIAnimFit`、对齐点 `UIAnimAnchor` |
| `Assets/HotUpdate/Framework/Script/UI/Anim/IUIAnimPlayer.cs` | 播放器抽象（纯 Unity 类型，不依赖第三方动画库） |
| `Assets/HotUpdate/Framework/Script/UI/Anim/SpineAnimCom.cs` | **通用 UI 动画组件**（`GComponent`）：挂载、排序、裁剪、alpha、自适应、加载释放 |
| `Assets/HotUpdate/Framework/Script/UI/Anim/SpineUIAnimPlayer.cs` | Spine 实现（`SkeletonAnimation`），所有 spine API 集中在这一个文件 |
| `Assets/HotUpdate/GameMain/Script/Game/ComponentUI/RoleAnimCom.cs` | 角色便捷封装：动作名常量、默认皮肤、待机兜底、`PlayOnce` |
| `Assets/HotUpdate/GameMain/Script/Game/ComponentUI/BaseData.cs` | 注册 `ui://Base/RoleAnimCom` → `RoleAnimCom` |
| `Assets/HotUpdate/HotUpdate.asmdef` | 引用 `spine-unity` / `spine-csharp` |

运行时：`Assets/Spine`（`spine-csharp` / `spine-unity` / `spine-unity-editor` 三个程序集）。

---

## 2. 资源现状（role1 / chi）

```
Assets/Res/Animator/Role/role1/
├─ chi.skel.bytes           Spine 4.1.08 二进制骨架（339 骨骼 / 152 槽位 / 7 皮肤 / 15 动画）
├─ chi.atlas.txt            atlas，单页 2048×1024
├─ chi.png                  图集贴图
├─ chi_Material.mat         spine-unity 自动生成的材质（Spine/Skeleton）
├─ chi_Atlas.asset          SpineAtlasAsset
└─ chi_SkeletonData.asset   SkeletonDataAsset（Addressables 地址：role1）
```

- **`SkeletonDataAsset.scale = 0.01`**：骨架本地单位是 1/100，组件用 `IUIAnimPlayer.unitScale`（=100）换算回 UI 像素。
- **Addressables**：`chi_SkeletonData.asset` 已进 `Role` 组，地址 `role1`；图集/材质/贴图作为依赖自动打进同一个包。

### 动画名（15 个）

`readystand`（待机）、`relaxstand`、`funnystand`、`attack`、`skill`、`power`、
`dizziness`、`fall`、`death`、`win`、`walk`、`run`、`float1` / `float2` / `float3`

常量都写在 `RoleAnimCom` 里，直接 `Play(RoleAnimCom.c_Attack)`。

### 皮肤（换装）

| 皮肤 | 情况 |
| --- | --- |
| `default` | **只有头、手、影子等公共部件**（直接用它只会看到一颗头） |
| `01` / `02` / `03` | 完整角色（金/绿配色差异），`01` 是默认 |
| `04` | 完整角色，但有一块白色破图 |
| `05` / `06` | 白色破图严重，基本不可用 |

所以 `RoleAnimCom.SetRole()` 默认会套上 `c_DefaultSkin = "01"`；`04~06` 需要美术回头检查导出。

---

## 3. 用法

```csharp
// 角色详情页：com_role_anim 是 FairyGUI 里的 RoleAnimCom
com_role_anim.SetRole("role1");                      // 加载骨架 + 皮肤 01 + 播 readystand
com_role_anim.PlayOnce(RoleAnimCom.c_Attack);        // 播一次，可传回调
com_role_anim.SetDress("02");                        // 换装
com_role_anim.SetFlipX(true);                        // 朝左
```

### 组件 API（`SpineAnimCom`）

| 方法 / 属性 | 说明 |
| --- | --- |
| `SetSpine(address, defaultAnim = null, loop = true)` | 加载并挂上；同一地址重复调用不会重建 |
| `SetRole(address, idle = null, skin = "01")` | 角色版：加皮肤 + 待机兜底（`RoleAnimCom`） |
| `Play(anim, loop = true, speed = 1f, track = 0)` | 播放 |
| `Stop()` / `Pause()` / `Resume()` / `SetTimeScale(float)` | 播放控制 |
| `SetSkin(skin)` / `HasSkin(skin)` / `SetDress(skin)` | 皮肤（换装） |
| `SetFlipX(bool)` | 水平翻转 |
| `HasAnimation(name)` / `GetAnimations()` | 查动作 |
| `SetFit(fit, width = 0, height = 0, anchor)` | 自适应缩放 + 对齐 |
| `SetScale(f)` / `SetOffset(x, y)` / `Refit()` | 额外缩放 / 像素微调 / 尺寸变了重算 |
| `Clear()` | 卸掉当前动画对象（挂点保留） |
| `player` | `IUIAnimPlayer`，需要轨道等高级操作时用 |
| `onComplete` / `onEvent` | 动画播完 / Spine 帧事件 |

### 对齐与自适应

`GameObjectGo` 是 **0×0 挂点**，它的位置就是角色的**脚底站位点**（默认 `BottomCenter`）。

- 默认 `UIAnimFit.None`：**1 Spine 单位 = 1 UI 像素**，尺寸与美术导出一致。
- 自动铺满：`SetFit(UIAnimFit.FitHeight, 0, 620)` → 实测把 3.84 单位的角色精确缩放到 **620 px**。
- 挂点放在界面靠上位置时角色会向**上**生长，注意别把脚点放在贴顶的位置（实测踩过这个坑）。

---

## 4. 实现要点（含实测踩坑）

1. **挂接方式**：`GGraph.SetNativeObject(GoWrapper)`。GoWrapper 代理排序（`renderingOrder` → `Renderer.sortingOrder`）、
   裁剪、层级（跟着 FairyGUI 的 UI 层走）。
2. **挂完必须 `CacheRenderers()`**：GoWrapper 只在 `SetWrapTarget` 时收集渲染器，Spine 对象是挂载之后才有的。
3. **材质克隆**：`SetWrapTarget(holder, cloneMaterial: true)`，否则 GoWrapper 会把共享材质的 `renderQueue` 改成 3000，污染其它界面。
4. **`unitScale` 换算**：`scale = 0.01` 时先 ×100 再算自适应。**注意不能乘两次**（第一版就是这里算错，`localScale` 变成 16152）。
5. **包围盒取 `mesh.bounds`，不要读 `mesh.vertices`**：spine-unity 走的是 Mesh 顶点缓冲上传，
   CPU 侧 `vertices` 读回来是 **NaN**（踩过）；`mesh.bounds` 有效。
6. **alpha 跟随**：`GoWrapper.onUpdate` 里把 `context.alpha * wrapper.alpha` 写进 `Skeleton.A`，否则界面淡入淡出时 Spine 不跟着变。
7. **释放**：`Dispose()` 先停播放器，再由 `GObject.Dispose` 统一 Dispose 掉 GoWrapper（连带销毁 holder 与 Spine 对象）。

### 如果以后导入新骨架出现「看不见」

先查 `chi_SkeletonData.asset` 的 **`atlasAssets` 是不是空的**：本次导入时 spine-unity 只生成了
`chi_Atlas.asset` / `chi_Material.mat`，没有自动挂到 `SkeletonDataAsset` 上，表现为材质变成
`Spine/Special/HiddenPass` 兜底、画面全空。手动把 `chi_Atlas` 拖进 `Atlas Assets` 即可（本工程已修好并保存）。

---

## 5. 实测验证记录

- `Assets/Spine` 三个程序集编译通过；`HotUpdate` 引用后 **0 error**。
- Addressables：`role1` → `SkeletonDataAsset` 解析成功（1 个 location）。
- 骨架解析：version 4.1.08、339 骨骼、152 槽位、7 皮肤、15 动画、`scale=0.01`。
- 皮肤对照渲染：`default` 只有头/手/影子，`01~03` 完整，`04~06` 有白色破图。
- 组件链路（Play 模式，真实界面里）：
  - `UIPackage.CreateObject("Base","RoleAnimCom")` → 类型正确（注册生效）
  - `SetRole("role1")` → 加载成功、皮肤 01 生效、15 个动画可查
  - 布局：`FitHeight 620` → 实测 `localScale = 161.53`、网格 3.84×161.53 = **620.0 px**
  - 渲染器：layer 5（UI 层）、材质 `Spine/Skeleton`、`sortingOrder` 由 GoWrapper 接管
  - 主菜单实拍：角色正确显示在界面里，缩放/站位/透明都对

截图：`Assets/Screenshots/spine_com_ingame.png`（已 gitignore）

---

## 6. 限制 / 后续

1. **世界（非 UI）复用**：`SpineUIAnimPlayer.Create(SkeletonDataAsset)` 本身就是普通 MonoBehaviour，挂到场景里即可。
2. **同屏多个角色**：现在每个组件一个播放器，直接支持；如果要列表里滚动复用，建议加一个播放器缓存池。
3. **纯 ScreenSpaceOverlay**：Spine 是真实 MeshRenderer，Overlay 画布不渲染它（FairyGUI 自带 `GLoader3D` 同样限制）。
   本工程的窗口是 ScreenSpaceCamera + UIPanel，没问题。
4. **`04~06` 皮肤破图**属于导出资源问题，组件侧无法修正。
5. 进战斗/探索界面后如果要按状态切动作（攻击/受击/死亡），在对应 View 里调 `Play(常量)` + `onComplete` 即可。
