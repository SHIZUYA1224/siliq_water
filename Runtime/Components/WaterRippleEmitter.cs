using UnityEngine;

namespace Siliq.Water
{
    /// <summary>
    /// 雨粒や環境演出用に、指定 Renderer の範囲内へ同心円状に広がる波紋を発生させる。
    /// 焼き済み波紋ノーマルを横へ流すのではなく、WaterRippleSource と同じ
    /// ワールド座標ベースの波紋バッファへ発生点を書き込む。
    /// </summary>
    [AddComponentMenu("Siliq Water/Water Ripple Emitter")]
    [RequireComponent(typeof(Renderer))]
    public class WaterRippleEmitter : MonoBehaviour
    {
        static readonly int UseRipplesId = Shader.PropertyToID("_UseRipples");
        static readonly int RippleChannelId = Shader.PropertyToID("_RippleChannel");
        static readonly int RippleSpeedId = Shader.PropertyToID("_RippleSpeed");
        static readonly int RippleWidthId = Shader.PropertyToID("_RippleWidth");
        static readonly int RippleLifetimeId = Shader.PropertyToID("_RippleLifetime");
        static readonly int RippleAmplitudeId = Shader.PropertyToID("_RippleAmplitude");
        const string RippleKeyword = "_USE_RIPPLES";

        [Tooltip("波紋を出す範囲として使う Renderer。未指定なら同じ GameObject の Renderer。")]
        public Renderer targetRenderer;

        [Tooltip("波紋を発生させる水面のワールド Y。通常は水面オブジェクトの高さ。")]
        public float waterSurfaceY = 0f;

        [Tooltip("複数の水面を独立させたい場合に使うチャンネル番号。対応する水面マテリアルの _RippleChannel と合わせる。")]
        [Range(0, 7)] public int rippleChannel = 0;

        [Header("発生")]
        [Tooltip("1秒あたりに発生する波紋数。")]
        [Range(0f, 20f)] public float ripplesPerSecond = 1.8f;

        [Tooltip("1回の発生タイミングで同時に出す波紋数。雨面を濃くしたい場合に増やす。")]
        [Range(1, 6)] public int burstCount = 1;

        [Tooltip("Renderer bounds ではなく手動の XZ 範囲を使う。")]
        public bool useManualArea = false;

        [Tooltip("手動範囲の中心。GameObject からのローカルオフセット。")]
        public Vector3 localCenterOffset = Vector3.zero;

        [Tooltip("手動範囲の XZ サイズ。useManualArea が ON の時だけ使用。")]
        public Vector2 manualAreaSize = new Vector2(4f, 4f);

        [Header("波紋")]
        [Tooltip("リングが外側へ広がる速さ。")]
        [Range(0.1f, 10f)] public float rippleSpeed = 2.8f;

        [Tooltip("リング幅。小さいほど細く鋭い輪になる。")]
        [Range(0.03f, 2f)] public float rippleWidth = 0.28f;

        [Tooltip("リングが消えるまでの秒数。")]
        [Range(0.3f, 10f)] public float rippleLifetime = 2.6f;

        [Tooltip("法線に乗せる波紋の強さ。")]
        [Range(0f, 3f)] public float rippleAmplitude = 1.15f;

        [Tooltip("Renderer の MaterialPropertyBlock とマテリアル keyword へ波紋設定を自動反映する。")]
        public bool applyShaderSettings = true;

        MaterialPropertyBlock propertyBlock;
        float accumulator;
        int sequence;

        void Reset()
        {
            targetRenderer = GetComponent<Renderer>();
            waterSurfaceY = transform.position.y;
        }

        void OnEnable()
        {
            if (targetRenderer == null) targetRenderer = GetComponent<Renderer>();
            accumulator = 0f;
            ApplyImmediate();
        }

        void OnValidate()
        {
            if (targetRenderer == null) targetRenderer = GetComponent<Renderer>();
            if (float.IsNaN(waterSurfaceY) || float.IsInfinity(waterSurfaceY)) waterSurfaceY = transform.position.y;
            manualAreaSize.x = Mathf.Max(0.01f, manualAreaSize.x);
            manualAreaSize.y = Mathf.Max(0.01f, manualAreaSize.y);
            ApplyImmediate();
        }

        void Update()
        {
            if (!Application.isPlaying || ripplesPerSecond <= 0f) return;

            float interval = 1f / Mathf.Max(0.01f, ripplesPerSecond);
            accumulator += Time.deltaTime;
            while (accumulator >= interval)
            {
                accumulator -= interval;
                for (int i = 0; i < burstCount; i++)
                {
                    EmitOne();
                }
            }
        }

        /// <summary>現在の設定を Renderer とマテリアルへ即時反映する。</summary>
        public void ApplyImmediate()
        {
            if (!applyShaderSettings || targetRenderer == null) return;
            if (propertyBlock == null) propertyBlock = new MaterialPropertyBlock();

            targetRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetFloat(RippleChannelId, rippleChannel);
            propertyBlock.SetFloat(RippleSpeedId, rippleSpeed);
            propertyBlock.SetFloat(RippleWidthId, rippleWidth);
            propertyBlock.SetFloat(RippleLifetimeId, rippleLifetime);
            propertyBlock.SetFloat(RippleAmplitudeId, rippleAmplitude);
            targetRenderer.SetPropertyBlock(propertyBlock);

            var materials = targetRenderer.sharedMaterials;
            if (materials == null) return;
            for (int i = 0; i < materials.Length; i++)
            {
                var mat = materials[i];
                if (mat == null) continue;
                if (mat.HasProperty(UseRipplesId)) mat.SetFloat(UseRipplesId, 1f);
                mat.EnableKeyword(RippleKeyword);
            }
        }

        void EmitOne()
        {
            WaterRippleSource.SpawnRipple(PickPosition(), rippleChannel);
        }

        Vector3 PickPosition()
        {
            float u = Hash01(sequence++, 17);
            float v = Hash01(sequence++, 53);

            if (!useManualArea && targetRenderer != null)
            {
                Bounds b = targetRenderer.bounds;
                return new Vector3(
                    Mathf.Lerp(b.min.x, b.max.x, u),
                    waterSurfaceY,
                    Mathf.Lerp(b.min.z, b.max.z, v));
            }

            Vector3 center = transform.TransformPoint(localCenterOffset);
            Vector3 right = transform.right * manualAreaSize.x;
            Vector3 forward = transform.forward * manualAreaSize.y;
            Vector3 p = center + (u - 0.5f) * right + (v - 0.5f) * forward;
            p.y = waterSurfaceY;
            return p;
        }

        static float Hash01(int x, int salt)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + salt * 668265263);
                h = (h ^ (h >> 13)) * 1274126177u;
                return ((h ^ (h >> 16)) & 0x00FFFFFF) / 16777215f;
            }
        }
    }
}
