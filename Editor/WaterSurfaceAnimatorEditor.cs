using Siliq.Water;
using UnityEditor;
using UnityEngine;

namespace Siliq.Water.Editor
{
    [CustomEditor(typeof(WaterSurfaceAnimator))]
    [CanEditMultipleObjects]
    public class WaterSurfaceAnimatorEditor : UnityEditor.Editor
    {
        SerializedProperty texturePropertyName;
        SerializedProperty materialSlot;
        SerializedProperty animateInEditMode;
        SerializedProperty editModePreviewFps;
        SerializedProperty directionDegrees;
        SerializedProperty speed;
        SerializedProperty strength;
        SerializedProperty tiling;
        SerializedProperty displacementStrength;
        SerializedProperty displacementScale;
        SerializedProperty displacementSpeed;
        SerializedProperty heightMapInfluence;
        SerializedProperty shallowColor;
        SerializedProperty deepColor;
        SerializedProperty reflectionColor;
        SerializedProperty transmissionColor;
        SerializedProperty sparkleColor;
        SerializedProperty opacity;
        SerializedProperty clarity;
        SerializedProperty edgeReflection;
        SerializedProperty reflectionStrength;
        SerializedProperty reflectionPatternStrength;
        SerializedProperty reflectionPatternScale;
        SerializedProperty transmissionStrength;
        SerializedProperty sparkle;
        SerializedProperty highlightStrength;
        SerializedProperty causticsStrength;
        SerializedProperty causticsScale;
        SerializedProperty causticsSpeed;
        SerializedProperty causticsFocus;
        SerializedProperty causticsPrismStrength;
        SerializedProperty bottomLightStrength;
        SerializedProperty causticsTint;

        void OnEnable()
        {
            texturePropertyName = serializedObject.FindProperty("texturePropertyName");
            materialSlot = serializedObject.FindProperty("materialSlot");
            animateInEditMode = serializedObject.FindProperty("animateInEditMode");
            editModePreviewFps = serializedObject.FindProperty("editModePreviewFps");
            directionDegrees = serializedObject.FindProperty("directionDegrees");
            speed = serializedObject.FindProperty("speed");
            strength = serializedObject.FindProperty("strength");
            tiling = serializedObject.FindProperty("tiling");
            displacementStrength = serializedObject.FindProperty("displacementStrength");
            displacementScale = serializedObject.FindProperty("displacementScale");
            displacementSpeed = serializedObject.FindProperty("displacementSpeed");
            heightMapInfluence = serializedObject.FindProperty("heightMapInfluence");
            shallowColor = serializedObject.FindProperty("shallowColor");
            deepColor = serializedObject.FindProperty("deepColor");
            reflectionColor = serializedObject.FindProperty("reflectionColor");
            transmissionColor = serializedObject.FindProperty("transmissionColor");
            sparkleColor = serializedObject.FindProperty("sparkleColor");
            opacity = serializedObject.FindProperty("opacity");
            clarity = serializedObject.FindProperty("clarity");
            edgeReflection = serializedObject.FindProperty("edgeReflection");
            reflectionStrength = serializedObject.FindProperty("reflectionStrength");
            reflectionPatternStrength = serializedObject.FindProperty("reflectionPatternStrength");
            reflectionPatternScale = serializedObject.FindProperty("reflectionPatternScale");
            transmissionStrength = serializedObject.FindProperty("transmissionStrength");
            sparkle = serializedObject.FindProperty("sparkle");
            highlightStrength = serializedObject.FindProperty("highlightStrength");
            causticsStrength = serializedObject.FindProperty("causticsStrength");
            causticsScale = serializedObject.FindProperty("causticsScale");
            causticsSpeed = serializedObject.FindProperty("causticsSpeed");
            causticsFocus = serializedObject.FindProperty("causticsFocus");
            causticsPrismStrength = serializedObject.FindProperty("causticsPrismStrength");
            bottomLightStrength = serializedObject.FindProperty("bottomLightStrength");
            causticsTint = serializedObject.FindProperty("causticsTint");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUI.BeginChangeCheck();
            DrawRendererSection();
            DrawMotionSection();
            DrawHeightSection();
            DrawColorSection();
            DrawOpticalSection();
            DrawCausticsSection();
            bool changed = EditorGUI.EndChangeCheck();

            serializedObject.ApplyModifiedProperties();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("マテリアルから読み込む"))
                {
                    SyncTargetsFromMaterial();
                    changed = false;
                }
                if (GUILayout.Button("今すぐ反映"))
                {
                    ApplyTargets();
                    changed = false;
                }
            }

