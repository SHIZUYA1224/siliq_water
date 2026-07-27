using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Siliq.Water
{
    /// <summary>
    /// 実行時にプロファイルから水面マップをベイクしてレンダラーへ適用するコンポーネント。
    /// テクスチャをビルドに含めず、ロード時に生成したい場合や、
    /// 起動ごとにシード違いの水面にしたい場合に使う。
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public class RuntimeWaterMapApplier : MonoBehaviour
    {
        [Tooltip("エディタの水面マップスタジオで保存したプロファイル")]
        public WaterMapProfile profile;

        [Tooltip("ベイク解像度 (2 のべき乗を推奨)")]
        public int resolution = 512;

        [Tooltip("ON にすると起動ごとにシードをランダム化して毎回違う水面にする")]
        public bool randomizeSeed = false;

        [Tooltip("16bit float テクスチャでベイクしてバンディングを防ぐ (メモリ 2 倍)")]
        public bool highPrecision = false;

        [Tooltip("ON にすると起動時の色計算をバックグラウンドで行い、メインスレッド停止を短くします。")]
        public bool generateAsync = true;

        [Tooltip("同じプロファイル・解像度・マップ種の生成結果をシーン内で使い回します。")]
        public bool useTextureCache = true;

        [Header("マテリアルプロパティ名")]
        [Tooltip("ノーマルマップの割り当て先 (Standard/URP Lit は _BumpMap、Siliq 水シェーダーは _NormalMap)")]
        public string normalMapProperty = "_NormalMap";

        [Tooltip("空でなければフォームマスクもベイクして割り当てる (例: _FoamMap)")]
        public string foamMapProperty = "";

        [Tooltip("空でなければフローマップもベイクして割り当てる (例: _FlowMap)")]
        public string flowMapProperty = "";

        Texture2D bakedNormal;
        Texture2D bakedFoam;
        Texture2D bakedFlow;
        TextureHandle normalHandle;
        TextureHandle foamHandle;
        TextureHandle flowHandle;
        Material materialInstance;
        Coroutine applyCoroutine;

        public bool IsGenerating { get; private set; }

        class CacheEntry
        {
            public Texture2D texture;
            public int references;
        }

        struct TextureHandle
        {
            public Texture2D texture;
            public string cacheKey;
            public bool cached;
        }

        struct MapRequest
        {
            public WaterMapType mapType;
            public string propertyName;
            public bool highPrecision;

            public MapRequest(WaterMapType mapType, string propertyName, bool highPrecision)
            {
                this.mapType = mapType;
                this.propertyName = propertyName;
                this.highPrecision = highPrecision;
            }
        }

        static readonly Dictionary<string, CacheEntry> TextureCache = new Dictionary<string, CacheEntry>();

        void Start()
        {
            if (generateAsync)
            {
                ApplyAsync();
            }
            else
            {
                Apply();
            }
        }

        /// <summary>マップをベイクしてレンダラーのマテリアルへ適用する。再呼び出しで再生成。</summary>
        public void Apply()
        {
            StopApplyCoroutine();

            if (!TryPrepare(out WaterMapSettings settings, out int size, out Material mat)) return;

            ReleaseTextures();

            var requests = BuildRequests(mat);
            for (int i = 0; i < requests.Count; i++)
            {
                MapRequest request = requests[i];
                TextureHandle handle = AcquireOrGenerate(settings, request.mapType, size, request.highPrecision);
                AssignTexture(mat, request.propertyName, handle.texture);
                StoreHandle(request.mapType, handle);
            }
        }

        /// <summary>マップを非同期生成して適用する。生成完了までは既存テクスチャを維持します。</summary>
        public void ApplyAsync()
        {
            StopApplyCoroutine();
            applyCoroutine = StartCoroutine(ApplyCoroutine());
        }

        IEnumerator ApplyCoroutine()
        {
            if (!TryPrepare(out WaterMapSettings settings, out int size, out Material mat))
            {
                applyCoroutine = null;
                yield break;
            }

            var requests = BuildRequests(mat);
            if (requests.Count == 0)
            {
                applyCoroutine = null;
                yield break;
            }

            IsGenerating = true;
            var pendingHandles = new List<TextureHandle>();
            var pendingRequests = new List<MapRequest>();
            bool applied = false;

            try
            {
                for (int i = 0; i < requests.Count; i++)
                {
                    MapRequest request = requests[i];
                    string key = BuildCacheKey(settings, request.mapType, size, request.highPrecision);
                    if (TryAcquireCached(key, out TextureHandle cached))
                    {
                        pendingRequests.Add(request);
                        pendingHandles.Add(cached);
                        continue;
                    }

                    Task<Color[]> task = Task.Run(() => WaterMapCore.GenerateColors(settings, request.mapType, size, 0f));
                    while (!task.IsCompleted)
                    {
                        yield return null;
                    }

                    if (task.IsFaulted)
                    {
                        Debug.LogException(task.Exception, this);
                        continue;
                    }

                    Texture2D tex = WaterMapCore.CreateTexture(task.Result, request.mapType, size, request.highPrecision,
                        WaterMapCore.ShouldDither(settings, request.mapType));
                    TextureHandle handle = StoreGeneratedTexture(key, tex);
                    pendingRequests.Add(request);
                    pendingHandles.Add(handle);
                    yield return null;
                }

                ReleaseTextures();
                for (int i = 0; i < pendingRequests.Count; i++)
                {
                    AssignTexture(mat, pendingRequests[i].propertyName, pendingHandles[i].texture);
                    StoreHandle(pendingRequests[i].mapType, pendingHandles[i]);
                }

                applied = true;
            }
            finally
            {
                if (!applied)
                {
                    for (int i = 0; i < pendingHandles.Count; i++)
                    {
                        ReleaseHandle(pendingHandles[i]);
                    }
                }
                IsGenerating = false;
                applyCoroutine = null;
            }
        }

        bool TryPrepare(out WaterMapSettings settings, out int size, out Material mat)
        {
            settings = null;
            mat = null;
            size = 0;

            if (profile == null)
            {
                Debug.LogWarning("[Siliq Water] プロファイルが設定されていません。", this);
                return false;
            }

            settings = profile.settings.Clone();
            if (randomizeSeed)
            {
                settings.globalSeed = Random.Range(0, 999999);
            }

            size = Mathf.Max(64, Mathf.ClosestPowerOfTwo(resolution));
            var renderer = GetComponent<Renderer>();
            if (renderer == null)
            {
                Debug.LogWarning("[Siliq Water] Renderer が見つかりません。", this);
                return false;
            }
            materialInstance = renderer.material; // インスタンス化して他オブジェクトへの影響を防ぐ
            mat = materialInstance;
            return mat != null;
        }

        List<MapRequest> BuildRequests(Material mat)
        {
            var requests = new List<MapRequest>();
            AddRequestIfValid(requests, mat, normalMapProperty, WaterMapType.Normal, highPrecision);
            AddRequestIfValid(requests, mat, foamMapProperty, WaterMapType.Foam, false);
            AddRequestIfValid(requests, mat, flowMapProperty, WaterMapType.Flow, false);
            return requests;
        }

        void AddRequestIfValid(List<MapRequest> requests, Material mat, string propertyName, WaterMapType type, bool precision)
        {
            if (string.IsNullOrEmpty(propertyName)) return;
            if (!mat.HasProperty(propertyName))
            {
                Debug.LogWarning($"[Siliq Water] マテリアル '{mat.name}' に '{propertyName}' プロパティがないため {type} の適用をスキップしました。", this);
                return;
            }
            requests.Add(new MapRequest(type, propertyName, precision));
        }

        void AssignTexture(Material mat, string propertyName, Texture2D texture)
        {
            if (texture == null || string.IsNullOrEmpty(propertyName) || !mat.HasProperty(propertyName)) return;
            mat.SetTexture(propertyName, texture);

            if (propertyName == "_BumpMap")
            {
                mat.EnableKeyword("_NORMALMAP");
            }
            else if (propertyName == "_FlowMap")
            {
                mat.EnableKeyword("_USE_FLOWMAP");
            }
            else if (propertyName == "_FoamMap")
            {
                mat.EnableKeyword("_SHORE_EFFECTS");
            }
        }

        void OnDestroy()
        {
            StopApplyCoroutine();
            ReleaseTextures();
            if (materialInstance != null)
            {
                DestroyGeneratedObject(materialInstance);
                materialInstance = null;
            }
        }

        void ReleaseTextures()
        {
            ReleaseHandle(normalHandle);
            ReleaseHandle(foamHandle);
            ReleaseHandle(flowHandle);
            normalHandle = foamHandle = flowHandle = default(TextureHandle);
            bakedNormal = bakedFoam = bakedFlow = null;
        }

        void StoreHandle(WaterMapType type, TextureHandle handle)
        {
            switch (type)
            {
                case WaterMapType.Normal:
                    normalHandle = handle;
                    bakedNormal = handle.texture;
                    break;
                case WaterMapType.Foam:
                    foamHandle = handle;
                    bakedFoam = handle.texture;
                    break;
                case WaterMapType.Flow:
                    flowHandle = handle;
                    bakedFlow = handle.texture;
                    break;
            }
        }

        TextureHandle AcquireOrGenerate(WaterMapSettings settings, WaterMapType type, int size, bool precision)
        {
            string key = BuildCacheKey(settings, type, size, precision);
            if (TryAcquireCached(key, out TextureHandle handle)) return handle;

            Texture2D tex = WaterMapCore.BakeTexture(settings, type, size, 0f, precision);
            return StoreGeneratedTexture(key, tex);
        }

        bool TryAcquireCached(string key, out TextureHandle handle)
        {
            handle = default(TextureHandle);
            if (!useTextureCache) return false;

            if (TextureCache.TryGetValue(key, out CacheEntry entry) && entry.texture != null)
            {
                entry.references++;
                handle.texture = entry.texture;
                handle.cacheKey = key;
                handle.cached = true;
                return true;
            }
            return false;
        }

        TextureHandle StoreGeneratedTexture(string key, Texture2D tex)
        {
            if (!useTextureCache)
            {
                return new TextureHandle { texture = tex, cached = false };
            }

            if (TextureCache.TryGetValue(key, out CacheEntry existing) && existing.texture != null)
            {
                existing.references++;
                DestroyGeneratedObject(tex);
                return new TextureHandle { texture = existing.texture, cacheKey = key, cached = true };
            }

            TextureCache[key] = new CacheEntry { texture = tex, references = 1 };
            return new TextureHandle { texture = tex, cacheKey = key, cached = true };
        }

        void ReleaseHandle(TextureHandle handle)
        {
            if (handle.texture == null) return;

            if (handle.cached && !string.IsNullOrEmpty(handle.cacheKey) && TextureCache.TryGetValue(handle.cacheKey, out CacheEntry entry))
            {
                entry.references--;
                if (entry.references <= 0)
                {
                    TextureCache.Remove(handle.cacheKey);
                    DestroyGeneratedObject(entry.texture);
                }
                return;
            }

            DestroyGeneratedObject(handle.texture);
        }

        static string BuildCacheKey(WaterMapSettings settings, WaterMapType type, int size, bool precision)
        {
            return type + "|" + size + "|" + precision + "|" + JsonUtility.ToJson(settings);
        }

        void StopApplyCoroutine()
        {
            if (applyCoroutine != null)
            {
                StopCoroutine(applyCoroutine);
                applyCoroutine = null;
                IsGenerating = false;
            }
        }

        static void DestroyGeneratedObject(Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying) Destroy(obj);
            else DestroyImmediate(obj);
        }
    }
}
