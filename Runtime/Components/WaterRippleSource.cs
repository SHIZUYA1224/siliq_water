using System.Collections.Generic;
using UnityEngine;

namespace Siliq.Water
{
    /// <summary>
    /// 水面に近づいた/触れたコライダー (アバターの当たり判定など) を検知し、
    /// Siliq Water シェーダーの波紋 (リップル) をワールド座標で発生させる。
    ///
    /// 対象の水面シェーダー側で「触れた時の波紋を有効化」(_USE_RIPPLES) を ON にしておくこと。
    /// このコンポーネントが書き込むグローバル配列 (_SiliqRipplePoints) は、
    /// シーン内のすべての Siliq 水面マテリアルで共有される。
    ///
    /// 【重要】VRChat にアップロードしたワールドでは通常の MonoBehaviour は実行されない
    /// (Udon のみが動作する) ため、このコンポーネントは Unity エディタでのテストや、
    /// VRChat 以外の一般的な Unity アプリでのみ機能する。VRChat ワールドで実際に
    /// アバターの波紋を再現するには、別途 UdonSharp 版
    /// (Runtime/VRChatSupport~/WaterRippleSourceUdon.cs.txt) を使用すること。
    /// </summary>
    [AddComponentMenu("Siliq Water/Water Ripple Source (Trigger)")]
    public class WaterRippleSource : MonoBehaviour
    {
        const int MaxRipples = 8;
        static readonly int RipplePointsId = Shader.PropertyToID("_SiliqRipplePoints");
        static readonly Vector4[] Points = new Vector4[MaxRipples];
        static int nextSlot = 0;

        [Tooltip("この Y 座標以下に来たコライダーを『水に入った』とみなす。水面オブジェクトのワールド Y に合わせること。")]
        public float waterSurfaceY = 0f;

        [Tooltip("水面よりこの高さ以上上にいる場合は無視する (誤爆防止のマージン)。")]
        public float ignoreAboveMargin = 0.5f;

        [Tooltip("同じコライダーが連続で波紋を発生させる最短間隔 (秒)。水に浸かったままだと波紋が出っぱなしになるのを防ぐ。")]
        public float minIntervalPerCollider = 0.4f;

        [Tooltip("トリガーに反応するタグ。空にするとタグを問わず全てのコライダーが対象。")]
        public string requiredTag = "";

        readonly Dictionary<Collider, float> lastSplashTime = new Dictionary<Collider, float>();

        void OnTriggerEnter(Collider other) => TrySplash(other);
        void OnTriggerStay(Collider other) => TrySplash(other);

        void TrySplash(Collider other)
        {
            if (!string.IsNullOrEmpty(requiredTag) && !other.CompareTag(requiredTag)) return;

            float now = Time.time;
            if (lastSplashTime.TryGetValue(other, out float last) && now - last < minIntervalPerCollider) return;

            Vector3 pos = other.ClosestPoint(transform.position);
            if (pos.y > waterSurfaceY + ignoreAboveMargin) return;

            lastSplashTime[other] = now;
            SpawnRipple(pos);
        }

        /// <summary>指定ワールド座標に波紋を発生させる。スクリプトから直接呼んでもよい。</summary>
        public static void SpawnRipple(Vector3 worldPos)
        {
            Points[nextSlot] = new Vector4(worldPos.x, worldPos.z, Time.time, 0f);
            nextSlot = (nextSlot + 1) % MaxRipples;
            Shader.SetGlobalVectorArray(RipplePointsId, Points);
        }
    }
}
