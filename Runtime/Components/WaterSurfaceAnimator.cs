using UnityEngine;

namespace Siliq.Water
{
    /// <summary>
    /// マテリアルのノーマルマップ (または任意のテクスチャプロパティ) を
    /// 時間経過でスクロールさせるだけの軽量コンポーネント。
    /// Standard / URP Lit / VRChat Mobile / 同梱シェーダーなど、
    /// 対象プロパティを持つシェーダーであれば動作する。
    /// </summary>
    [AddComponentMenu("Siliq Water/Water Surface Animator")]
    [RequireComponent(typeof(Renderer))]
    public class WaterSurfaceAnimator : MonoBehaviour
    {
        [Tooltip("スクロールさせるテクスチャのプロパティ名。Standard/URP Lit/VRChat Mobile は _BumpMap、Siliq 独自シェーダーは _NormalMap。")]
        public string texturePropertyName = "_BumpMap";

        [Tooltip("1秒あたりのUVオフセット量。値を変えると流れる向き・速さが変わる。")]
        public Vector2 scrollSpeed = new Vector2(0.02f, 0.013f);

        Renderer targetRenderer;
        Material materialInstance;
        Vector2 offset;
        int propertyId;

        void Awake()
        {
            targetRenderer = GetComponent<Renderer>();
            propertyId = Shader.PropertyToID(texturePropertyName);
        }

        void OnEnable()
        {
            // マテリアルをインスタンス化し、他オブジェクトと共有しているアセットを変更しないようにする
            materialInstance = targetRenderer.material;
        }

        void Update()
        {
            offset += scrollSpeed * Time.deltaTime;
            offset.x %= 1f;
            offset.y %= 1f;

            if (materialInstance.HasProperty(propertyId))
            {
                materialInstance.SetTextureOffset(propertyId, offset);
            }
        }
    }
}
