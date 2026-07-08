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
        const float MaxSurfaceSpeed = 0.6f;
        const int DefaultEditModePreviewFps = 10;

        [Tooltip("スクロールさせるテクスチャのプロパティ名。Standard/URP Lit/VRChat Mobile は _BumpMap、Siliq 独自シェーダーは _NormalMap。")]
        public string texturePropertyName = "_BumpMap";

        [Tooltip("複数マテリアルの Renderer で対象にするスロット。通常は 0。")]
        [Min(0)] public int materialSlot = 0;

        [Tooltip("編集モードでもシーンビュー上でスクロールをプレビューする。")]
        public bool animateInEditMode = true;

        [Tooltip("編集モードでのプレビュー更新回数。低いほど軽く、10fps 前後で調整しやすい。Play中の速度には影響しません。")]
        [Range(1, 30)] public int editModePreviewFps = DefaultEditModePreviewFps;

        [Header("動き")]
        [Tooltip("波が流れる向き (度)。0=右、90=上、180=左、270=下。")]
        [Range(0f, 360f)] public float directionDegrees = 30f;

        [Tooltip("流れる速さ。0で静止、0.04がゆっくり、0.08が標準、0.16が速め。")]
        [Range(0f, MaxSurfaceSpeed)] public float speed = 0.08f;

        [Header("見た目")]
        [Tooltip("凹凸の強さ。シェーダーに _BumpScale (Standard 等) がある場合のみ有効。")]
        [Range(0f, 3f)] public float strength = 1.4f;

        [Tooltip("模様の大きさ。1 が元のサイズ、大きいほど模様が細かく (タイリング数が増え) 見える。")]
        [Range(0.1f, 8f)] public float tiling = 1f;

        [Header("高さ")]
        [Tooltip("水面メッシュを実際に上下させる量。Plane のように頂点があるメッシュで有効です。")]
        [Range(0f, 0.5f)] public float displacementStrength = 0.04f;

        [Tooltip("高さの波長。小さいほど大きなうねり、大きいほど細かい起伏になります。")]
        [Range(0.05f, 4f)] public float displacementScale = 0.75f;

        [Tooltip("高さ変位の動く速さ。")]
        [Range(0f, 2f)] public float displacementSpeed = 0.08f;

        [Tooltip("書き出したハイトマップを高さに使う割合。0 なら手続き的なうねりのみ、1 ならハイトマップ中心。")]
        [Range(0f, 1f)] public float heightMapInfluence = 0f;

        [Header("色")]
        [Tooltip("水面の明るい部分の色。Standard / URP Lit ではこの色がベースカラーになります。")]
        public Color shallowColor = new Color(0.34f, 0.90f, 1f, 1f);

        [Tooltip("水面の深い部分の色。Siliq 水シェーダーで有効。")]
        public Color deepColor = new Color(0.01f, 0.16f, 0.34f, 1f);

        [Tooltip("空や環境が映り込む反射色。Siliq 水シェーダーで有効。")]
        public Color reflectionColor = new Color(0.82f, 0.96f, 1f, 1f);

        [Tooltip("透けた水の内側から出る色。透明感と水らしい厚みを作ります。Siliq 水シェーダーで有効。")]
        public Color transmissionColor = new Color(0.40f, 0.95f, 1f, 1f);

        [Tooltip("細い光ときらめきの色。Siliq 水シェーダーで有効。")]
        public Color sparkleColor = new Color(0.95f, 1f, 1f, 1f);

        [Header("透明・反射")]
        [Tooltip("水面の不透明度。1 に近いほど濃く、低いほど透けます。透明マテリアルで特に有効。")]
        [Range(0.05f, 1f)] public float opacity = 0.55f;

        [Tooltip("斜めから見た時に戻る輪郭反射と不透明感。Siliq 水シェーダーで有効。")]
        [Range(0f, 1f)] public float edgeReflection = 0.55f;

        [Tooltip("全体の反射の強さ。Siliq 水シェーダーで有効。")]
        [Range(0f, 1f)] public float reflectionStrength = 0.85f;

        [Tooltip("透過光の強さ。透明感と水の厚みを足します。Siliq 水シェーダーで有効。")]
        [Range(0f, 1f)] public float transmissionStrength = 0.62f;

        [Tooltip("細い光の揺らぎときらめきの強さ。Siliq 水シェーダーで有効。")]
        [Range(0f, 1f)] public float sparkle = 0.22f;

        [Tooltip("強いハイライトの量。反射が弱く見える時はここを上げます。Siliq 水シェーダーで有効。")]
        [Range(0f, 2f)] public float highlightStrength = 1.15f;

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
        int displacementStrengthPropertyId;
        int displacementScalePropertyId;
        int displacementSpeedPropertyId;
        int heightMapInfluencePropertyId;
        int scroll1PropertyId;
        int scroll2PropertyId;
        int mainTexPropertyId;
        int mainTexStPropertyId;
        int baseMapPropertyId;
        int baseMapStPropertyId;
        int opacityPropertyId;
        int colorPropertyId;
        int baseColorPropertyId;
        int shallowColorPropertyId;
        int deepColorPropertyId;
        int horizonColorPropertyId;
        int transmissionColorPropertyId;
        int glimmerColorPropertyId;
        int edgeReflectionPropertyId;
        int reflStrengthPropertyId;
        int transmissionStrengthPropertyId;
        int glimmerIntensityPropertyId;
        int glintIntensityPropertyId;
        int specIntensityPropertyId;
        string cachedPropertyName;
        Vector2 baseTextureScale = Vector2.one;
        Vector2 baseTextureOffset = Vector2.zero;
        Vector2 mainTexBaseScale = Vector2.one;
        Vector2 mainTexBaseOffset = Vector2.zero;
        Vector2 baseMapBaseScale = Vector2.one;
        Vector2 baseMapBaseOffset = Vector2.zero;
        Color materialColor = Color.white;
        Color materialBaseColor = Color.white;
        bool hasTextureTransform;
        bool hasMainTexTransform;
        bool hasBaseMapTransform;
        bool hasColorProperty;
        bool hasBaseColorProperty;
        bool hasSiliqScrollControls;

