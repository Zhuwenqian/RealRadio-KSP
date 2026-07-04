# RealRadio 神舟飞船无线电播报系统实施计划

## 一、概要

在 `RealRadio` 模组中新增一套针对神舟飞船（Shenzhou）+ 长征二号 F（CZ-2F）组合体的真实无线电播报系统。

核心功能：

1. 进入 KSP 飞行场景后，自动检测当前飞行器是否包含神舟/CZ-2F 关键部件，仅需检测一次。
2. 若判定为神舟飞船，则启用 Shenzhou 无线电包，监听下列分离事件并播放对应音频：
   - 逃逸塔分离 → `escapetower deculop.ogg`
   - 助推器分离 → 本次跳过（cfg 未找到）
   - 一二级分离 → 依次播放 `stage1 deculoped1.ogg`、`stage1 deculoped2.ogg`
   - 整流罩分离 → `fairing deculop.ogg`
3. 当飞行器处于在轨状态且所有引擎油门均 ≤5% 时，判定无线电序列结束，后续不再触发新的音频。

## 二、现状分析

- `Source/RealRadioMod.cs` 是当前唯一插件源码，使用 `[KSPAddon(Startup.MainMenu, true)]` 在主菜单初始化，内部没有任何业务逻辑。
- `RealRadio.csproj` 仅引用了 `Assembly-CSharp`、`Assembly-CSharp-firstpass`、`KSPAssets`、`UnityEngine` 等核心 DLL，并自动将编译产物复制到 `GameData/RealRadio/Plugins/`。
- 已存在 5 个 `.ogg` 音频文件位于 `GameData/RealRadio/Radio Pack/Shenzhou/`，但没有任何代码加载或触发它们。
- KIU 部件包提供了完整的长征二号 F 与神舟飞船部件配置，部件名（`name = ...`）是 C# 端识别的关键依据。
- 目前没有配置文件读取逻辑，本次功能采用硬编码部件名 + 硬编码音频路径的方式实现，保持简单。

## 三、待确认决策（已确认）

| 问题 | 用户选择 | 影响 |
|------|----------|------|
| 插件初始化场景 | 改为飞行场景入口 | 将 `[KSPAddon(Startup.MainMenu, true)]` 改为 `[KSPAddon(Startup.Flight, false)]` |
| 助推器分离音频 | 跳过，待找到 cfg 再实现 | 本次不实现 `booster deculop.ogg` 触发逻辑 |
| 序列结束行为 | 停止后续事件音频并进入空闲 | 设置 `_sequenceEnded` 标志，后续事件不再播放音频，但插件仍运行 |

## 四、拟修改/新增文件

### 4.1 `Source/RealRadioMod.cs`（修改）

- 将 `[KSPAddon(Startup.MainMenu, true)]` 改为 `[KSPAddon(Startup.Flight, false)]`。
- 移除 `DontDestroyOnLoad` 与全局单例 `_initialized` 逻辑（飞行场景每次进入都会新建实例）。
- 在 `Awake()` 或 `Start()` 中创建并挂载 `ShenzhouRadioController`。
- 顶部注释更新为：飞行场景入口，负责初始化并启动神舟无线电播报控制器。

### 4.2 `Source/ShenzhouRadioController.cs`（新增）

职责：神舟无线电播报系统的总控制器，管理状态机与事件分发。

主要成员：

- `private bool _isShenzhouVessel`：是否判定为神舟飞船。
- `private bool _detectionDone`：部件检测是否已完成（只检测一次）。
- `private bool _sequenceEnded`：无线电序列是否已结束。
- `private Vessel _activeVessel`：当前飞行器引用。
- `private RadioAudioPlayer _audioPlayer`：音频播放器实例。
- `private VesselEventTracker _eventTracker`：事件追踪器实例。

主要方法：

- `private void Start()`：
  - 等待 `FlightGlobals.ActiveVessel` 有效。
  - 调用 `DetectShenzhouVessel()` 进行一次部件检测。
  - 若为神舟飞船，初始化 `_audioPlayer`、`_eventTracker` 并注册事件回调。
