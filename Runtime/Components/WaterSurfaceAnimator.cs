using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Siliq.Water
{
    /// <summary>
    /// マテリアルのノーマルマップ (または任意のテクスチャプロパティ) を
    /// 時間経過でスクロールさせる軽量コンポーネント。
    /// Standard / URP Lit / VRChat Mobile など、対象プロパティを持つ
    /// シェーダーであれば動作する。
    ///
    /// インスペクタの値を変えるとエディタ上 (Play せずシーンビュー上) でも
    /// その場で見た目に反映されるため、調整がしやすい。
    /// </summary>
    [AddComponentMenu("Siliq Water/Water Surface Animator")]
    [RequireComponent(typeof(Renderer))]
    [ExecuteAlways]
    public class WaterSurfaceAnimator : MonoBehaviour
    {
        [Tooltip("スクロールさせるテクスチャのプロパティ名。Standard/URP Lit/VRChat Mobile は _BumpMap、Siliq 独自シェーダーは _NormalMap。")]
        public string texturePropertyName = "_BumpMap";

        [Header("動き")]
        [Tooltip("波が流れる向き (度)。0=右、90=上、180=左、270=下。")]
        [Range(0f, 360f)] public float directionDegrees = 30f;

        [Tooltip("流れる速さ。0で静止。")]
        [Range(0f, 2f)] public float speed = 0.3f;

        [Header("見た目")]
        [Tooltip("凹凸の強さ。シェーダーに _BumpScale (Standard 等) がある場合のみ有効。")]
        [Range(0f, 3f)] public float strength = 1f;

        [Tooltip("模様の大きさ。1 が元のサイズ、大きいほど模様が細かく (タイリング数が増え) 見える。")]
        [Range(0.1f, 8f)] public float tiling = 1f;

        Renderer targetRenderer;
        Material materialInstance;
        Vector2 offset;
        int texPropertyId;
        int bumpScalePropertyId;
        string cachedPropertyName;

#if UNITY_EDITOR
        double lastEditorTime;
#endif

        void OnEnable()
        {
            targetRenderer = GetComponent<Renderer>();
            materialInstance = targetRenderer.sharedMaterial != null ? targetRenderer.material : null;
            CachePropertyIds();

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                lastEditorTime = 0;
                EditorApplication.update -= EditorTick;
                EditorApplication.update += EditorTick;
            }
#endif
        }

        void OnDisable()
        {
#if UNITY_EDITOR
            EditorApplication.update -= EditorTick;
#endif
        }

        void CachePropertyIds()
        {
            cachedPropertyName = texturePropertyName;
            texPropertyId = Shader.PropertyToID(texturePropertyName);
            bumpScalePropertyId = Shader.PropertyToID("_BumpScale");
        }

        void Update()
        {
            if (!Application.isPlaying) return; // エディタ (非再生時) は EditorTick が担当
            Animate(Time.deltaTime);
        }

#if UNITY_EDITOR
        void EditorTick()
        {
            if (this == null || Application.isPlaying) return;
            double now = EditorApplication.timeSinceStartup;
            float dt = lastEditorTime > 0 ? (float)(now - lastEditorTime) : 0f;
            lastEditorTime = now;
            Animate(dt);
            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.RepaintAll();
            }
        }
#endif

        void Animate(float dt)
        {
            if (materialInstance == null || targetRenderer == null) return;
            if (cachedPropertyName != texturePropertyName) CachePropertyIds();

            float rad = directionDegrees * Mathf.Deg2Rad;
            var dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            offset += dir * speed * dt;
            offset.x %= 1f;
            offset.y %= 1f;

            if (materialInstance.HasProperty(texPropertyId))
            {
                materialInstance.SetTextureOffset(texPropertyId, offset);
                materialInstance.SetTextureScale(texPropertyId, Vector2.one * tiling);
            }
            if (materialInstance.HasProperty(bumpScalePropertyId))
            {
                materialInstance.SetFloat(bumpScalePropertyId, strength);
            }
        }
    }
}
