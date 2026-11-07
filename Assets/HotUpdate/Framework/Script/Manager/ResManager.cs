using Framework.Script.Base;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace Framework.Script.Manager
{
    /// <summary>
    /// Addressables 资源句柄，用于替代原来的 AssetBundle 返回值。
    /// 一个句柄对应一个 Addressables Key（Address 或 Label）。
    /// </summary>
    public class ResHandle
    {
        /// <summary>
        /// Addressables Key（Address 或 Label）
        /// </summary>
        public string Key { get; private set; }

        /// <summary>
        /// 兼容旧代码中的 kAB.name
        /// </summary>
        public string name
        {
            get { return Key; }
        }

        internal AsyncOperationHandle<IList<UnityEngine.Object>> Handle;
        internal IList<UnityEngine.Object> Assets;

        internal bool IsValid
        {
            get { return Handle.IsValid(); }
        }

        internal ResHandle(string _key)
        {
            Key = _key;
        }
    }

    /// <summary>
    /// 资源管理器：所有资源改为通过 Addressables 加载。
    /// 旧的 Load/GetObject 接口保留，内部使用 Addressables 实现，尽量少改调用方代码。
    /// </summary>
    public class ResManager : Singleton<ResManager>
    {
        // 请求的 Key（Address / Label） -> 预加载句柄
        private Dictionary<string, ResHandle> m_ResHandleDic = new();

        // 资源 Address + 类型 -> 单个资源加载句柄
        private Dictionary<string, AsyncOperationHandle> m_AssetHandleDic = new();

        private bool m_IsInit = false;

        /// <summary>
        /// 初始化 Addressables（首次访问 ResManager.Instance 时执行）
        /// </summary>
        protected override void OnInit()
        {
            if (m_IsInit)
            {
                return;
            }

            try
            {
                AsyncOperationHandle kHandle = Addressables.InitializeAsync();
                kHandle.WaitForCompletion();
                m_IsInit = kHandle.Status == AsyncOperationStatus.Succeeded;

                if (m_IsInit)
                {
                    Debug.Log("[ResManager] Addressables 初始化完成");
                }
                else
                {
                    Debug.LogError("[ResManager] Addressables 初始化失败：" + kHandle.OperationException);
                }
            }
            catch (Exception ex)
            {
                m_IsInit = false;
                Debug.LogError("[ResManager] Addressables 初始化异常\n" + ex);
            }
        }

        /// <summary>
        /// 预加载资源（兼容旧接口）。
        /// 旧 AssetBundle 的 ".ui / .unity3d / .data" 后缀语义已废弃：
        /// FairyGUI 包统一由 FairyGUIManager 按 &lt;包名&gt;_fui / &lt;包名&gt;_atlas 地址加载，
        /// 这里只按资源 Address（或 Label）预加载，不再拼接任何后缀。
        /// </summary>
        /// <param name="_name">资源名 / Addressables 地址或 Label</param>
        public ResHandle Load(string _name)
        {
            if (string.IsNullOrEmpty(_name))
            {
                Debug.LogError("[ResManager] Load 资源名不能为空");
                return null;
            }

            string kRequestKey = _name;

            ResHandle kResHandle;
            if (m_ResHandleDic.TryGetValue(kRequestKey, out kResHandle))
            {
                return kResHandle;
            }

            string kRealKey;
            IList<IResourceLocation> kLocations = FindLocations(kRequestKey, typeof(UnityEngine.Object), out kRealKey);
            kResHandle = new ResHandle(kRealKey ?? kRequestKey);

            if (kLocations == null || kLocations.Count == 0)
            {
                // 预加载失败不算致命错误：旧逻辑里 Load 只负责预加载，真正取资源在 GetObject 中
                Debug.LogWarning("[ResManager] Addressables 未找到预加载 Key：" + kRequestKey);
            }
            else
            {
                try
                {
                    AsyncOperationHandle<IList<UnityEngine.Object>> kLoadHandle =
                        Addressables.LoadAssetsAsync<UnityEngine.Object>(kLocations, null, false);
                    kResHandle.Assets = kLoadHandle.WaitForCompletion();

                    if (kResHandle.Assets != null)
                    {
                        kResHandle.Handle = kLoadHandle;
                        Debug.Log("[ResManager] 预加载成功 Key：" + kResHandle.Key + "，资源数量：" + kResHandle.Assets.Count);
                    }
                    else
                    {
                        Addressables.Release(kLoadHandle);
                        Debug.LogError("[ResManager] Addressables 预加载失败 Key：" + kResHandle.Key);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError("[ResManager] Addressables 预加载异常 Key：" + kRequestKey + "\n" + ex);
                }
            }

            m_ResHandleDic.Add(kRequestKey, kResHandle);
            return kResHandle;
        }

        /// <summary>
        /// 兼容旧调用：后缀参数已废弃并被忽略
        /// </summary>
        [Obsolete("AssetBundle 后缀已废弃，请直接调用 Load(name)")]
        public ResHandle Load(string _name, string _suffix)
        {
            return Load(_name);
        }


        /// <summary>
        /// 从预加载结果中获取资源；如果预加载没有包含该资源，则按 Address 单独加载。
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="_handle">Load 返回的资源句柄</param>
        /// <param name="_name">资源名称（资源的 Address，一般是资源文件名）</param>
        public T GetObject<T>(ResHandle _handle, string _name = "") where T : UnityEngine.Object
        {
            // 1. 优先从预加载结果中查找
            if (_handle != null && _handle.Assets != null)
            {
                for (int i = 0; i < _handle.Assets.Count; i++)
                {
                    UnityEngine.Object kAsset = _handle.Assets[i];
                    if (kAsset == null || !(kAsset is T))
                    {
                        continue;
                    }

                    if (string.IsNullOrEmpty(_name) ||
                        string.Equals(kAsset.name, _name, StringComparison.OrdinalIgnoreCase))
                    {
                        return (T)kAsset;
                    }
                }
            }

            string kAddress = string.IsNullOrEmpty(_name)
                ? (_handle != null ? _handle.Key : string.Empty)
                : _name;

            if (string.IsNullOrEmpty(kAddress))
            {
                Debug.LogError("[ResManager] GetObject 资源名不能为空");
                return null;
            }

            // 2. 按资源 Address 加载
            T kResult = LoadAsset<T>(kAddress);
            if (kResult != null)
            {
                return kResult;
            }

            if (_handle != null && !string.IsNullOrEmpty(_name) && _name != _handle.Key)
            {
                // 3. 兼容 "Key/资源名" 形式的 Address
                kResult = LoadAsset<T>(_handle.Key + "/" + _name);
                if (kResult != null)
                {
                    return kResult;
                }
            }

            // 4. 有些资源配置时直接把 Key 用作了 Address
            if (_handle != null && _name != _handle.Key)
            {
                kResult = LoadAsset<T>(_handle.Key);
                if (kResult != null)
                {
                    return kResult;
                }
            }

            Debug.LogError("[ResManager] Addressables 加载资源失败，Key：" +
                (_handle != null ? _handle.Key : "null") + "，资源名：" + _name);
            return null;
        }

        /// <summary>
        /// 按 Addressables Address 同步加载单个资源
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="_address">资源 Address</param>
        public T LoadAsset<T>(string _address) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(_address))
            {
                return null;
            }

            string kCacheKey = GetAssetCacheKey<T>(_address);
            AsyncOperationHandle kCachedHandle;
            if (m_AssetHandleDic.TryGetValue(kCacheKey, out kCachedHandle))
            {
                if (kCachedHandle.IsValid())
                {
                    return kCachedHandle.Result as T;
                }
                m_AssetHandleDic.Remove(kCacheKey);
            }

            string kRealAddress;
            if (!HasLocations<T>(_address, out kRealAddress))
            {
                return null;
            }

            AsyncOperationHandle<T> kHandle = default(AsyncOperationHandle<T>);
            T kAsset;
            try
            {
                kHandle = Addressables.LoadAssetAsync<T>(kRealAddress);
                kAsset = kHandle.WaitForCompletion();
            }
            catch (Exception ex)
            {
                Debug.LogError("[ResManager] Addressables 加载异常 Address：" + kRealAddress + "\n" + ex);
                if (kHandle.IsValid())
                {
                    Addressables.Release(kHandle);
                }
                return null;
            }

            if (kAsset == null)
            {
                if (kHandle.IsValid())
                {
                    Addressables.Release(kHandle);
                }
                return null;
            }

            m_AssetHandleDic.Add(kCacheKey, kHandle);
            return kAsset;
        }

        /// <summary>
        /// 卸载掉所有已加载的资源
        /// </summary>
        /// <param name="_y">仅为兼容旧接口；Addressables 通过 Release 句柄管理资源，无需再指定是否销毁资源</param>
        public void UnLoadAll(bool _y = false)
        {
            foreach (var kHandle in m_AssetHandleDic.Values)
            {
                if (kHandle.IsValid())
                {
                    Addressables.Release(kHandle);
                }
            }
            m_AssetHandleDic.Clear();

            foreach (var kResHandle in m_ResHandleDic.Values)
            {
                if (kResHandle.IsValid)
                {
                    Addressables.Release(kResHandle.Handle);
                }
                kResHandle.Assets = null;
            }
            m_ResHandleDic.Clear();

            Debug.Log("[ResManager] 已释放全部 Addressables 资源");
        }

        /// <summary>
        /// 卸载指定的资源
        /// </summary>
        /// <param name="_name">资源名 / 旧 AB 包名</param>
        /// <param name="_y">仅为兼容旧接口</param>
        public void Unload(string _name, bool _y = false)
        {
            if (string.IsNullOrEmpty(_name))
            {
                return;
            }

            List<string> kAssetKeys = new List<string>();
            foreach (var kPair in m_AssetHandleDic)
            {
                if (kPair.Key == _name || kPair.Key.StartsWith(_name + "|"))
                {
                    kAssetKeys.Add(kPair.Key);
                }
            }

            for (int i = 0; i < kAssetKeys.Count; i++)
            {
                AsyncOperationHandle kHandle = m_AssetHandleDic[kAssetKeys[i]];
                if (kHandle.IsValid())
                {
                    Addressables.Release(kHandle);
                }
                m_AssetHandleDic.Remove(kAssetKeys[i]);
            }

            ResHandle kResHandle;
            if (m_ResHandleDic.TryGetValue(_name, out kResHandle))
            {
                if (kResHandle.IsValid)
                {
                    Addressables.Release(kResHandle.Handle);
                }
                kResHandle.Assets = null;
                m_ResHandleDic.Remove(_name);
            }
        }

        /// <summary>
        /// 查询 Key 对应的资源位置，并兼容大小写（旧 AB 包名全部是小写）
        /// </summary>
        private IList<IResourceLocation> FindLocations(string _key, Type _type, out string _realKey)
        {
            IList<IResourceLocation> kLocations = GetLocations(_key, _type);
            if (kLocations != null && kLocations.Count > 0)
            {
                _realKey = _key;
                return kLocations;
            }

            string kLowerKey = _key.ToLowerInvariant();
            if (kLowerKey != _key)
            {
                kLocations = GetLocations(kLowerKey, _type);
                if (kLocations != null && kLocations.Count > 0)
                {
                    _realKey = kLowerKey;
                    return kLocations;
                }
            }

            _realKey = null;
            return null;
        }

        private IList<IResourceLocation> GetLocations(string _key, Type _type)
        {
            try
            {
                AsyncOperationHandle<IList<IResourceLocation>> kHandle =
                    Addressables.LoadResourceLocationsAsync(_key, _type);
                IList<IResourceLocation> kLocations = kHandle.WaitForCompletion();

                if (kHandle.IsValid())
                {
                    Addressables.Release(kHandle);
                }

                return kLocations;
            }
            catch (Exception ex)
            {
                Debug.LogError("[ResManager] Addressables 查询资源位置异常 Key：" + _key + "\n" + ex);
                return null;
            }
        }

        private bool HasLocations<T>(string _address, out string _realAddress) where T : UnityEngine.Object
        {
            string kRealKey;
            IList<IResourceLocation> kLocations = FindLocations(_address, typeof(T), out kRealKey);
            _realAddress = kRealKey;
            return kLocations != null && kLocations.Count > 0;
        }

        private string GetAssetCacheKey<T>(string _address)
        {
            return _address + "|" + typeof(T).FullName;
        }
    }
}