- `private void Update()`：
  - 若未判定为神舟或序列已结束，直接返回。
  - 每帧调用 `_eventTracker.UpdateTracking()` 更新部件状态。
  - 调用 `CheckSequenceEnd()` 判断序列结束条件。
- `private void DetectShenzhouVessel()`：
  - 遍历 `FlightGlobals.ActiveVessel.Parts`。
  - 检查 `part.partInfo.name` 是否在以下列表中：
    - `KCHS_SZ_ServiceModule`
    - `KCHS_SZ_OrbitalModule`
    - `KCHS_SZ_Re_entryModule`
    - `KCLV_CZ2F_stage1`
    - `KCLV_CZ2F_stage2`
  - 任一匹配即判定为神舟飞船，记录日志。
- `private void CheckSequenceEnd()`：
  - 条件：`vessel.situation == Vessel.Situations.ORBITING`。
  - 且：遍历 `vessel.FindPartModulesImplementing<ModuleEngines>()`，所有引擎的 `currentThrottle <= 0.05f`（考虑无引擎情况视为满足）。
  - 满足时设置 `_sequenceEnded = true` 并输出日志。
- `private void OnEventTriggered(RadioEventType eventType)`：
  - 根据事件类型调用 `_audioPlayer` 播放对应音频或音频队列。
  - 若 `_sequenceEnded == true` 直接忽略。

### 4.3 `Source/RadioAudioPlayer.cs`（新增）

职责：加载并播放 `GameData/RealRadio/Radio Pack/Shenzhou/` 下的 `.ogg` 音频文件。

主要成员：

- `private AudioSource _audioSource`：Unity 音频源，挂载到控制器 GameObject。
- `private Dictionary<string, AudioClip> _clipCache`：已加载音频缓存，避免重复加载。
- `private Queue<AudioClip> _sequenceQueue`：用于一二级分离的两段连播队列。
- `private bool _isPlayingSequence`：是否正在播放队列。

主要方法：

- `public void Initialize()`：
  - 在当前 GameObject 上创建 `AudioSource` 组件。
  - 设置 `playOnAwake = false`。
- `public void PlayOneShot(string clipPath)`：
  - 通过 `GameDatabase.Instance.GetAudioClip(clipPath)` 获取 `AudioClip`。
  - 缓存后调用 `_audioSource.PlayOneShot(clip)`。
- `public void PlaySequence(params string[] clipPaths)`：
  - 将所有音频加入 `_sequenceQueue`。
  - 开始协程 `PlaySequenceCoroutine()` 依次播放。
- `private IEnumerator PlaySequenceCoroutine()`：
  - 从队列中取出一个 `AudioClip` 播放。
  - 等待 `clip.length` 秒后继续播放下一个。
  - 队列为空时设置 `_isPlayingSequence = false`。

音频路径约定（KSP `GameDatabase` 格式，相对于 `GameData/`）：

- `RealRadio/Radio Pack/Shenzhou/escapetower deculop`
- `RealRadio/Radio Pack/Shenzhou/stage1 deculoped1`
- `RealRadio/Radio Pack/Shenzhou/stage1 deculoped2`
- `RealRadio/Radio Pack/Shenzhou/fairing deculop`

### 4.4 `Source/VesselEventTracker.cs`（新增）

职责：追踪指定部件是否从当前飞行器上分离，并在首次分离时触发回调。

主要成员：

- `private Vessel _vessel`：当前飞行器。
- `private Dictionary<string, List<Part>> _trackedParts`：按事件类型分组保存的部件引用。
- `private HashSet<RadioEventType> _triggeredEvents`：已触发过的事件，防止重复触发。
- `private Action<RadioEventType> _onEvent`：事件触发回调。

枚举 `RadioEventType`：

- `EscapeTowerJettison`
- `StageOneTwoSeparation`
- `FairingJettison`

主要方法：

- `public void Initialize(Vessel vessel, Action<RadioEventType> onEvent)`：
  - 保存飞行器与回调。
  - 初始化 `_trackedParts`：
    - `EscapeTowerJettison`：`KCLV_CZ2F_Escape_tower`、`KCLV_CZ2F_Escape_tower_Upper`
    - `StageOneTwoSeparation`：`KCLV_CZ2F_interstage`
    - `FairingJettison`：`KCLV_CZ2F_Fairing_Upper`、`KCLV_CZ2F_Fairing_Mid`
