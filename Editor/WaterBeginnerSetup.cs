using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Siliq.Water.Editor
{
    /// <summary>
    /// 初心者が最初の水面を迷わず作るための入口。
    /// 最高品質デモ用のメッシュ、マテリアル、Animator、最低限のライト/カメラを一括で用意する。
    /// </summary>
    public static class WaterBeginnerSetup
    {
        const string RootMenu = "Tools/Siliq Water/かんたん作成/";
        const string GameObjectRootMenu = "GameObject/Siliq Water/かんたん作成/";
        const string GeneratedRoot = "Assets/SiliqWater";
        const string GeneratedMeshFolder = GeneratedRoot + "/GeneratedMeshes";
        const string FlagshipMenuPath = "GameObject/Siliq Water/用途別マテリアルを適用/フラッグシップ透明水 (Flagship Crystal)";
        const string CrystalLagoonMenuPath = "GameObject/Siliq Water/用途別マテリアルを適用/クリスタルラグーン (Crystal Lagoon)";
        const string CrystalLagoonHeroMenuPath = "GameObject/Siliq Water/用途別マテリアルを適用/クリスタルラグーン Hero (Crystal Lagoon Hero)";
        internal const string CrystalLagoonHeroCompletePrefabPackagePath = "Packages/com.siliq.water-normalmap/PrebakedPack/Prefabs/PF_Siliq_CrystalLagoon_Hero_Complete.prefab";
        const int PremiumGridSegments = 96;
        const float PremiumGridSize = 20f;

        [MenuItem(RootMenu + "最高品質 Hero 完成セットを配置", false, 0)]
        [MenuItem(GameObjectRootMenu + "最高品質 Hero 完成セットを配置", false, 0)]
        public static void PlaceCrystalLagoonHeroCompletePrefab()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CrystalLagoonHeroCompletePrefabPackagePath);
            if (prefab == null)
            {
                EditorUtility.DisplayDialog(
                    "Siliq Water",
                    "Hero 完成 Prefab が見つかりません。Package が正しく読み込まれているか確認してください。",
                    "OK");
                return;
            }

            var parent = Selection.activeTransform;
            var instance = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
            if (instance == null)
            {
                instance = Object.Instantiate(prefab);
                if (parent != null) instance.transform.SetParent(parent, false);
            }

            Undo.RegisterCreatedObjectUndo(instance, "Siliq 最高品質 Hero 完成セットを配置");
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            Selection.activeGameObject = instance;
            EnsurePreviewLight();
            EnsurePreviewCamera(instance.transform.position);
            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.FrameSelected();
            }

            EditorUtility.DisplayDialog(
                "Siliq Water",
                "最高品質 Hero 完成セットを配置しました。\n\n" +
                "水面、明るい床、Hero 専用 caustics overlay、確認用ライトが一体です。\n" +
                "まずはこの状態で透明感、水底光、斜め反射を確認してください。",
                "OK");
        }

        [MenuItem(RootMenu + "最高品質 Hero 水面を作成", false, 1)]
        [MenuItem(GameObjectRootMenu + "最高品質 Hero 水面を作成", false, 1)]
        public static void CreateCrystalLagoonHeroWater()
        {
            CreatePremiumWater(
                "Siliq Water - Crystal Lagoon Hero",
                "Siliq 最高品質 Hero 水面を作成",
                CrystalLagoonHeroMenuPath,
                "最高品質 Hero 水面を作成しました。\n\n" +
                "Hero 専用 normal / height / caustics を使う、透明感と水底光を最優先した水面です。\n" +
                "まずはこのまま Scene View で、斜めからの反射と水底光を確認してください。\n" +
                "水底の光が強すぎる場合は WaterSurfaceAnimator の「水底の光」を下げてください。");
        }

        [MenuItem(RootMenu + "クリスタルラグーン水面を作成", false, 2)]
        [MenuItem(GameObjectRootMenu + "クリスタルラグーン水面を作成", false, 2)]
        public static void CreateCrystalLagoonWater()
        {
            CreatePremiumWater(
                "Siliq Water - Crystal Lagoon",
                "Siliq クリスタルラグーン水面を作成",
                CrystalLagoonMenuPath,
                "クリスタルラグーン水面を作成しました。\n\n" +
                "透き通った美しさを優先した水面です。最初はこのまま Play / Scene View で確認してください。\n" +
                "水底の光が強すぎる場合は WaterSurfaceAnimator の「水底の光」を下げてください。\n" +
                "高さが見えない場合は、この水面メッシュのまま使ってください。1枚 Quad では実高さが出ません。");
        }

        [MenuItem(RootMenu + "フラッグシップ水面を作成", false, 3)]
        [MenuItem(GameObjectRootMenu + "フラッグシップ水面を作成", false, 3)]
        public static void CreateFlagshipWater()
        {
            CreatePremiumWater(
                "Siliq Water - Flagship Crystal",
                "Siliq フラッグシップ水面を作成",
                FlagshipMenuPath,
                "フラッグシップ水面を作成しました。\n\n" +
                "最初はこのまま Play / Scene View で確認してください。\n" +
                "高さが見えない場合は、この水面メッシュのまま使ってください。1枚 Quad では実高さが出ません。\n" +
                "VRChat Quest / iOS では、透明や反射を重くしすぎず、必要なら Studio で Normal PNG だけを書き出してください。");
        }

        static void CreatePremiumWater(string objectName, string undoName, string menuPath, string dialogBody)
        {
            var parent = Selection.activeTransform;
            var go = new GameObject(objectName);
            Undo.RegisterCreatedObjectUndo(go, undoName);

            if (parent != null)
            {
                Undo.SetTransformParent(go.transform, parent, undoName);
                go.transform.localPosition = Vector3.zero;
            }
            else
            {
                go.transform.position = Vector3.zero;
            }

            var filter = Undo.AddComponent<MeshFilter>(go);
            var renderer = Undo.AddComponent<MeshRenderer>(go);
            filter.sharedMesh = GetOrCreateWaterGridMesh(PremiumGridSegments, PremiumGridSize);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            Selection.activeGameObject = go;
            bool applied = EditorApplication.ExecuteMenuItem(menuPath);
            if (!applied)
            {
                ApplyMinimalFallback(go);
            }

            EnsurePreviewLight();
            EnsurePreviewCamera(go.transform.position);
            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.FrameSelected();
            }

            EditorUtility.DisplayDialog(
                "Siliq Water",
                dialogBody,
                "OK");
        }

        [MenuItem(RootMenu + "選択中の水面を診断して自動修復", false, 20)]
        [MenuItem(GameObjectRootMenu + "選択中の水面を診断して自動修復", false, 20)]
        public static void DiagnoseAndRepairSelection()
        {
            var selected = Selection.gameObjects;
            if (selected == null || selected.Length == 0)
            {
                EditorUtility.DisplayDialog("Siliq Water", "水面にしたいオブジェクトを選択してください。", "OK");
                return;
            }

            int repaired = 0;
            var report = new List<string>();
            foreach (var go in selected)
            {
                if (go == null) continue;
                bool changed = RepairWaterObject(go, report);
                if (changed) repaired++;
            }

            string body = report.Count > 0
                ? string.Join("\n", report)
                : "問題は見つかりませんでした。";

            EditorUtility.DisplayDialog(
                "Siliq Water 診断結果",
                $"修復したオブジェクト: {repaired}\n\n{body}",
                "OK");
        }

        [MenuItem(RootMenu + "選択中の水面を診断して自動修復", true)]
        [MenuItem(GameObjectRootMenu + "選択中の水面を診断して自動修復", true)]
        static bool ValidateDiagnoseAndRepairSelection()
        {
            return Selection.gameObjects != null && Selection.gameObjects.Length > 0;
        }

        internal static bool RepairWaterObject(GameObject go, List<string> report)
        {
            bool changed = false;
            var renderer = go.GetComponent<Renderer>();
            if (renderer == null)
            {
                var filter = go.GetComponent<MeshFilter>();
                if (filter == null)
                {
                    Undo.AddComponent<MeshFilter>(go).sharedMesh = GetOrCreateWaterGridMesh(PremiumGridSegments, PremiumGridSize);
                    report?.Add($"・{go.name}: MeshFilter を追加");
                }
                renderer = Undo.AddComponent<MeshRenderer>(go);
                report?.Add($"・{go.name}: MeshRenderer を追加");
                changed = true;
            }

            var meshFilter = go.GetComponent<MeshFilter>();
            if (meshFilter != null && (meshFilter.sharedMesh == null || meshFilter.sharedMesh.vertexCount < 64))
            {
                Undo.RecordObject(meshFilter, "Siliq 水面メッシュ修復");
                meshFilter.sharedMesh = GetOrCreateWaterGridMesh(PremiumGridSegments, PremiumGridSize);
                EditorUtility.SetDirty(meshFilter);
                report?.Add($"・{go.name}: 高さ表現用の分割メッシュへ交換");
                changed = true;
            }

            if (NeedsWaterMaterialRepair(renderer.sharedMaterial))
            {
                Selection.activeGameObject = go;
                bool applied = EditorApplication.ExecuteMenuItem(CrystalLagoonMenuPath);
                if (!applied)
                {
                    ApplyMinimalFallback(go);
                }
                report?.Add($"・{go.name}: 安全なクリスタルラグーン水マテリアルを適用");
                changed = true;
            }

            var animator = go.GetComponent<WaterSurfaceAnimator>();
            if (animator == null)
            {
                animator = Undo.AddComponent<WaterSurfaceAnimator>(go);
                report?.Add($"・{go.name}: WaterSurfaceAnimator を追加");
                changed = true;
            }

            if (animator != null)
            {
                Undo.RecordObject(animator, "Siliq 水面設定修復");
                var mat = renderer.sharedMaterial;
                animator.texturePropertyName = mat != null && mat.HasProperty("_NormalMap") ? "_NormalMap" : "_BumpMap";
                animator.SyncLookFromMaterial();
                if (animator.speed <= 0f) animator.speed = 0.00025f;
                if (animator.opacity < 0.35f) animator.opacity = 0.52f;
                if (animator.reflectionStrength < 0.8f) animator.reflectionStrength = 1f;
                if (animator.edgeReflection < 0.65f) animator.edgeReflection = 0.88f;
                if (animator.displacementStrength <= 0f) animator.displacementStrength = 0.012f;
                if (animator.displacementSpeed > 0.00008f) animator.displacementSpeed = 0.00008f;
                if (animator.causticsSpeed > 0.000012f) animator.causticsSpeed = 0.000012f;
                animator.ApplyImmediate(0f);
                EditorUtility.SetDirty(animator);
            }

            return changed;
        }

        static bool NeedsWaterMaterialRepair(Material mat)
        {
            if (mat == null || mat.shader == null) return true;
            if (!WaterShaderUtility.IsUsableShaderForCurrentPipeline(mat.shader)) return true;
            if (mat.shader.name.StartsWith("Siliq/Water", System.StringComparison.Ordinal))
            {
                return IsUnsafeFlagshipMaterial(mat);
            }

            bool hasNormalTexture = false;
            if (mat.HasProperty("_NormalMap") && mat.GetTexture("_NormalMap") != null) hasNormalTexture = true;
            if (mat.HasProperty("_BumpMap") && mat.GetTexture("_BumpMap") != null) hasNormalTexture = true;
            if (!hasNormalTexture) return true;
            return IsUnsafeFlagshipMaterial(mat);
        }

        static bool IsUnsafeFlagshipMaterial(Material mat)
        {
            if (mat == null) return false;
            if (mat.name.IndexOf("Flagship", System.StringComparison.OrdinalIgnoreCase) < 0) return false;

            if (mat.HasProperty("_HeightMapInfluence") && mat.GetFloat("_HeightMapInfluence") > 0.05f) return true;
            if (mat.HasProperty("_DisplacementStrength") && mat.GetFloat("_DisplacementStrength") > 0.02f) return true;
            if (mat.HasProperty("_Parallax") && mat.GetFloat("_Parallax") > 0.001f) return true;
            return mat.IsKeywordEnabled("_PARALLAXMAP");
        }

        internal static Mesh GetOrCreateWaterGridMesh(int segments, float size)
        {
            segments = Mathf.Clamp(segments, 2, 192);
            size = Mathf.Max(1f, size);

            EnsureFolder("Assets", "SiliqWater");
            EnsureFolder(GeneratedRoot, "GeneratedMeshes");

            string path = $"{GeneratedMeshFolder}/Siliq_Water_Grid_{segments}_{Mathf.RoundToInt(size)}m.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null) return existing;

            var mesh = BuildWaterGridMesh(segments, size);
            AssetDatabase.CreateAsset(mesh, path);
            AssetDatabase.SaveAssets();
            return mesh;
        }

        internal static Mesh BuildWaterGridMesh(int segments, float size)
        {
            segments = Mathf.Clamp(segments, 2, 192);
            int verticesPerSide = segments + 1;
            var vertices = new Vector3[verticesPerSide * verticesPerSide];
            var normals = new Vector3[vertices.Length];
            var tangents = new Vector4[vertices.Length];
            var uv = new Vector2[vertices.Length];
            var triangles = new int[segments * segments * 6];

            float half = size * 0.5f;
            for (int y = 0; y < verticesPerSide; y++)
            {
                float v = y / (float)segments;
                for (int x = 0; x < verticesPerSide; x++)
                {
                    float u = x / (float)segments;
                    int index = y * verticesPerSide + x;
                    vertices[index] = new Vector3(Mathf.Lerp(-half, half, u), 0f, Mathf.Lerp(-half, half, v));
                    normals[index] = Vector3.up;
                    tangents[index] = new Vector4(1f, 0f, 0f, 1f);
                    uv[index] = new Vector2(u, v);
                }
            }

            int ti = 0;
            for (int y = 0; y < segments; y++)
            {
                for (int x = 0; x < segments; x++)
                {
                    int i0 = y * verticesPerSide + x;
                    int i1 = i0 + 1;
                    int i2 = i0 + verticesPerSide;
                    int i3 = i2 + 1;
                    triangles[ti++] = i0;
                    triangles[ti++] = i2;
                    triangles[ti++] = i1;
                    triangles[ti++] = i1;
                    triangles[ti++] = i2;
                    triangles[ti++] = i3;
                }
            }

            var mesh = new Mesh
            {
                name = $"Siliq Water Grid {segments}x{segments} {size:0.#}m",
            };
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.tangents = tangents;
            mesh.uv = uv;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            return mesh;
        }

        static void ApplyMinimalFallback(GameObject go)
        {
            var renderer = go.GetComponent<Renderer>();
            if (renderer == null) return;

            int shaderIndex = WaterShaderUtility.BestSiliqMaterialShaderIndex();
            if (shaderIndex == WaterShaderUtility.NoMaterialIndex)
            {
                shaderIndex = WaterShaderUtility.BestFallbackMaterialShaderIndex();
            }
            string shaderName = WaterShaderUtility.ShaderNameForMaterialIndex(shaderIndex);
            var shader = WaterShaderUtility.FindUsableShaderForCurrentPipeline(shaderName);
            if (shader == null) return;

            var mat = new Material(shader) { name = "M_Water_Beginner_Flagship" };
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", new Color(0.24f, 0.92f, 1f, 0.52f));
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", new Color(0.24f, 0.92f, 1f, 0.52f));
            if (mat.HasProperty("_Opacity")) mat.SetFloat("_Opacity", 0.52f);
            if (mat.HasProperty("_Clarity")) mat.SetFloat("_Clarity", 0.72f);
            if (mat.HasProperty("_RefractionStrength")) mat.SetFloat("_RefractionStrength", 0.28f);
            if (mat.HasProperty("_BottomGlowStrength")) mat.SetFloat("_BottomGlowStrength", 0.55f);
            if (mat.HasProperty("_DepthTintStrength")) mat.SetFloat("_DepthTintStrength", 0.28f);
            if (mat.HasProperty("_TransmissionStrength")) mat.SetFloat("_TransmissionStrength", 0.72f);
            if (mat.HasProperty("_ReflStrength")) mat.SetFloat("_ReflStrength", 0.92f);
            renderer.sharedMaterial = mat;
        }

        static void EnsurePreviewLight()
        {
            if (Object.FindObjectOfType<Light>() != null) return;

            var lightGo = new GameObject("Siliq Water Preview Light");
            Undo.RegisterCreatedObjectUndo(lightGo, "Siliq プレビューライトを作成");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.25f;
            light.color = new Color(0.92f, 0.98f, 1f, 1f);
            lightGo.transform.rotation = Quaternion.Euler(50f, -28f, 0f);
        }

        static void EnsurePreviewCamera(Vector3 target)
        {
            if (Camera.main != null || Object.FindObjectOfType<Camera>() != null) return;

            var cameraGo = new GameObject("Siliq Water Preview Camera");
            Undo.RegisterCreatedObjectUndo(cameraGo, "Siliq プレビューカメラを作成");
            var camera = cameraGo.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.fieldOfView = 45f;
            cameraGo.tag = "MainCamera";
            cameraGo.transform.position = target + new Vector3(0f, 5.5f, -8f);
            cameraGo.transform.rotation = Quaternion.Euler(58f, 0f, 0f);
        }

        static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }
    }
}
