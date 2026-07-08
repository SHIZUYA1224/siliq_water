#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Siliq.Water.Editor
{
    internal static class WaterPrebakedAssetRebuilder
    {
        const string TextureRoot = "Packages/com.siliq.water-normalmap/PrebakedPack/Textures";

        [MenuItem("Tools/Siliq Water/開発/Crystal Lagoon 水底光を再生成")]
        public static void RebuildCrystalLagoonCaustics()
        {
            WriteCaustics(WaterMapPresets.CrystalLagoonCaustics(), $"{TextureRoot}/Water_Caustics_CrystalLagoon_01.png");
            WriteCaustics(WaterMapPresets.CrystalLagoonHeroCaustics(), $"{TextureRoot}/Water_Caustics_CrystalLagoon_Hero_01.png");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        static void WriteCaustics(WaterMapSettings settings, string packagePath)
        {
            int size = Mathf.Max(256, settings.resolution);
            var colors = WaterMapCore.GenerateColors(settings, WaterMapType.Caustics, size, 0f);
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false, true)
            {
                wrapMode = TextureWrapMode.Repeat,
                name = Path.GetFileNameWithoutExtension(packagePath),
            };

            tex.SetPixels32(WaterMapCore.Quantize(colors));
            tex.Apply(false, false);

            string fullPath = ResolvePackagePath(packagePath);
            File.WriteAllBytes(fullPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(packagePath, ImportAssetOptions.ForceUpdate);
        }

        static string ResolvePackagePath(string packagePath)
        {
            var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssetPath(packagePath);
            if (packageInfo == null || string.IsNullOrEmpty(packageInfo.resolvedPath))
            {
                return Path.GetFullPath(packagePath);
            }

            string prefix = $"Packages/{packageInfo.name}/";
            string relativePath = packagePath.StartsWith(prefix)
                ? packagePath.Substring(prefix.Length)
                : Path.GetFileName(packagePath);
            return Path.Combine(packageInfo.resolvedPath, relativePath);
        }
    }
}
#endif
