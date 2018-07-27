using System;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Rendering;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    using _ = CoreEditorUtils;
    using CED = CoreEditorDrawer<HDProbeUI, SerializedHDProbe>;

    partial class HDProbeUI
    {
        public static readonly CED.IDrawer Inspector;

        public static readonly CED.IDrawer SectionProbeModeSettings;
        public static readonly CED.IDrawer ProxyVolumeSettings = CED.FoldoutGroup(
                "Proxy Volume",
                (s, d, o) => s.isSectionExpendedProxyVolume,
                FoldoutOption.Indent,
                CED.Action(Drawer_SectionProxySettings)
                );
        public static readonly CED.IDrawer SectionProbeModeBakedSettings = CED.noop;
        //[TODO]
        //public static readonly CED.IDrawer SectionProbeModeCustomSettings = CED.Action(Drawer_SectionProbeModeCustomSettings);
        public static readonly CED.IDrawer SectionProbeModeRealtimeSettings = CED.Action(Drawer_SectionProbeModeRealtimeSettings);
        public static readonly CED.IDrawer SectionBakeButton = CED.Action(Drawer_SectionBakeButton);

        public static readonly CED.IDrawer SectionFoldoutAdditionalSettings = CED.FoldoutGroup(
                "Artistic Settings",
                (s, d, o) => s.isSectionExpendedAdditionalSettings,
                FoldoutOption.Indent,
                CED.Action(Drawer_SectionInfluenceSettings)
                );

        public static readonly CED.IDrawer SectionFoldoutCaptureSettings;

        public static readonly CED.IDrawer SectionCaptureMirrorSettings = CED.Action(Drawer_SectionCaptureMirror);

        //[TODO]
        //public static readonly CED.IDrawer SectionCaptureStaticSettings = CED.Action(Drawer_SectionCaptureStatic);

        static HDProbeUI()
        {
            SectionFoldoutCaptureSettings = CED.FoldoutGroup(
                    "Capture Settings",
                    (s, d, o) => s.isSectionExpandedCaptureSettings,
                    FoldoutOption.Indent,
                    CED.Action(Drawer_SectionCaptureSettings),
                    CED.FadeGroup(
                        (s, d, o, i) =>
                        {
                            switch (i)
                            {
                                default:
                                case 0: return s.isSectionExpandedCaptureMirrorSettings;
                                case 1: return s.isSectionExpandedCaptureStaticSettings;
                            }
                        },
                        FadeOption.None,
                        SectionCaptureMirrorSettings)//,
                        //[TODO]
                        //SectionCaptureStaticSettings)
                    );

            SectionProbeModeSettings = CED.Group(
                    CED.Action(Drawer_FieldCaptureType),
                    CED.FadeGroup(
                        (s, d, o, i) => s.IsSectionExpandedReflectionProbeMode((ReflectionProbeMode)i),
                        FadeOption.Indent,
                        SectionProbeModeBakedSettings,
                        SectionProbeModeRealtimeSettings//,
                        //[TODO]
                        //SectionProbeModeCustomSettings
                        )
                    );

            Inspector = CED.Group(
                    CED.Action(Drawer_Toolbars),
                    CED.space,
                    ProxyVolumeSettings,
                    CED.Select(
                        (s, d, o) => s.influenceVolume,
                        (s, d, o) => d.influenceVolume,
                        InfluenceVolumeUI.SectionFoldoutShapePlanar
                        ),
                    CED.Action(Drawer_DifferentShapeError),
                    SectionFoldoutCaptureSettings,
                    SectionFoldoutAdditionalSettings,
                    CED.Select(
                        (s, d, o) => s.frameSettings,
                        (s, d, o) => d.frameSettings,
                        FrameSettingsUI.Inspector
                        ),
                    CED.space,
                    CED.Action(Drawer_SectionBakeButton)
                    );
        }

        protected const EditMode.SceneViewEditMode EditBaseShape = EditMode.SceneViewEditMode.ReflectionProbeBox;
        protected const EditMode.SceneViewEditMode EditInfluenceShape = EditMode.SceneViewEditMode.GridBox;
        protected const EditMode.SceneViewEditMode EditInfluenceNormalShape = EditMode.SceneViewEditMode.Collider;
        protected const EditMode.SceneViewEditMode EditCenter = EditMode.SceneViewEditMode.ReflectionProbeOrigin;

        //[TODO]
        //static void Drawer_SectionCaptureStatic(HDProbeUI s, SerializedHDProbe d, Editor o)
        //{
        //    EditorGUILayout.PropertyField(d.captureLocalPosition, _.GetContent("Capture Local Position"));

        //    _.DrawMultipleFields(
        //        "Clipping Planes",
        //        new[] { d.captureNearPlane, d.captureFarPlane },
        //        new[] { _.GetContent("Near|The closest point relative to the camera that drawing will occur."), _.GetContent("Far|The furthest point relative to the camera that drawing will occur.\n") });
        //}

        static void Drawer_SectionCaptureMirror(HDProbeUI s, SerializedHDProbe d, Editor o)
        {
            // EditorGUILayout.PropertyField(d.captureMirrorPlaneLocalPosition, _.GetContent("Plane Position"));
            // EditorGUILayout.PropertyField(d.captureMirrorPlaneLocalNormal, _.GetContent("Plane Normal"));
        }

        static void Drawer_DifferentShapeError(HDProbeUI s, SerializedHDProbe d, Editor o)
        {
            var proxy = d.proxyVolumeReference.objectReferenceValue as ReflectionProxyVolumeComponent;
            if (proxy != null
                && (int)proxy.proxyVolume.shape != d.influenceVolume.shape.enumValueIndex
                && proxy.proxyVolume.shape != ProxyShape.Infinite)
            {
                EditorGUILayout.HelpBox(
                    proxyInfluenceShapeMismatchHelpBoxText,
                    MessageType.Error,
                    true
                    );
            }
        }

        protected static void Drawer_SectionCaptureSettings(HDProbeUI s, SerializedHDProbe d, Editor o)
        {
            var hdrp = GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset;
            GUI.enabled = false;
            EditorGUILayout.LabelField(
                _.GetContent("Probe Texture Size (Set By HDRP)"),
                _.GetContent(hdrp.renderPipelineSettings.lightLoopSettings.planarReflectionTextureSize.ToString()),
                EditorStyles.label);
            EditorGUILayout.Toggle(
                _.GetContent("Probe Compression (Set By HDRP)"),
                hdrp.renderPipelineSettings.lightLoopSettings.planarReflectionCacheCompressed);
            GUI.enabled = true;

            //[TODO]
            EditorGUILayout.PropertyField(d.overrideFieldOfView, _.GetContent("Override FOV"));
            if (d.overrideFieldOfView.boolValue)
            {
                ++EditorGUI.indentLevel;
                EditorGUILayout.PropertyField(d.fieldOfViewOverride, _.GetContent("Field Of View"));
                --EditorGUI.indentLevel;
            }
        }

        //[TODO]
        //static void Drawer_SectionProbeModeCustomSettings(HDProbeUI s, SerializedHDProbe d, Editor o)
        //{
        //    d.customTexture.objectReferenceValue = EditorGUILayout.ObjectField(_.GetContent("Capture"), d.customTexture.objectReferenceValue, typeof(Texture), false);
        //    var texture = d.customTexture.objectReferenceValue as Texture;
        //    if (texture != null && texture.dimension != TextureDimension.Tex2D)
        //        EditorGUILayout.HelpBox("Provided Texture is not a 2D Texture, it will be ignored", MessageType.Warning);
        //}

        static void Drawer_SectionBakeButton(HDProbeUI s, SerializedHDProbe d, Editor o)
        {
            //[TODO]optimize
            if (d.target is HDAdditionalReflectionData)
                EditorReflectionSystemGUI.DrawBakeButton((ReflectionProbeMode)d.mode.intValue, d.target.GetComponent<ReflectionProbe>());
            else //PlanarReflectionProbe
                EditorReflectionSystemGUI.DrawBakeButton((ReflectionProbeMode)d.mode.intValue, d.target as PlanarReflectionProbe);
        }

        static void Drawer_SectionProbeModeRealtimeSettings(HDProbeUI s, SerializedHDProbe d, Editor o)
        {
            GUI.enabled = false;
            EditorGUILayout.PropertyField(d.refreshMode, _.GetContent("Refresh Mode"));
            //[TODO]
            //EditorGUILayout.PropertyField(d.capturePositionMode, _.GetContent("Capture Position Mode"));
            GUI.enabled = true;
        }

        static void Drawer_SectionProxySettings(HDProbeUI s, SerializedHDProbe d, Editor o)
        {
            EditorGUILayout.PropertyField(d.proxyVolumeReference, _.GetContent("Reference"));

            if (d.proxyVolumeReference.objectReferenceValue != null)
            {
                var proxy = (ReflectionProxyVolumeComponent)d.proxyVolumeReference.objectReferenceValue;
                if ((int)proxy.proxyVolume.shape != d.influenceVolume.shape.enumValueIndex
                    && proxy.proxyVolume.shape != ProxyShape.Infinite)
                    EditorGUILayout.HelpBox(
                        proxyInfluenceShapeMismatchHelpBoxText,
                        MessageType.Error,
                        true
                        );
            }
            else
            {
                EditorGUILayout.HelpBox(
                        noProxyHelpBoxText,
                        MessageType.Info,
                        true
                        );
            }
        }

        static void Drawer_SectionInfluenceSettings(HDProbeUI s, SerializedHDProbe d, Editor o)
        {
            EditorGUILayout.PropertyField(d.weight, weightContent);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(d.multiplier, multiplierContent);
            if (EditorGUI.EndChangeCheck())
                d.multiplier.floatValue = Mathf.Max(0.0f, d.multiplier.floatValue);
        }

        static void Drawer_FieldCaptureType(HDProbeUI s, SerializedHDProbe d, Editor o)
        {
            GUI.enabled = false;
            EditorGUILayout.PropertyField(d.mode, fieldCaptureTypeContent);
            GUI.enabled = true;
        }



        protected enum ToolBar { Influence, Capture }
        protected ToolBar[] toolBars = new ToolBar[] { ToolBar.Influence, ToolBar.Capture };

        static readonly EditMode.SceneViewEditMode[] k_InfluenceToolbar_SceneViewEditModes =
        {
            EditBaseShape,
            EditInfluenceShape,
            EditInfluenceNormalShape,
        };

        static readonly EditMode.SceneViewEditMode[] k_CaptureToolbar_SceneViewEditModes =
        {
            EditCenter
        };

        static void Drawer_Toolbars(HDProbeUI s, SerializedHDProbe d, Editor o)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUI.changed = false;

            foreach(ToolBar toolBar in s.toolBars)
            {
                switch (toolBar)
                {
                    case ToolBar.Influence:
                        EditMode.DoInspectorToolbar(k_InfluenceToolbar_SceneViewEditModes, influenceToolbar_Contents, GetBoundsGetter(o), o);
                        break;
                    case ToolBar.Capture:
                        EditMode.DoInspectorToolbar(k_CaptureToolbar_SceneViewEditModes, captureToolbar_Contents, GetBoundsGetter(o), o);
                        break;
                }
                GUILayout.FlexibleSpace();
            }

            GUILayout.EndHorizontal();
        }


        static public void Drawer_ToolBarButton(int buttonIndex, Editor owner, params GUILayoutOption[] styles)
        {
            if (GUILayout.Button(influenceToolbar_Contents[buttonIndex], styles))
            {
                EditMode.ChangeEditMode(k_InfluenceToolbar_SceneViewEditModes[buttonIndex], GetBoundsGetter(owner)(), owner);
            }
        }

        static Func<Bounds> GetBoundsGetter(Editor o)
        {
            return () =>
                {
                    var bounds = new Bounds();
                    foreach (Component targetObject in o.targets)
                    {
                        var rp = targetObject.transform;
                        var b = rp.position;
                        bounds.Encapsulate(b);
                    }
                    return bounds;
                };
        }
    }
}
