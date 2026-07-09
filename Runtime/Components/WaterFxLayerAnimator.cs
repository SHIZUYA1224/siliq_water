using UnityEngine;

namespace Siliq.Water
{
    /// <summary>
    /// Drives the lightweight FX shader in Edit Mode so placed water FX do not look frozen before Play.
    /// Runtime playback still uses the shader's built-in _Time path.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class WaterFxLayerAnimator : MonoBehaviour
    {
        static readonly int ManualTimeId = Shader.PropertyToID("_ManualTime");

        public Renderer targetRenderer;
        public bool animateInEditMode = true;
        [Range(0f, 4f)] public float previewTimeScale = 1f;
        public float previewTimeOffset;
        [Range(1, 60)] public int editModePreviewFps = 18;

        MaterialPropertyBlock block;
        float lastPreviewUpdateTime = -999f;

        void OnEnable()
        {
            ApplyCurrentTime(true);
        }

        void OnDisable()
        {
            ApplyManualTime(0f);
        }

        void OnValidate()
        {
            if (previewTimeScale < 0f) previewTimeScale = 0f;
            editModePreviewFps = Mathf.Clamp(editModePreviewFps, 1, 60);
            ApplyCurrentTime(true);
        }

        void Update()
        {
            ApplyCurrentTime(false);
        }

        void OnWillRenderObject()
        {
            ApplyCurrentTime(false);
        }

        public void ApplyImmediate(float previewTime)
        {
            ApplyManualTime(previewTimeOffset + Mathf.Max(0f, previewTime) * previewTimeScale);
        }

        void ApplyCurrentTime(bool force)
        {
            if (Application.isPlaying || !animateInEditMode)
            {
                ApplyManualTime(0f);
                return;
            }

            float now = Time.realtimeSinceStartup;
            float interval = 1f / Mathf.Max(1, editModePreviewFps);
            if (!force && now - lastPreviewUpdateTime < interval) return;

            lastPreviewUpdateTime = now;
            ApplyManualTime(previewTimeOffset + now * previewTimeScale);
        }

        void ApplyManualTime(float manualTime)
        {
            var rendererToUse = targetRenderer != null ? targetRenderer : GetComponent<Renderer>();
            if (rendererToUse == null) return;

            if (block == null) block = new MaterialPropertyBlock();
            rendererToUse.GetPropertyBlock(block);
            block.SetFloat(ManualTimeId, manualTime);
            rendererToUse.SetPropertyBlock(block);
        }
    }
}
