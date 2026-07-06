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

        [Tooltip("複数マテリアルの Renderer で対象にするスロット。通常は 0。")]
        [Min(0)] public int materialSlot = 0;

        [Tooltip("編集モードでもシーンビュー上でスクロールをプレビューする。")]
        public bool animateInEditMode = true;

        [Header("動き")]
        [Tooltip("波が流れる向き (度)。0=右、90=上、180=左、270=下。")]
        [Range(0f, 360f)] public float directionDegrees = 30f;

        [Tooltip("流れる速さ。0で静止。")]
        [Range(0f, 3f)] public float speed = 0.6f;

        [Header("見た目")]
        [Tooltip("凹凸の強さ。シェーダーに _BumpScale (Standard 等) がある場合のみ有効。")]
        [Range(0f, 3f)] public float strength = 1.4f;

        [Tooltip("模様の大きさ。1 が元のサイズ、大きいほど模様が細かく (タイリング数が増え) 見える。")]
        [Range(0.1f, 8f)] public float tiling = 1f;

        Renderer targetRenderer;
        Material targetMaterial;
        MaterialPropertyBlock propertyBlock;
        Vector2 offset;
        int texPropertyId;
        int texStPropertyId;
        int bumpScalePropertyId;
        int normalStrengthPropertyId;
        int tiling1PropertyId;
        int tiling2PropertyId;
        int scroll1PropertyId;
        int scroll2PropertyId;
        int mainTexPropertyId;
        int mainTexStPropertyId;
        int baseMapPropertyId;
        int baseMapStPropertyId;
        string cachedPropertyName;
        Vector2 baseTextureScale = Vector2.one;
        Vector2 baseTextureOffset = Vector2.zero;
        Vector2 mainTexBaseScale = Vector2.one;
        Vector2 mainTexBaseOffset = Vector2.zero;
        Vector2 baseMapBaseScale = Vector2.one;
        Vector2 baseMapBaseOffset = Vector2.zero;
        bool hasTextureTransform;
        bool hasMainTexTransform;
        bool hasBaseMapTransform;
        bool hasSiliqScrollControls;

#if UNITY_EDITOR
        double lastEditorTime;
#endif

        void OnEnable()
        {
            RebindRendererAndMaterial();
            EnsurePropertyBlock();

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
            texStPropertyId = Shader.PropertyToID(texturePropertyName + "_ST");
            bumpScalePropertyId = Shader.PropertyToID("_BumpScale");
            normalStrengthPropertyId = Shader.PropertyToID("_NormalStrength");
            tiling1PropertyId = Shader.PropertyToID("_Tiling1");
            tiling2PropertyId = Shader.PropertyToID("_Tiling2");
            scroll1PropertyId = Shader.PropertyToID("_Scroll1");
            scroll2PropertyId = Shader.PropertyToID("_Scroll2");
            mainTexPropertyId = Shader.PropertyToID("_MainTex");
            mainTexStPropertyId = Shader.PropertyToID("_MainTex_ST");
            baseMapPropertyId = Shader.PropertyToID("_BaseMap");
            baseMapStPropertyId = Shader.PropertyToID("_BaseMap_ST");
        }

        void Update()
        {
            if (!Application.isPlaying) return; // エディタ (非再生時) は EditorTick が担当
            Animate(Time.deltaTime);
        }

#if UNITY_EDITOR
        void EditorTick()
        {
            if (this == null || Application.isPlaying || !animateInEditMode) return;
            double now = EditorApplication.timeSinceStartup;
            float dt = lastEditorTime > 0 ? (float)(now - lastEditorTime) : 0f;
            lastEditorTime = now;
            Animate(dt);
            if (speed > 0f && SceneView.lastActiveSceneView != null)
            {
                SceneView.RepaintAll();
            }
        }
