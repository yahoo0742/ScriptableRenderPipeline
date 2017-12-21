using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public partial class Decal
    {
        // Main structure that store the user data (i.e user input of master node in material graph)
        [GenerateHLSL(PackingRules.Exact, false, true, 10000)]
        public struct DecalSurfaceData
        {
            [SurfaceDataAttributes("Base Color", false, true)]
            public Vector4 baseColor;
            [SurfaceDataAttributes("Normal", true)]
            public Vector4 normalWS;
            [SurfaceDataAttributes("Mask", true)]
            public Vector4 mask;
        };

		[GenerateHLSL(PackingRules.Exact, false, true, 10001)]
        public struct DecalSurfaceDataVS
        {
			[SurfaceDataAttributes("Height", true)]
			public float height; 
		}

        [GenerateHLSL(PackingRules.Exact)]
        public enum DBufferMaterial
        {            
            Count = 4
        };

        //-----------------------------------------------------------------------------
        // DBuffer management
        //-----------------------------------------------------------------------------

		// should this be combined into common class shared with Lit.cs???
       static public int GetMaterialDBufferCount() { return (int)DBufferMaterial.Count; }

	   static RenderTextureFormat[Count] m_RTFormat = { RenderTextureFormat.ARGB32, RenderTextureFormat.ARGB32, RenderTextureFormat.ARGB32, RenderTextureFormat.RFloat };
	   static RenderTextureReadWrite[Count] m_RTReadWrite = { RenderTextureReadWrite.sRGB, RenderTextureReadWrite.Linear, RenderTextureReadWrite.Linear, RenderTextureReadWrite.Linear};

       static public void GetMaterialDBufferDescription(out RenderTextureFormat[] RTFormat, out RenderTextureReadWrite[] RTReadWrite)
       {
            RTFormat = m_RTFormat;
            RTReadWrite = m_RTReadWrite;
       }
    }
}