#if UNITY_EDITOR
        double lastEditorTime;
        double lastEditorPreviewStepTime;
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

        void Reset()
        {
            RebindRendererAndMaterial();
            SyncLookFromMaterial();
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
            displacementStrengthPropertyId = Shader.PropertyToID("_DisplacementStrength");
            displacementScalePropertyId = Shader.PropertyToID("_DisplacementScale");
            displacementSpeedPropertyId = Shader.PropertyToID("_DisplacementSpeed");
            heightMapInfluencePropertyId = Shader.PropertyToID("_HeightMapInfluence");
            scroll1PropertyId = Shader.PropertyToID("_Scroll1");
            scroll2PropertyId = Shader.PropertyToID("_Scroll2");
            mainTexPropertyId = Shader.PropertyToID("_MainTex");
            mainTexStPropertyId = Shader.PropertyToID("_MainTex_ST");
            baseMapPropertyId = Shader.PropertyToID("_BaseMap");
            baseMapStPropertyId = Shader.PropertyToID("_BaseMap_ST");
            opacityPropertyId = Shader.PropertyToID("_Opacity");
            colorPropertyId = Shader.PropertyToID("_Color");
            baseColorPropertyId = Shader.PropertyToID("_BaseColor");
            shallowColorPropertyId = Shader.PropertyToID("_ShallowColor");
            deepColorPropertyId = Shader.PropertyToID("_DeepColor");
            horizonColorPropertyId = Shader.PropertyToID("_HorizonColor");
            transmissionColorPropertyId = Shader.PropertyToID("_TransmissionColor");
            glimmerColorPropertyId = Shader.PropertyToID("_GlimmerColor");
            edgeReflectionPropertyId = Shader.PropertyToID("_EdgeReflection");
            reflStrengthPropertyId = Shader.PropertyToID("_ReflStrength");
            transmissionStrengthPropertyId = Shader.PropertyToID("_TransmissionStrength");
            glimmerIntensityPropertyId = Shader.PropertyToID("_GlimmerIntensity");
            glintIntensityPropertyId = Shader.PropertyToID("_GlintIntensity");
            specIntensityPropertyId = Shader.PropertyToID("_SpecIntensity");
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
            float effectiveSpeed = Mathf.Clamp(speed, 0f, MaxSurfaceSpeed);
            if (effectiveSpeed <= 0f) return;
            if (!IsSelectedForEditModePreview())
            {
                lastEditorTime = 0;
                lastEditorPreviewStepTime = 0;
                return;
            }

            double now = EditorApplication.timeSinceStartup;
            float interval = 1f / Mathf.Max(1, editModePreviewFps);
            if (lastEditorPreviewStepTime > 0 && now - lastEditorPreviewStepTime < interval)
            {
                return;
            }

            float dt = lastEditorTime > 0 ? Mathf.Min((float)(now - lastEditorTime), interval * 2f) : interval;
            lastEditorTime = now;
            lastEditorPreviewStepTime = now;
            Animate(dt);
            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.RepaintAll();
            }
        }

        bool IsSelectedForEditModePreview()
        {
            if (Selection.activeGameObject == gameObject) return true;
            var selected = Selection.gameObjects;
            if (selected == null) return false;
            for (int i = 0; i < selected.Length; i++)
            {
                if (selected[i] == gameObject) return true;
            }
            return false;
        }
