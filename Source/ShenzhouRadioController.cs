/*
 * 文件用途：神舟飞船无线电播报系统的核心控制器。
 * 说明：
 *   - 在飞行场景中检测当前飞行器是否包含神舟/CZ-2F 关键部件，只需检测一次。
 *   - 若判定为神舟飞船，则初始化事件追踪器与音频播放器。
 *   - 监听逃逸塔分离、一二级分离、整流罩分离事件，并播放对应音频。
 *   - 当飞行器在轨且所有引擎油门 ≤5% 时，判定无线电序列结束，后续不再触发音频。
 *   - 采用模块化设计：VesselEventTracker 负责事件检测，RadioAudioPlayer 负责音频播放。
 */

using System.Collections.Generic;
using UnityEngine;

namespace RealRadio
{
    /// <summary>
    /// 神舟飞船无线电播报控制器。
    /// </summary>
    public class ShenzhouRadioController : MonoBehaviour
    {
        // 判定为神舟飞船所需检测的关键部件名
        // 实际运行时 KSP 中这些部件名使用点号分隔
        private static readonly HashSet<string> ShenzhouPartNames = new HashSet<string>
        {
            "KCHS.SZ.ServiceModule",   // 服务舱
            "KCHS.SZ.OrbitalModule",   // 轨道舱
            "KCHS.SZ.Re.entryModule",  // 返回舱
            "KCLV.CZ2F.stage1",        // 长征二号 F 一级燃料箱
            "KCLV.CZ2F.stage2"         // 长征二号 F 二级燃料箱
        };

        // 音频路径前缀，所有 Shenzhou 无线电包音频都位于此目录下
        // 路径格式为 KSP GameDatabase 路径，相对于 GameData/，不含扩展名
        private const string AudioPathPrefix = "RealRadio/Radio Pack/Shenzhou/";

        // 是否已判定为神舟飞船
        private bool _isShenzhouVessel;

        // 部件检测是否已完成（只检测一次）
        private bool _detectionDone;

        // 无线电序列是否已结束
        private bool _sequenceEnded;

        // 当前飞行器引用
        private Vessel _activeVessel;

        // 音频播放器
        private RadioAudioPlayer _audioPlayer;

        // 事件追踪器
        private VesselEventTracker _eventTracker;

        /// <summary>
        /// MonoBehaviour 启动回调。
        /// 尝试获取当前飞行器并进行一次性神舟部件检测。
        /// </summary>
        private void Start()
        {
            _isShenzhouVessel = false;
            _detectionDone = false;
            _sequenceEnded = false;
            _activeVessel = null;
            _audioPlayer = null;
            _eventTracker = null;

            // 等待 ActiveVessel 有效
            if (FlightGlobals.ActiveVessel == null)
            {
                Debug.Log("[RealRadio] ActiveVessel 尚未就绪，跳过本次初始化");
                return;
            }

            _activeVessel = FlightGlobals.ActiveVessel;

            // 进行一次性的神舟飞船部件检测
            DetectShenzhouVessel();

            if (!_isShenzhouVessel)
            {
                Debug.Log("[RealRadio] 当前飞行器不是神舟飞船，无线电系统不启动");
                return;
            }

            // 初始化音频播放器
            _audioPlayer = gameObject.AddComponent<RadioAudioPlayer>();
            _audioPlayer.Initialize();

            // 初始化事件追踪器并注册回调
            _eventTracker = new VesselEventTracker();
            _eventTracker.Initialize(_activeVessel, OnEventTriggered);

            Debug.Log("[RealRadio] 神舟飞船无线电播报系统已启动");
        }

        /// <summary>
        /// MonoBehaviour 每帧更新回调。
        /// 更新事件追踪状态并检查序列结束条件。
        /// </summary>
        private void Update()
        {
            // 不是神舟飞船，无需处理
            if (!_isShenzhouVessel)
            {
                return;
            }

            // 序列已结束，不再触发新音频，但插件仍保持运行
            if (_sequenceEnded)
            {
                return;
            }

            // 更新部件分离追踪
            _eventTracker?.UpdateTracking();

            // 检查是否满足序列结束条件
            CheckSequenceEnd();
        }

