/*
 * 文件用途：RealRadio 模组的基础入口类。
 * 说明：
 *   - 通过 [KSPAddon] 特性在 KSP 主菜单启动时初始化模组。
 *   - 继承 MonoBehaviour 并调用 DontDestroyOnLoad，确保场景切换时实例不被销毁。
 *   - 使用单例保护（_initialized 静态标志）防止重复初始化。
 *   - 当前仅作为基础框架，后续可在 Awake/Start 中注册事件、加载配置、初始化网络/音频等模块。
 */

using UnityEngine;

namespace RealRadio
{
    /// <summary>
    /// RealRadio 模组主入口。
    /// </summary>
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class RealRadioMod : MonoBehaviour
    {
        // 静态标志，用于确保整个游戏生命周期内只初始化一次。
        // true 表示已经初始化完成，再次创建时直接销毁自身。
        private static bool _initialized = false;

        /// <summary>
        /// MonoBehaviour 唤醒回调，在对象创建时调用。
        /// </summary>
        private void Awake()
        {
            // 如果已经初始化过，说明是场景切换后重新加载造成的重复实例，直接销毁
            if (_initialized)
            {
                Destroy(this);
                return;
            }

            // 标记为已初始化
            _initialized = true;

            // 使当前 GameObject 在场景切换时不被销毁，保持模组状态
            DontDestroyOnLoad(this);

            // 输出初始化日志，便于在 KSP 日志中确认模组已加载
            Debug.Log("[RealRadio] 模组已初始化，版本 0.1.0.0");
        }

        /// <summary>
        /// MonoBehaviour 启动回调，在第一次 Update 之前调用。
        /// 可在此加载配置、注册事件等。
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