#endif

        void OnValidate()
        {
            if (texturePropertyName == null) texturePropertyName = string.Empty;
            speed = Mathf.Clamp(speed, 0f, MaxSurfaceSpeed);
            editModePreviewFps = Mathf.Clamp(editModePreviewFps, 1, 30);
            displacementStrength = Mathf.Clamp(displacementStrength, 0f, 0.5f);
            displacementScale = Mathf.Clamp(displacementScale, 0.05f, 4f);
            displacementSpeed = Mathf.Clamp(displacementSpeed, 0f, 2f);
            heightMapInfluence = Mathf.Clamp01(heightMapInfluence);
            opacity = Mathf.Clamp(opacity, 0.05f, 1f);
            edgeReflection = Mathf.Clamp01(edgeReflection);
            reflectionStrength = Mathf.Clamp01(reflectionStrength);
            transmissionStrength = Mathf.Clamp01(transmissionStrength);
            sparkle = Mathf.Clamp01(sparkle);
            highlightStrength = Mathf.Clamp(highlightStrength, 0f, 2f);
            if (propertyBlock == null) propertyBlock = new MaterialPropertyBlock();
            RebindRendererAndMaterial();
            ApplyProperties(0f);
        }

        /// <summary>テストやカスタムツールから即時反映したい場合に使う。</summary>
        public void ApplyImmediate(float deltaTime = 0f)
        {
            Animate(deltaTime);
        }

        /// <summary>現在のマテリアルから透明・反射系の初期値を読み取り、インスペクタ操作の開始点にする。</summary>
        public void SyncLookFromMaterial()
        {
            RebindRendererAndMaterial();
            if (targetMaterial == null) return;

            if (targetMaterial.HasProperty(opacityPropertyId))
            {
                opacity = Mathf.Clamp01(targetMaterial.GetFloat(opacityPropertyId));
            }
            else if (hasBaseColorProperty)
            {
                opacity = Mathf.Clamp01(materialBaseColor.a);
            }
            else if (hasColorProperty)
            {
                opacity = Mathf.Clamp01(materialColor.a);
            }

            if (targetMaterial.HasProperty(edgeReflectionPropertyId))
            {
                edgeReflection = Mathf.Clamp01(targetMaterial.GetFloat(edgeReflectionPropertyId));
            }
            if (targetMaterial.HasProperty(shallowColorPropertyId))
            {
                shallowColor = targetMaterial.GetColor(shallowColorPropertyId);
            }
            else if (hasBaseColorProperty)
            {
                shallowColor = materialBaseColor;
            }
            else if (hasColorProperty)
            {
                shallowColor = materialColor;
            }
            if (targetMaterial.HasProperty(deepColorPropertyId))
            {
                deepColor = targetMaterial.GetColor(deepColorPropertyId);
            }
            if (targetMaterial.HasProperty(horizonColorPropertyId))
            {
                reflectionColor = targetMaterial.GetColor(horizonColorPropertyId);
            }
            if (targetMaterial.HasProperty(transmissionColorPropertyId))
            {
                transmissionColor = targetMaterial.GetColor(transmissionColorPropertyId);
            }
            if (targetMaterial.HasProperty(glimmerColorPropertyId))
            {
                sparkleColor = targetMaterial.GetColor(glimmerColorPropertyId);
            }
            if (targetMaterial.HasProperty(reflStrengthPropertyId))
            {
                reflectionStrength = Mathf.Clamp01(targetMaterial.GetFloat(reflStrengthPropertyId));
            }
            if (targetMaterial.HasProperty(displacementStrengthPropertyId))
            {
                displacementStrength = Mathf.Clamp(targetMaterial.GetFloat(displacementStrengthPropertyId), 0f, 0.5f);
            }
            if (targetMaterial.HasProperty(displacementScalePropertyId))
            {
                displacementScale = Mathf.Clamp(targetMaterial.GetFloat(displacementScalePropertyId), 0.05f, 4f);
            }
            if (targetMaterial.HasProperty(displacementSpeedPropertyId))
            {
                displacementSpeed = Mathf.Clamp(targetMaterial.GetFloat(displacementSpeedPropertyId), 0f, 2f);
            }
            if (targetMaterial.HasProperty(heightMapInfluencePropertyId))
            {
                heightMapInfluence = Mathf.Clamp01(targetMaterial.GetFloat(heightMapInfluencePropertyId));
            }
            if (targetMaterial.HasProperty(transmissionStrengthPropertyId))
            {
                transmissionStrength = Mathf.Clamp01(targetMaterial.GetFloat(transmissionStrengthPropertyId));
            }
            if (targetMaterial.HasProperty(glimmerIntensityPropertyId))
            {
                sparkle = Mathf.Clamp01(targetMaterial.GetFloat(glimmerIntensityPropertyId));
            }
            if (targetMaterial.HasProperty(specIntensityPropertyId))
            {
                highlightStrength = Mathf.Clamp(targetMaterial.GetFloat(specIntensityPropertyId), 0f, 2f);
            }
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
            hasColorProperty = false;
            hasBaseColorProperty = false;
            hasSiliqScrollControls = false;
            baseTextureScale = Vector2.one;
            baseTextureOffset = Vector2.zero;
            mainTexBaseScale = Vector2.one;
            mainTexBaseOffset = Vector2.zero;
            baseMapBaseScale = Vector2.one;
            baseMapBaseOffset = Vector2.zero;
            materialColor = Color.white;
            materialBaseColor = Color.white;

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

            hasColorProperty = targetMaterial.HasProperty(colorPropertyId);
            if (hasColorProperty)
            {
                materialColor = targetMaterial.GetColor(colorPropertyId);
            }

            hasBaseColorProperty = targetMaterial.HasProperty(baseColorPropertyId);
            if (hasBaseColorProperty)
            {
                materialBaseColor = targetMaterial.GetColor(baseColorPropertyId);
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
            float effectiveSpeed = Mathf.Clamp(speed, 0f, MaxSurfaceSpeed);
            offset += dir * effectiveSpeed * dt;
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
            if (targetMaterial.HasProperty(displacementStrengthPropertyId))
            {
                propertyBlock.SetFloat(displacementStrengthPropertyId, Mathf.Clamp(displacementStrength, 0f, 0.5f));
            }
            if (targetMaterial.HasProperty(displacementScalePropertyId))
            {
                propertyBlock.SetFloat(displacementScalePropertyId, Mathf.Clamp(displacementScale, 0.05f, 4f));
            }
            if (targetMaterial.HasProperty(displacementSpeedPropertyId))
            {
                propertyBlock.SetFloat(displacementSpeedPropertyId, Mathf.Clamp(displacementSpeed, 0f, 2f));
            }
            if (targetMaterial.HasProperty(heightMapInfluencePropertyId))
            {
                propertyBlock.SetFloat(heightMapInfluencePropertyId, Mathf.Clamp01(heightMapInfluence));
            }

            ApplyLookControls();

            // Siliq 独自シェーダーは [NoScaleOffset] の _NormalMap を使うため、
            // テクスチャ ST ではなくシェーダー固有のスクロール/タイリング値を動かす。
            if (hasSiliqScrollControls)
            {
                Vector2 scroll = dir * effectiveSpeed;
                if (targetMaterial.HasProperty(scroll1PropertyId))
                {
                    propertyBlock.SetVector(scroll1PropertyId, new Vector4(scroll.x, scroll.y, 0f, 0f));
                }
                if (targetMaterial.HasProperty(scroll2PropertyId))
                {
                    Vector2 second = new Vector2(-dir.y, dir.x) * effectiveSpeed * 0.73f;
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

        void ApplyLookControls()
        {
            float clampedOpacity = Mathf.Clamp01(opacity);
            if (targetMaterial.HasProperty(opacityPropertyId))
            {
                propertyBlock.SetFloat(opacityPropertyId, clampedOpacity);
            }

            if (hasColorProperty)
            {
                Color c = shallowColor;
                c.a = clampedOpacity;
                propertyBlock.SetColor(colorPropertyId, c);
            }
            if (hasBaseColorProperty)
            {
                Color c = shallowColor;
                c.a = clampedOpacity;
                propertyBlock.SetColor(baseColorPropertyId, c);
            }
            if (targetMaterial.HasProperty(shallowColorPropertyId))
            {
                propertyBlock.SetColor(shallowColorPropertyId, shallowColor);
            }
            if (targetMaterial.HasProperty(deepColorPropertyId))
            {
                propertyBlock.SetColor(deepColorPropertyId, deepColor);
            }
            if (targetMaterial.HasProperty(horizonColorPropertyId))
            {
                propertyBlock.SetColor(horizonColorPropertyId, reflectionColor);
            }
            if (targetMaterial.HasProperty(transmissionColorPropertyId))
            {
                propertyBlock.SetColor(transmissionColorPropertyId, transmissionColor);
            }
            if (targetMaterial.HasProperty(glimmerColorPropertyId))
            {
                propertyBlock.SetColor(glimmerColorPropertyId, sparkleColor);
            }

            if (targetMaterial.HasProperty(edgeReflectionPropertyId))
            {
                propertyBlock.SetFloat(edgeReflectionPropertyId, Mathf.Clamp01(edgeReflection));
            }
            if (targetMaterial.HasProperty(reflStrengthPropertyId))
            {
                propertyBlock.SetFloat(reflStrengthPropertyId, Mathf.Clamp01(reflectionStrength));
            }
            if (targetMaterial.HasProperty(transmissionStrengthPropertyId))
            {
                propertyBlock.SetFloat(transmissionStrengthPropertyId, Mathf.Clamp01(transmissionStrength));
            }
            if (targetMaterial.HasProperty(glimmerIntensityPropertyId))
            {
                propertyBlock.SetFloat(glimmerIntensityPropertyId, Mathf.Clamp01(sparkle));
            }
            if (targetMaterial.HasProperty(glintIntensityPropertyId))
            {
                propertyBlock.SetFloat(glintIntensityPropertyId, Mathf.Clamp01(sparkle) * 2f);
            }
            if (targetMaterial.HasProperty(specIntensityPropertyId))
            {
                propertyBlock.SetFloat(specIntensityPropertyId, Mathf.Clamp(highlightStrength, 0f, 2f));
            }
        }
    }
}
