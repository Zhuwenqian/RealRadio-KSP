/*
 * 文件用途：加载并播放 RealRadio 的无线电音频资源。
 * 说明：
 *   - 通过 KSP 的 GameDatabase 加载位于 GameData/RealRadio/Radio Pack/Shenzhou/ 下的 .ogg 音频。
 *   - 支持单次播放与顺序播放两种模式；一二级分离需要两段音频连续播放。
 *   - 使用 Unity AudioSource 播放，音频随场景销毁自动释放。
 *   - 对加载失败的音频输出日志，便于排查路径或格式问题。
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RealRadio
{
    /// <summary>
    /// 无线电音频播放器。
    /// </summary>
    public class RadioAudioPlayer : MonoBehaviour
    {
        // Unity 音频源组件，用于播放 AudioClip
        private AudioSource _audioSource;

        // 音频缓存，避免重复从 GameDatabase 加载同一个 AudioClip
        // Key: GameDatabase 中的路径（如 "RealRadio/Radio Pack/Shenzhou/escapetower deculop"）
        private Dictionary<string, AudioClip> _clipCache;

        // 顺序播放队列，用于一二级分离的两段连播
        private Queue<AudioClip> _sequenceQueue;

        // 是否正在播放顺序队列
        private bool _isPlayingSequence;

        /// <summary>
        /// 初始化播放器，创建 AudioSource 并准备缓存。
        /// </summary>
        public void Initialize()
        {
            _clipCache = new Dictionary<string, AudioClip>();
            _sequenceQueue = new Queue<AudioClip>();
            _isPlayingSequence = false;

            // 在当前 GameObject 上创建 AudioSource
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 0f; // 2D 音效，不受空间位置影响
            _audioSource.volume = 1f;

            Debug.Log("[RealRadio] RadioAudioPlayer 初始化完成");
        }

        /// <summary>
        /// 播放单个音频。
        /// </summary>
        /// <param name="clipPath">GameDatabase 中的音频路径，相对于 GameData/</param>
        public void PlayOneShot(string clipPath)
        {
            AudioClip clip = LoadClip(clipPath);
            if (clip == null)
            {
                Debug.LogWarning($"[RealRadio] 无法播放音频：{clipPath}");
                return;
            }

            _audioSource.PlayOneShot(clip);
            Debug.Log($"[RealRadio] 播放音频：{clipPath}");
        }

        /// <summary>
        /// 顺序播放多个音频，每个音频播放完毕后自动播放下一个。
        /// </summary>
        /// <param name="clipPaths">GameDatabase 中的音频路径数组</param>
        public void PlaySequence(params string[] clipPaths)
        {
            if (clipPaths == null || clipPaths.Length == 0)
            {
                return;
            }

            foreach (string clipPath in clipPaths)
            {
                AudioClip clip = LoadClip(clipPath);
                if (clip != null)
                {
                    _sequenceQueue.Enqueue(clip);
                }
                else
                {
                    Debug.LogWarning($"[RealRadio] 顺序播放跳过缺失音频：{clipPath}");
                }
            }

            if (_sequenceQueue.Count > 0 && !_isPlayingSequence)
            {
                StartCoroutine(PlaySequenceCoroutine());
            }
        }

        /// <summary>
        /// 从 GameDatabase 加载 AudioClip，并使用缓存避免重复加载。
        /// </summary>
        /// <param name="clipPath">GameDatabase 中的音频路径</param>
        /// <returns>加载成功的 AudioClip，失败返回 null</returns>
        private AudioClip LoadClip(string clipPath)
        {
            if (string.IsNullOrEmpty(clipPath))
            {
                return null;
            }

            // 先从缓存查找
            if (_clipCache.TryGetValue(clipPath, out AudioClip cachedClip))
            {
                return cachedClip;
            }

            // 通过 GameDatabase 加载音频
            // 路径格式：相对于 GameData/，不包含扩展名，空格保持原样
            AudioClip clip = GameDatabase.Instance.GetAudioClip(clipPath);

            if (clip != null)
            {
                _clipCache[clipPath] = clip;
            }
            else
            {
                Debug.LogWarning($"[RealRadio] 加载音频失败：{clipPath}");
            }

            return clip;
        }

        /// <summary>
        /// 顺序播放协程。
        /// </summary>
        private IEnumerator PlaySequenceCoroutine()
        {
            _isPlayingSequence = true;

            while (_sequenceQueue.Count > 0)
            {
                AudioClip clip = _sequenceQueue.Dequeue();
                if (clip == null)
                {
                    continue;
                }

                _audioSource.clip = clip;
                _audioSource.Play();
                Debug.Log($"[RealRadio] 顺序播放音频：{clip.name}");

                // 等待当前音频播放完毕
                yield return new WaitForSeconds(clip.length);
            }

            _isPlayingSequence = false;
        }
    }
}
