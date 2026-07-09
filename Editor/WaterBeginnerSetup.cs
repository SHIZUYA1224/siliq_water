using System.Collections.Generic;
using Siliq.Water;
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
        internal const string CrystalLagoonHeroCompletePrefabPackagePath = "Packages/com.siliq.water-normalmap/PrebakedPack/Prefabs/PF_Siliq_CrystalLagoon_Hero_Complete.prefab";
        internal const string SunlitPoolCompletePrefabPackagePath = "Packages/com.siliq.water-normalmap/PrebakedPack/Prefabs/PF_Siliq_SunlitPool_Complete.prefab";
        const int PremiumGridSegments = 96;
        const float PremiumGridSize = 20f;

        [MenuItem(RootMenu + "最高品質 Hero 完成セットを配置", false, 0)]
        [MenuItem(GameObjectRootMenu + "最高品質 Hero 完成セットを配置", false, 0)]
        public static void PlaceCrystalLagoonHeroCompletePrefab()
        {
            PlaceCompletePrefab(
                CrystalLagoonHeroCompletePrefabPackagePath,
                "Hero 完成 Prefab",
                "Siliq 最高品質 Hero 完成セットを配置",
                "最高品質 Hero 完成セットを配置しました。\n\n" +
                "水面、明るい床、Hero 専用 caustics overlay、確認用ライトが一体です。\n" +
                "まずはこの状態で透明感、水底光、斜め反射を確認してください。");
        }

        [MenuItem(RootMenu + "透明プール完成セットを配置", false, 1)]
        [MenuItem(GameObjectRootMenu + "透明プール完成セットを配置", false, 1)]
        public static void PlaceSunlitPoolCompletePrefab()
        {
            PlaceCompletePrefab(
                SunlitPoolCompletePrefabPackagePath,
                "透明プール完成 Prefab",
                "Siliq 透明プール完成セットを配置",
                "透明プール完成セットを配置しました。\n\n" +
                "透明プール水面、明るい床、SunlitPool 専用 caustics overlay、確認用ライトが一体です。\n" +
                "室内プールや浅いプールで、水底の広い床光と柔らかい光リボンを確認できます。");
        }

        static void PlaceCompletePrefab(string packagePath, string missingLabel, string undoName, string dialogBody)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(packagePath);
            if (prefab == null)
            {
                EditorUtility.DisplayDialog(
                    "Siliq Water",
                    $"{missingLabel} が見つかりません。Package が正しく読み込まれているか確認してください。",
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

            Undo.RegisterCreatedObjectUndo(instance, undoName);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            Selection.activeGameObject = instance;
            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.FrameSelected();
            }

            EditorUtility.DisplayDialog("Siliq Water", dialogBody, "OK");
        }

        [MenuItem(RootMenu + "最高品質 Hero 水面を作成", false, 2)]
        [MenuItem(GameObjectRootMenu + "最高品質 Hero 水面を作成", false, 2)]
        public static void CreateCrystalLagoonHeroWater()
        {
            CreatePremiumWater(
                "Siliq Water - Crystal Lagoon Hero",
                "Siliq 最高品質 Hero 水面を作成",
                WaterPackQuickApply.ReadyLook.CrystalLagoonHero,
                "最高品質 Hero 水面を作成しました。\n\n" +
                "Hero 専用 normal / height を使う、透明感と反射を優先した水面です。\n" +
                "水底の光は水面ではなく、完成セット内の床/caustics overlay など別メッシュへ付けてください。\n" +
                "高さが見えない場合は、この水面メッシュのまま使ってください。1枚 Quad では実高さが出ません。");
        }

        [MenuItem(RootMenu + "ウォーターテーブル水面を作成", false, 3)]
        [MenuItem(GameObjectRootMenu + "ウォーターテーブル水面を作成", false, 3)]
        public static void CreateWaterTableWater()
        {
            CreatePremiumWater(
                "Siliq Water - Water Table",
                "Siliq ウォーターテーブル水面を作成",
                WaterPackQuickApply.ReadyLook.WaterTable,
                "ウォーターテーブル水面を作成しました。\n\n" +
                "中央から広がる浅いリング波、黒青い水面、強い反射を持つテーブル向け水面です。\n" +
                "飛沫や粒子、塩のような後付け表現は入れていません。\n" +
                "ガラス天板やLEDラインは別オブジェクトで作り、この水面は水面メッシュだけに使ってください。");
        }

        [MenuItem(RootMenu + "クリスタルラグーン水面を作成", false, 4)]
        [MenuItem(GameObjectRootMenu + "クリスタルラグーン水面を作成", false, 4)]
        public static void CreateCrystalLagoonWater()
        {
            CreatePremiumWater(
                "Siliq Water - Crystal Lagoon",
                "Siliq クリスタルラグーン水面を作成",
                WaterPackQuickApply.ReadyLook.CrystalLagoon,
                "クリスタルラグーン水面を作成しました。\n\n" +
                "透き通った美しさを優先した水面です。最初はこのまま Play / Scene View で確認してください。\n" +
                "水底の光は床や水底用の別メッシュへ caustics overlay material を貼って作ります。\n" +
                "高さが見えない場合は、この水面メッシュのまま使ってください。1枚 Quad では実高さが出ません。");
        }

        [MenuItem(RootMenu + "フラッグシップ水面を作成", false, 5)]
        [MenuItem(GameObjectRootMenu + "フラッグシップ水面を作成", false, 5)]
        public static void CreateFlagshipWater()
        {
            CreatePremiumWater(
                "Siliq Water - Flagship Crystal",
                "Siliq フラッグシップ水面を作成",
                WaterPackQuickApply.ReadyLook.FlagshipCrystal,
                "フラッグシップ水面を作成しました。\n\n" +
                "最初はこのまま Play / Scene View で確認してください。\n" +
                "高さが見えない場合は、この水面メッシュのまま使ってください。1枚 Quad では実高さが出ません。\n" +
                "VRChat Quest / iOS では、完成Material画面で対象プラットフォームを選んでください。");
        }

        static void CreatePremiumWater(
            string objectName,
            string undoName,
            WaterPackQuickApply.ReadyLook readyLook,
            string dialogBody)
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
            WaterPackQuickApply.ApplyReadyLookToSelection(readyLook, WaterPackQuickApply.TargetForActiveBuild());

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
                WaterPackQuickApply.ApplyReadyLookToSelection(
                    WaterPackQuickApply.ReadyLook.CrystalLagoon,
                    WaterPackQuickApply.TargetForActiveBuild());
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
                if (animator.speed <= 0f) animator.speed = 0.16f;
                animator.ApplyImmediate(0f);
                EditorUtility.SetDirty(animator);
            }

            return changed;
        }

        static bool NeedsWaterMaterialRepair(Material mat)
        {
            if (mat == null || mat.shader == null) return true;
            if (!WaterShaderUtility.IsUsableShaderForCurrentPipeline(mat.shader)) return true;
            string assetPath = AssetDatabase.GetAssetPath(mat);
            if (!string.IsNullOrEmpty(assetPath) &&
                assetPath.StartsWith("Assets/SiliqWater/GeneratedMaterials/", System.StringComparison.Ordinal))
            {
                return true;
            }
            if (mat.name.StartsWith("M_Water_Look_", System.StringComparison.Ordinal)) return true;
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