- `public void UpdateTracking()`：
  - 对每种事件类型，检查对应部件是否仍属于 `_vessel`。
  - 若之前存在而现在不存在（`part == null || part.vessel != _vessel`），且尚未触发，则调用 `_onEvent(eventType)` 并标记已触发。

说明：

- 不直接监听 `ModuleDecouple` 是为了避免依赖具体 decouple 触发时机；通过引用失效判断分离，实现更简单稳定。
- 由于部件被物理分离后原 `Part` 对象可能变为独立 vessel 或被销毁，`part.vessel != _vessel` 是可靠判据。

### 4.5 `RealRadio.csproj`（修改）

- 在 `<ItemGroup>` 的 `<Compile>` 节点中新增：
  - `Source/ShenzhouRadioController.cs`
  - `Source/RadioAudioPlayer.cs`
  - `Source/VesselEventTracker.cs`

### 4.6 `readme/功能更新.md`（修改）

- 新增 2026-07-04 条目：
  - 实现神舟飞船无线电播报系统。
  - 飞行场景初始化并一次性检测神舟部件。
  - 支持逃逸塔、一二级、整流罩分离音频触发。
  - 在轨无油门时结束无线电序列。
  - 新增 `ShenzhouRadioController`、`RadioAudioPlayer`、`VesselEventTracker` 三个模块。

## 五、实现原理

1. **场景入口**：利用 KSP 的 `[KSPAddon(Startup.Flight, false)]` 特性，每次进入飞行场景自动创建插件实例；退出场景时 Unity 自动销毁，保证每次飞行状态独立。
2. **部件识别**：通过 `part.partInfo.name` 匹配 KIU 部件包中定义的部件名，识别神舟飞船特征模块。
3. **事件检测**：采用“引用追踪”方式，在 `Update` 中持续检查关键部件是否仍属于当前飞行器；一旦某类关键部件全部脱离，即判定对应分离事件发生。
4. **音频播放**：利用 KSP 已加载到 `GameDatabase` 的 `AudioClip`，通过 Unity `AudioSource` 播放；一二级分离使用协程实现两段音频顺序播放。
5. **序列结束**：结合 `Vessel.Situations.ORBITING` 与所有 `ModuleEngines.currentThrottle` 阈值判断，防止在滑行阶段继续播报无意义事件。

## 六、验证步骤

1. 编译 `RealRadio.sln`，确认无错误，`RealRadio.dll` 自动复制到 `GameData/RealRadio/Plugins/`。
2. 在 KSP 中新建一艘包含 `KCHS_SZ_ServiceModule`（或返回舱/轨道舱）+ `KCLV_CZ2F_stage1` + `KCLV_CZ2F_stage2` 的飞行器并发射。
3. 查看 KSP 日志（`KSP.log` / `output_log.txt`），确认出现 `[RealRadio] 检测到神舟飞船` 日志。
4. 手动或自动触发逃逸塔分离，确认日志输出并听到 `escapetower deculop.ogg`。
5. 触发一二级分离（抛离 `KCLV_CZ2F_interstage`），确认依次听到 `stage1 deculoped1.ogg` 与 `stage1 deculoped2.ogg`。
6. 触发整流罩分离，确认听到 `fairing deculop.ogg`。
7. 将飞船送入轨道并关闭所有引擎油门，确认日志输出 `[RealRadio] 无线电序列结束`；再次触发分离事件时无音频播放。
8. 发射一艘不含任何神舟/CZ-2F 部件的普通火箭，确认没有 `[RealRadio] 检测到神舟飞船` 日志，也不触发任何音频。

## 七、风险与回退

- `GameDatabase.GetAudioClip` 对带空格路径的支持需要实测；若加载失败，改用 `KSP.IO` 或 `UnityEngine.Networking.UnityWebRequest` 读取文件系统。
- 若 KIU 部件名在未来版本变更，只需更新 `VesselEventTracker` 与 `ShenzhouRadioController` 中的字符串常量。
- 助推器分离逻辑本次未实现，属于已知缺口，后续补充 cfg 后再扩展 `RadioEventType.BoosterSeparation`。
