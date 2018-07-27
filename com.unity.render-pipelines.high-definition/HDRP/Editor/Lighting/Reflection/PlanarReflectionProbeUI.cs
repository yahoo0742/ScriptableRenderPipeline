namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    partial class PlanarReflectionProbeUI : HDProbeUI
    {
        new SerializedPlanarReflectionProbe data;

        internal PlanarReflectionProbeUI()
        {
            toolBars = new[] { ToolBar.Influence };
            data = base.data as SerializedPlanarReflectionProbe;
        }

        public override void Update()
        {
            isSectionExpandedCaptureMirrorSettings.target = data.isMirrored;
            isSectionExpandedCaptureStaticSettings.target = !data.isMirrored;
            base.Update();
        }
    }
}
