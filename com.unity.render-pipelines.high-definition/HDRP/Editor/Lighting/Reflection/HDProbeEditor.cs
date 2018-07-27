using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    using _ = CoreEditorUtils;

    [CustomEditorForRenderPipeline(typeof(HDProbe), typeof(HDRenderPipelineAsset), true)]
    [CanEditMultipleObjects]
    class HDProbeEditor : Editor
    {
        static Dictionary<HDProbe, HDProbeUI> s_StateMap = new Dictionary<HDProbe, HDProbeUI>();

        public static bool TryGetUIStateFor(HDProbe p, out HDProbeUI r)
        {
            return s_StateMap.TryGetValue(p, out r);
        }



        SerializedHDProbe m_SerializedAsset;
        HDProbeUI m_UIState = new HDProbeUI();
        HDProbeUI[] m_UIHandleState;
        protected HDProbe[] m_TypedTargets;

        void OnEnable()
        {
            m_SerializedAsset = new SerializedPlanarReflectionProbe(serializedObject);
            m_UIState.Reset(m_SerializedAsset, Repaint);

            m_TypedTargets = new HDProbe[targets.Length];
            m_UIHandleState = new HDProbeUI[m_TypedTargets.Length];
            for (var i = 0; i < m_TypedTargets.Length; i++)
            {
                m_TypedTargets[i] = (HDProbe)targets[i];
                m_UIHandleState[i] = new HDProbeUI();
                m_UIHandleState[i].Reset(m_SerializedAsset, null);

                s_StateMap[m_TypedTargets[i]] = m_UIHandleState[i];
            }
        }

        void OnDisable()
        {
            for (var i = 0; i < m_TypedTargets.Length; i++)
                s_StateMap.Remove(m_TypedTargets[i]);
        }

        public override void OnInspectorGUI()
        {
            var s = m_UIState;
            var d = m_SerializedAsset;
            var o = this;

            s.Update();
            d.Update();

            HDProbeUI.Inspector.Draw(s, d, o);

            d.Apply();
        }

        void OnSceneGUI()
        {
            for (var i = 0; i < m_TypedTargets.Length; i++)
            {
                m_UIHandleState[i].Update();
                m_UIHandleState[i].influenceVolume.showInfluenceHandles = m_UIState.influenceVolume.isSectionExpandedShape.target;
                m_UIHandleState[i].showCaptureHandles = m_UIState.isSectionExpandedCaptureSettings.target;
                HDProbeUI.DrawHandles(m_UIHandleState[i], m_TypedTargets[i], this);
            }

            //[TODO]check
            //SceneViewOverlay_Window(_.GetContent("Planar Probe"), OnOverlayGUI, -100, target);
        }


    }
}
