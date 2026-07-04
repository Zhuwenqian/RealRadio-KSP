/*
 * 文件用途：RealRadio 模组的飞行场景入口类。
 * 说明：
 *   - 通过 [KSPAddon] 特性在 KSP 进入飞行场景（Flight）时初始化模组。
 *   - 每次进入飞行场景都会创建新的实例，退出场景时由 Unity 自动销毁，
 *     保证每次飞行的无线电播报状态相互独立。
 *   - 负责创建并挂载 ShenzhouRadioController，启动神舟飞船无线电播报系统。
 *   - 本身不承载业务逻辑，仅作为场景入口与控制器容器。
 */

using UnityEngine;

namespace RealRadio
{
    /// <summary>
    /// RealRadio 模组飞行场景主入口。
    /// </summary>
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class RealRadioMod : MonoBehaviour
    {
        /// <summary>
        /// MonoBehaviour 唤醒回调，在对象创建时调用。
        /// 在此处挂载神舟无线电播报控制器。
        /// </summary>
        private void Awake()
        {
            Debug.Log("[RealRadio] 飞行场景模组已初始化，版本 0.2.0.0");

            // 挂载神舟飞船无线电播报控制器
            // 该控制器负责部件检测、事件监听、音频播放等全部业务逻辑
            gameObject.AddComponent<ShenzhouRadioController>();
        }

        /// <summary>
        /// MonoBehaviour 启动回调，在第一次 Update 之前调用。
        /// </summary>
        private void Start()
        {
            Debug.Log("[RealRadio] Start 调用完成");
        }

        /// <summary>
        /// MonoBehaviour 销毁回调，用于清理资源。
        /// </summary>
        private void OnDestroy()
        {
            Debug.Log("[RealRadio] OnDestroy 调用");
        }
    }
}
