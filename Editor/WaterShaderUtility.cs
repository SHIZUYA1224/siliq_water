using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Siliq.Water.Editor
{
    internal static class WaterShaderUtility
    {
        internal const int NoMaterialIndex = 0;
        internal const int StandardIndex = 1;
        internal const int UrpLitIndex = 2;
        internal const int SiliqMobileIndex = 3;
        internal const int SiliqUrpIndex = 4;

        internal const string StandardShaderName = "Standard";
        internal const string UrpLitShaderName = "Universal Render Pipeline/Lit";
        internal const string SiliqMobileShaderName = "Siliq/Water Mobile (Quest)";
        internal const string SiliqUrpShaderName = "Siliq/Water URP";

        const string UniversalCoreIncludePath = "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl";
        const string InternalErrorShaderName = "Hidden/InternalErrorShader";

        enum PipelineMode
        {
            BuiltIn,
            Universal,
            OtherScriptablePipeline,
        }

        internal static string ShaderNameForMaterialIndex(int index)
        {
            switch (index)
            {
                case StandardIndex: return StandardShaderName;
                case UrpLitIndex: return UrpLitShaderName;
                case SiliqMobileIndex: return SiliqMobileShaderName;
                case SiliqUrpIndex: return SiliqUrpShaderName;
                default: return null;
            }
        }

        internal static Shader FindUsableShaderForCurrentPipeline(string shaderName)
        {
            var shader = string.IsNullOrEmpty(shaderName) ? null : Shader.Find(shaderName);
            return IsUsableShaderForCurrentPipeline(shader) ? shader : null;
        }

        internal static bool IsUsableShaderForCurrentPipeline(Shader shader)
        {
            if (!IsUsableShaderObject(shader)) return false;
            return IsShaderNameCompatibleWithCurrentPipeline(shader.name);
        }

        internal static bool IsUsableShaderObject(Shader shader)
        {
            return shader != null && shader.isSupported && shader.name != InternalErrorShaderName;
        }

        internal static bool IsMaterialCompatibleWithCurrentPipeline(Material mat)
        {
            return mat != null && IsUsableShaderForCurrentPipeline(mat.shader);
        }

        internal static int BestSiliqMaterialShaderIndex()
        {
            PipelineMode mode = GetPipelineMode();
            if (mode == PipelineMode.Universal)
            {
                if (FindUsableShaderForCurrentPipeline(SiliqUrpShaderName) != null) return SiliqUrpIndex;
                if (FindUsableShaderForCurrentPipeline(UrpLitShaderName) != null) return UrpLitIndex;
                return NoMaterialIndex;
            }

            if (mode == PipelineMode.BuiltIn)
            {
                if (FindUsableShaderForCurrentPipeline(SiliqMobileShaderName) != null) return SiliqMobileIndex;
                if (FindUsableShaderForCurrentPipeline(StandardShaderName) != null) return StandardIndex;
            }

            return NoMaterialIndex;
        }

        internal static int BestFallbackMaterialShaderIndex()
        {
            PipelineMode mode = GetPipelineMode();
            if (mode == PipelineMode.Universal)
            {
                return FindUsableShaderForCurrentPipeline(UrpLitShaderName) != null ? UrpLitIndex : NoMaterialIndex;
            }

            if (mode == PipelineMode.BuiltIn)
            {
                return FindUsableShaderForCurrentPipeline(StandardShaderName) != null ? StandardIndex : NoMaterialIndex;
            }

            return NoMaterialIndex;
        }

        internal static int ResolveSafeMaterialShaderIndex(int requestedIndex)
        {
            string requestedShaderName = ShaderNameForMaterialIndex(requestedIndex);
            if (FindUsableShaderForCurrentPipeline(requestedShaderName) != null) return requestedIndex;

            int siliqIndex = BestSiliqMaterialShaderIndex();
            if (siliqIndex != NoMaterialIndex) return siliqIndex;
            return BestFallbackMaterialShaderIndex();
        }

        internal static bool IsUniversalPipelineActive()
        {
            return GetPipelineMode() == PipelineMode.Universal;
        }

        internal static bool HasUniversalPipelinePackage()
        {
            if (!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(UniversalCoreIncludePath))) return true;

            var pipeline = GraphicsSettings.renderPipelineAsset;
            if (pipeline != null && pipeline.GetType().FullName.Contains("UnityEngine.Rendering.Universal")) return true;

            return System.Type.GetType("UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset, Unity.RenderPipelines.Universal.Runtime") != null;
        }

        static bool IsShaderNameCompatibleWithCurrentPipeline(string shaderName)
        {
            if (string.IsNullOrEmpty(shaderName)) return false;

            PipelineMode mode = GetPipelineMode();
            if (mode == PipelineMode.Universal)
            {
                if (!HasUniversalPipelinePackage()) return false;
                return shaderName == UrpLitShaderName || shaderName == SiliqUrpShaderName;
            }

            if (mode == PipelineMode.BuiltIn)
            {
                return shaderName == StandardShaderName || shaderName == SiliqMobileShaderName;
            }

            return false;
        }

        static PipelineMode GetPipelineMode()
        {
            var pipeline = GraphicsSettings.renderPipelineAsset;
            if (pipeline == null) return PipelineMode.BuiltIn;

            string typeName = pipeline.GetType().Name;
            if (typeName.Contains("Universal") || typeName.Contains("URP"))
            {
                return PipelineMode.Universal;
            }

            return PipelineMode.OtherScriptablePipeline;
        }
    }
}
