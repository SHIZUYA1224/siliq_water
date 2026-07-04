using UnityEngine;

namespace Siliq.Water
{
    /// <summary>
    /// 実行時にプロファイルから水面マップをベイクしてレンダラーへ適用するコンポーネント。
    /// テクスチャをビルドに含めず、ロード時に生成したい場合や、
    /// 起動ごとにシード違いの水面にしたい場合に使う。
    /// </summary>
    [AddComponentMenu("Siliq Water/Runtime Water Map Applier")]
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

        void Start()
        {
            Apply();
        }

        /// <summary>マップをベイクしてレンダラーのマテリアルへ適用する。再呼び出しで再生成。</summary>
        public void Apply()
        {
            if (profile == null)
            {
                Debug.LogWarning("[Siliq Water] プロファイルが設定されていません。", this);
                return;
            }

            ReleaseTextures();

            WaterMapSettings settings = profile.settings.Clone();
            if (randomizeSeed)
            {
                settings.globalSeed = Random.Range(0, 999999);
            }

            int size = Mathf.Max(64, Mathf.ClosestPowerOfTwo(resolution));
            var renderer = GetComponent<Renderer>();
            Material mat = renderer.material; // インスタンス化して他オブジェクトへの影響を防ぐ

            if (!string.IsNullOrEmpty(normalMapProperty))
            {
                bakedNormal = WaterMapCore.BakeTexture(settings, WaterMapType.Normal, size, 0f, highPrecision);
                mat.SetTexture(normalMapProperty, bakedNormal);
                // Standard / URP Lit の _BumpMap を使う場合はキーワードも必要
                if (normalMapProperty == "_BumpMap")
                {
                    mat.EnableKeyword("_NORMALMAP");
                }
            }
            if (!string.IsNullOrEmpty(foamMapProperty))
            {
                bakedFoam = WaterMapCore.BakeTexture(settings, WaterMapType.Foam, size);
                mat.SetTexture(foamMapProperty, bakedFoam);
            }
            if (!string.IsNullOrEmpty(flowMapProperty))
            {
                bakedFlow = WaterMapCore.BakeTexture(settings, WaterMapType.Flow, size);
                mat.SetTexture(flowMapProperty, bakedFlow);
            }
        }

        void OnDestroy()
        {
            ReleaseTextures();
        }

        void ReleaseTextures()
        {
            if (bakedNormal != null) Destroy(bakedNormal);
            if (bakedFoam != null) Destroy(bakedFoam);
            if (bakedFlow != null) Destroy(bakedFlow);
            bakedNormal = bakedFoam = bakedFlow = null;
        }
    }
}