        /// <summary>
        /// 检测当前飞行器是否包含神舟/CZ-2F 关键部件。
        /// 只需检测一次，结果保存在 _isShenzhouVessel 中。
        /// </summary>
        private void DetectShenzhouVessel()
        {
            if (_detectionDone)
            {
                return;
            }

            _detectionDone = true;
            _isShenzhouVessel = false;

            if (_activeVessel == null || _activeVessel.Parts == null)
            {
                return;
            }

            Debug.Log($"[RealRadio] 开始检测飞行器部件，共 {_activeVessel.Parts.Count} 个");

            foreach (Part part in _activeVessel.Parts)
            {
                if (part == null || part.partInfo == null)
                {
                    continue;
                }

                string partName = part.partInfo.name;
                // 归一化部件名：把下划线替换为点号，兼容 cfg 中的下划线写法
                string normalizedName = NormalizePartName(partName);

                Debug.Log($"[RealRadio] 检测到部件：{partName}（归一化：{normalizedName}）");

                if (ShenzhouPartNames.Contains(normalizedName))
                {
                    _isShenzhouVessel = true;
                    Debug.Log($"[RealRadio] 检测到神舟飞船关键部件：{partName}");
                    break;
                }
            }
        }

        /// <summary>
        /// 归一化部件名，用于兼容下划线与点号命名差异。
        /// 例如 "KCHS_SZ_Re_entryModule" 会归一化为 "KCHS.SZ.Re.entryModule"。
        /// </summary>
        /// <param name="partName">原始部件名</param>
        /// <returns>归一化后的部件名</returns>
        private static string NormalizePartName(string partName)
        {
            if (string.IsNullOrEmpty(partName))
            {
                return partName;
            }

            return partName.Replace('_', '.');
        }

        /// <summary>
        /// 事件触发回调，根据事件类型播放对应音频。
        /// </summary>
        /// <param name="eventType">触发的无线电事件类型</param>
        private void OnEventTriggered(RadioEventType eventType)
        {
            // 序列结束后忽略所有新事件
            if (_sequenceEnded)
            {
                return;
            }

            Debug.Log($"[RealRadio] 处理事件：{eventType}");

            switch (eventType)
            {
                case RadioEventType.EscapeTowerJettison:
                    _audioPlayer.PlayOneShot(AudioPathPrefix + "escapetower deculop");
                    break;

                case RadioEventType.BoosterSeparation:
                    _audioPlayer.PlayOneShot(AudioPathPrefix + "booster deculop");
                    break;

                case RadioEventType.StageOneTwoSeparation:
                    // 一二级分离时依次播放两段音频
                    _audioPlayer.PlaySequence(
                        AudioPathPrefix + "stage1 deculoped1",
                        AudioPathPrefix + "stage1 deculoped2");
                    break;

                case RadioEventType.FairingJettison:
                    _audioPlayer.PlayOneShot(AudioPathPrefix + "fairing deculop");
                    break;

                default:
                    Debug.LogWarning($"[RealRadio] 未知事件类型：{eventType}");
                    break;
            }
        }

        /// <summary>
        /// 检查无线电序列是否应结束。
        /// 结束条件：飞行器在轨（ORBITING）且所有引擎油门 ≤5%。
        /// </summary>
        private void CheckSequenceEnd()
        {
            if (_sequenceEnded || _activeVessel == null)
            {
                return;
            }

            // 条件 1：飞行器处于在轨状态
            bool isOrbiting = _activeVessel.situation == Vessel.Situations.ORBITING;
            if (!isOrbiting)
            {
                return;
            }

            // 条件 2：所有引擎油门均 ≤ 5%
            // 若飞行器上没有引擎，则视为满足条件
            bool anyEngineThrottling = false;
            List<ModuleEngines> engines = _activeVessel.FindPartModulesImplementing<ModuleEngines>();
            foreach (ModuleEngines engine in engines)
            {
                if (engine != null && engine.currentThrottle > 0.05f)
                {
                    anyEngineThrottling = true;
                    break;
                }
            }

            if (!anyEngineThrottling)
            {
                _sequenceEnded = true;
                Debug.Log("[RealRadio] 无线电序列结束：飞行器已在轨且无引擎油门大于 5%");
            }
        }
    }
}
