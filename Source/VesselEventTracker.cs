/*
 * 文件用途：追踪神舟/CZ-2F 飞行器上关键部件的分离事件。
 * 说明：
 *   - 通过保存关键 Part 引用，在 Update 中持续检查这些部件是否仍属于当前飞行器。
 *   - 当某类关键部件全部脱离当前飞行器时，触发对应的 RadioEventType 回调。
 *   - 不直接监听 ModuleDecouple，避免依赖 decouple 触发时机，实现更稳定。
 *   - 每个事件只会触发一次，防止重复播报。
 */

using System;
using System.Collections.Generic;
using UnityEngine;

namespace RealRadio
{
    /// <summary>
    /// 可被追踪的无线电事件类型。
    /// </summary>
    public enum RadioEventType
    {
        /// <summary>逃逸塔分离</summary>
        EscapeTowerJettison,

        /// <summary>助推器分离（CZ-2F/CZ-3 助推器）</summary>
        BoosterSeparation,

        /// <summary>一二级分离（通过级间段抛离判定）</summary>
        StageOneTwoSeparation,

        /// <summary>整流罩分离</summary>
        FairingJettison
    }

    /// <summary>
    /// 飞行器关键部件分离事件追踪器。
    /// </summary>
    public class VesselEventTracker
    {
        // 当前要追踪的飞行器
        private Vessel _vessel;

        // 事件触发回调
        private Action<RadioEventType> _onEvent;

        // 按事件类型分组保存的部件引用
        // Key: RadioEventType 枚举名称字符串
        // Value: 该事件对应的所有关键部件实例列表
        private Dictionary<string, List<Part>> _trackedParts;

        // 已触发过的事件集合，避免重复触发
        private HashSet<RadioEventType> _triggeredEvents;

        /// <summary>
        /// 初始化追踪器。
        /// </summary>
        /// <param name="vessel">要追踪的飞行器</param>
        /// <param name="onEvent">分离事件触发时的回调</param>
        public void Initialize(Vessel vessel, Action<RadioEventType> onEvent)
        {
            _vessel = vessel ?? throw new ArgumentNullException(nameof(vessel));
            _onEvent = onEvent ?? throw new ArgumentNullException(nameof(onEvent));

            _trackedParts = new Dictionary<string, List<Part>>();
            _triggeredEvents = new HashSet<RadioEventType>();

            // 注册各类事件需要追踪的部件名
            // 实际运行时 KSP 中这些部件名使用点号分隔
            TrackPartsForEvent(RadioEventType.EscapeTowerJettison,
                "KCLV.CZ2F.Escape.tower",
                "KCLV.CZ2F.Escape.tower.Upper");

            // CZ-3B 助推器燃料箱包含 CZ-2F 样式变体，cfg 中 name = KCLV_CZ3_BOOSTER_TANK
            TrackPartsForEvent(RadioEventType.BoosterSeparation,
                "KCLV.CZ3.BOOSTER.TANK");

            TrackPartsForEvent(RadioEventType.StageOneTwoSeparation,
                "KCLV.CZ2F.interstage");

            TrackPartsForEvent(RadioEventType.FairingJettison,
                "KCLV.CZ2F.Fairing.Upper",
                "KCLV.CZ2F.Fairing.Mid");
        }

        /// <summary>
        /// 为指定事件类型注册需要追踪的部件。
        /// 只在初始化时调用一次。
        /// </summary>
        /// <param name="eventType">事件类型</param>
        /// <param name="partNames">部件名列表</param>
        private void TrackPartsForEvent(RadioEventType eventType, params string[] partNames)
        {
            string key = eventType.ToString();
            List<Part> parts = new List<Part>();

            foreach (string partName in partNames)
            {
                // 归一化目标部件名，兼容点号与下划线差异
                string normalizedTargetName = NormalizePartName(partName);

                // 在当前飞行器中查找所有匹配的部件
                foreach (Part part in _vessel.Parts)
                {
                    if (part != null && part.partInfo != null)
                    {
                        string normalizedActualName = NormalizePartName(part.partInfo.name);
                        if (normalizedActualName == normalizedTargetName)
                        {
                            parts.Add(part);
                        }
                    }
                }
            }

            if (parts.Count > 0)
            {
                _trackedParts[key] = parts;
                Debug.Log($"[RealRadio] 事件 {key} 已追踪 {parts.Count} 个部件");
            }
        }

        /// <summary>
        /// 归一化部件名，把下划线替换为点号，兼容不同命名风格。
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
        /// 每帧更新追踪状态，应在 MonoBehaviour.Update 中调用。
        /// </summary>
        public void UpdateTracking()
        {
            if (_vessel == null || _trackedParts == null || _trackedParts.Count == 0)
            {
                return;
            }

            foreach (var kvp in _trackedParts)
            {
                // 解析事件类型
                if (!Enum.TryParse<RadioEventType>(kvp.Key, out RadioEventType eventType))
                {
                    continue;
                }

                // 已触发过的事件不再检查
                if (_triggeredEvents.Contains(eventType))
                {
                    continue;
                }

                List<Part> parts = kvp.Value;

                // 如果该事件没有追踪到任何部件，则跳过
                if (parts == null || parts.Count == 0)
                {
                    continue;
                }

                // 检查是否还有至少一个部件仍属于当前飞行器
                bool anyAttached = false;
                for (int i = 0; i < parts.Count; i++)
                {
                    Part part = parts[i];
                    if (part != null && part.vessel == _vessel)
                    {
                        anyAttached = true;
                        break;
                    }
                }

                // 所有关键部件都已脱离当前飞行器，判定事件发生
                if (!anyAttached)
                {
                    _triggeredEvents.Add(eventType);
                    Debug.Log($"[RealRadio] 事件触发：{eventType}");
                    _onEvent?.Invoke(eventType);
                }
            }
        }
    }
}
