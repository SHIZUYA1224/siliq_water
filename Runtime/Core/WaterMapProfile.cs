using UnityEngine;

namespace Siliq.Water
{
    /// <summary>
    /// 生成設定をアセットとして保存・共有するためのプロファイル。
    /// エディタウィンドウから保存 / 読み込みできるほか、
    /// ランタイムで参照して WaterMapCore.BakeTexture に渡すこともできる。
    /// </summary>
    [CreateAssetMenu(fileName = "WaterMapProfile", menuName = "Siliq Water/水面マッププロファイル")]
    public class WaterMapProfile : ScriptableObject
    {
        public WaterMapSettings settings = new WaterMapSettings();

        /// <summary>指定マップをこのプロファイル設定でベイクする (ランタイム可)。</summary>
        public Texture2D Bake(WaterMapType mapType, int size, float t = 0f, bool highPrecision = false)
        {
            return WaterMapCore.BakeTexture(settings, mapType, size, t, highPrecision);
        }
    }
}