            if (changed)
            {
                ApplyTargets();
            }
        }

        void DrawRendererSection()
        {
            EditorGUILayout.LabelField("対象", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(texturePropertyName, new GUIContent("Texture Property"));
            EditorGUILayout.PropertyField(materialSlot, new GUIContent("Material Slot"));
            EditorGUILayout.PropertyField(animateInEditMode, new GUIContent("Animate In Edit Mode"));
            using (new EditorGUI.DisabledScope(!animateInEditMode.boolValue))
            {
                EditorGUILayout.PropertyField(editModePreviewFps, new GUIContent("Edit Mode Preview Fps"));
            }
            EditorGUILayout.Space(4f);
        }

        void DrawMotionSection()
        {
            EditorGUILayout.LabelField("動き", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(directionDegrees, new GUIContent("方向"));
            EditorGUILayout.PropertyField(speed, new GUIContent("速さ"));
            EditorGUILayout.PropertyField(strength, new GUIContent("凹凸"));
            EditorGUILayout.PropertyField(tiling, new GUIContent("模様の細かさ"));
            EditorGUILayout.Space(4f);
        }

        void DrawColorSection()
        {
            EditorGUILayout.LabelField("色", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(shallowColor, new GUIContent("明るい水色"));
            EditorGUILayout.PropertyField(deepColor, new GUIContent("深い水色"));
            EditorGUILayout.PropertyField(reflectionColor, new GUIContent("反射色"));
            EditorGUILayout.PropertyField(transmissionColor, new GUIContent("透過光"));
            EditorGUILayout.PropertyField(sparkleColor, new GUIContent("きらめき色"));
            EditorGUILayout.Space(4f);
        }

        void DrawHeightSection()
        {
            EditorGUILayout.LabelField("高さ", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(displacementStrength, new GUIContent("高さ"));
            EditorGUILayout.PropertyField(displacementScale, new GUIContent("高さの波長"));
            EditorGUILayout.PropertyField(displacementSpeed, new GUIContent("高さの速度"));
            EditorGUILayout.PropertyField(heightMapInfluence, new GUIContent("ハイトマップの影響"));
            EditorGUILayout.Space(4f);
        }

        void DrawOpticalSection()
        {
            EditorGUILayout.LabelField("透明・反射", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(opacity, new GUIContent("不透明度"));
            EditorGUILayout.PropertyField(clarity, new GUIContent("透明な抜け感"));
            EditorGUILayout.PropertyField(edgeReflection, new GUIContent("輪郭反射"));
            EditorGUILayout.PropertyField(reflectionStrength, new GUIContent("反射量"));
            EditorGUILayout.PropertyField(reflectionPatternStrength, new GUIContent("反射パターン"));
            EditorGUILayout.PropertyField(reflectionPatternScale, new GUIContent("反射パターンの大きさ"));
            EditorGUILayout.PropertyField(transmissionStrength, new GUIContent("透過光量"));
            EditorGUILayout.PropertyField(sparkle, new GUIContent("きらめき"));
            EditorGUILayout.PropertyField(highlightStrength, new GUIContent("ハイライト"));
            EditorGUILayout.Space(4f);
        }

        void DrawCausticsSection()
        {
            EditorGUILayout.LabelField("水底の光", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(causticsStrength, new GUIContent("光の強さ"));
            EditorGUILayout.PropertyField(causticsScale, new GUIContent("光の細かさ"));
            EditorGUILayout.PropertyField(causticsSpeed, new GUIContent("光の速度"));
            EditorGUILayout.PropertyField(causticsFocus, new GUIContent("光の焦点"));
            EditorGUILayout.PropertyField(causticsPrismStrength, new GUIContent("光の色分散"));
            EditorGUILayout.PropertyField(bottomLightStrength, new GUIContent("水底光の透け"));
            EditorGUILayout.PropertyField(causticsTint, new GUIContent("光の色"));
            EditorGUILayout.Space(4f);
        }

        void SyncTargetsFromMaterial()
        {
            foreach (Object obj in targets)
            {
                var animator = obj as WaterSurfaceAnimator;
                if (animator == null) continue;

                Undo.RecordObject(animator, "水面の見た目を読み込む");
                animator.SyncLookFromMaterial();
                animator.ApplyImmediate(0f);
                EditorUtility.SetDirty(animator);
            }
            serializedObject.Update();
            SceneView.RepaintAll();
        }

        void ApplyTargets()
        {
            foreach (Object obj in targets)
            {
                var animator = obj as WaterSurfaceAnimator;
                if (animator == null) continue;

                animator.ApplyImmediate(0f);
                EditorUtility.SetDirty(animator);
            }
            SceneView.RepaintAll();
        }
    }
}