#endif

        void OnValidate()
        {
            if (texturePropertyName == null) texturePropertyName = string.Empty;
            if (propertyBlock == null) propertyBlock = new MaterialPropertyBlock();
            RebindRendererAndMaterial();
            ApplyProperties(0f);
        }

        /// <summary>テストやカスタムツールから即時反映したい場合に使う。</summary>
        public void ApplyImmediate(float deltaTime = 0f)
        {
            Animate(deltaTime);
        }

        void EnsurePropertyBlock()
        {
            if (propertyBlock == null)
            {
                propertyBlock = new MaterialPropertyBlock();
            }
        }

        void RebindRendererAndMaterial()
        {
            if (targetRenderer == null) targetRenderer = GetComponent<Renderer>();
            CachePropertyIds();

            targetMaterial = null;
            hasTextureTransform = false;
            hasMainTexTransform = false;
            hasBaseMapTransform = false;
            hasSiliqScrollControls = false;
            baseTextureScale = Vector2.one;
            baseTextureOffset = Vector2.zero;
            mainTexBaseScale = Vector2.one;
            mainTexBaseOffset = Vector2.zero;
            baseMapBaseScale = Vector2.one;
            baseMapBaseOffset = Vector2.zero;

            if (targetRenderer == null) return;
            var materials = targetRenderer.sharedMaterials;
            if (materials == null || materials.Length == 0) return;

            int index = Mathf.Clamp(materialSlot, 0, materials.Length - 1);
            targetMaterial = materials[index];
            if (targetMaterial == null) return;

            hasTextureTransform = targetMaterial.HasProperty(texPropertyId);
            if (hasTextureTransform)
            {
                baseTextureScale = targetMaterial.GetTextureScale(texturePropertyName);
                baseTextureOffset = targetMaterial.GetTextureOffset(texturePropertyName);
            }

            // Standard / URP Lit は normal map 固有の ST ではなく、
            // 主テクスチャの ST で normal map の UV も動かす実装がある。
            if (texturePropertyName == "_BumpMap")
            {
                hasMainTexTransform = TryCacheTextureTransform("_MainTex", mainTexPropertyId, out mainTexBaseScale, out mainTexBaseOffset);
                hasBaseMapTransform = TryCacheTextureTransform("_BaseMap", baseMapPropertyId, out baseMapBaseScale, out baseMapBaseOffset);
            }

            hasSiliqScrollControls = targetMaterial.HasProperty(scroll1PropertyId) ||
                                     targetMaterial.HasProperty(scroll2PropertyId) ||
                                     targetMaterial.HasProperty(tiling1PropertyId) ||
                                     targetMaterial.HasProperty(normalStrengthPropertyId);
        }

        bool TryCacheTextureTransform(string propertyName, int propertyId, out Vector2 scale, out Vector2 offset)
        {
            scale = Vector2.one;
            offset = Vector2.zero;
            if (targetMaterial == null || !targetMaterial.HasProperty(propertyId)) return false;

            scale = targetMaterial.GetTextureScale(propertyName);
            offset = targetMaterial.GetTextureOffset(propertyName);
            return true;
        }

        void Animate(float dt)
        {
            if (targetRenderer == null || targetMaterial == null ||
                cachedPropertyName != texturePropertyName ||
                CurrentSharedMaterial() != targetMaterial)
            {
                RebindRendererAndMaterial();
            }
            ApplyProperties(dt);
        }

        Material CurrentSharedMaterial()
        {
            if (targetRenderer == null) return null;
            var materials = targetRenderer.sharedMaterials;
            if (materials == null || materials.Length == 0) return null;
            int index = Mathf.Clamp(materialSlot, 0, materials.Length - 1);
            return materials[index];
        }

        void ApplyProperties(float dt)
        {
            if (targetRenderer == null || targetMaterial == null) return;

            float rad = directionDegrees * Mathf.Deg2Rad;
            var dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            offset += dir * speed * dt;
            offset.x %= 1f;
            offset.y %= 1f;

            EnsurePropertyBlock();
            var materials = targetRenderer.sharedMaterials;
            if (materials == null || materials.Length == 0) return;
            int index = Mathf.Clamp(materialSlot, 0, materials.Length - 1);
            targetRenderer.GetPropertyBlock(propertyBlock, index);

            if (hasTextureTransform)
            {
                Vector2 scale = new Vector2(baseTextureScale.x * tiling, baseTextureScale.y * tiling);
                Vector2 finalOffset = baseTextureOffset + offset;
                propertyBlock.SetVector(texStPropertyId, new Vector4(scale.x, scale.y, finalOffset.x, finalOffset.y));
            }
            if (hasMainTexTransform)
            {
                SetTextureSt(mainTexStPropertyId, mainTexBaseScale, mainTexBaseOffset);
            }
            if (hasBaseMapTransform)
            {
                SetTextureSt(baseMapStPropertyId, baseMapBaseScale, baseMapBaseOffset);
            }
            if (targetMaterial.HasProperty(bumpScalePropertyId))
            {
                propertyBlock.SetFloat(bumpScalePropertyId, strength);
            }
            if (targetMaterial.HasProperty(normalStrengthPropertyId))
            {
                propertyBlock.SetFloat(normalStrengthPropertyId, strength);
            }

            // Siliq 独自シェーダーは [NoScaleOffset] の _NormalMap を使うため、
            // テクスチャ ST ではなくシェーダー固有のスクロール/タイリング値を動かす。
            if (hasSiliqScrollControls)
            {
                Vector2 scroll = dir * speed;
                if (targetMaterial.HasProperty(scroll1PropertyId))
                {
                    propertyBlock.SetVector(scroll1PropertyId, new Vector4(scroll.x, scroll.y, 0f, 0f));
                }
                if (targetMaterial.HasProperty(scroll2PropertyId))
                {
                    Vector2 second = new Vector2(-dir.y, dir.x) * speed * 0.73f;
                    propertyBlock.SetVector(scroll2PropertyId, new Vector4(second.x, second.y, 0f, 0f));
                }
                if (targetMaterial.HasProperty(tiling1PropertyId))
                {
                    propertyBlock.SetFloat(tiling1PropertyId, tiling);
                }
                if (targetMaterial.HasProperty(tiling2PropertyId))
                {
                    propertyBlock.SetFloat(tiling2PropertyId, Mathf.Max(0.1f, tiling * 2.7f));
                }
            }

            targetRenderer.SetPropertyBlock(propertyBlock, index);
        }

        void SetTextureSt(int propertyId, Vector2 baseScale, Vector2 baseOffset)
        {
            Vector2 scale = new Vector2(baseScale.x * tiling, baseScale.y * tiling);
            Vector2 finalOffset = baseOffset + offset;
            propertyBlock.SetVector(propertyId, new Vector4(scale.x, scale.y, finalOffset.x, finalOffset.y));
        }
    }
}
