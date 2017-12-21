using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    public class DecalUI : ShaderGUI
    {
        protected static class Styles
        {
            public static string InputsText = "Inputs";

            public static GUIContent baseColorText = new GUIContent("Base Color + Blend", "Albedo (RGB) and Blend Factor (A)");
            public static GUIContent normalMapText = new GUIContent("Normal Map", "Normal Map (BC7/BC5/DXT5(nm))");
			public static GUIContent maskMapText = new GUIContent("Mask Map - M(R), AO(G), D(B), S(A)", "Mask map");
			public static GUIContent heightMapText = new GUIContent("Height Map (R)", "Height Map.\nFor floating point textures, min, max and base value should be 0, 1 and 0.");
			public static GUIContent heightMapCenterText = new GUIContent("Height Map Base", "Base of the heightmap in the texture (between 0 and 1)");
			public static GUIContent heightMapMinText = new GUIContent("Height Min (cm)", "Minimum value in the heightmap (in centimeters)");
			public static GUIContent heightMapMaxText = new GUIContent("Height Max (cm)", "Maximum value in the heightmap (in centimeters)");
            public static GUIContent decalBlendText = new GUIContent("Decal Blend", "Whole decal blend");
        }

        protected MaterialProperty baseColorMap = new MaterialProperty();
        protected const string kBaseColorMap = "_BaseColorMap";

        protected MaterialProperty normalMap = new MaterialProperty();
        protected const string kNormalMap = "_NormalMap";

		protected MaterialProperty maskMap = new MaterialProperty();
		protected const string kMaskMap = "_MaskMap";

		protected MaterialProperty heightMap = new MaterialProperty();
		protected const string kHeightMap = "_HeightMap";
		protected MaterialProperty heightAmplitude = new MaterialProperty();
		protected const string kHeightAmplitude = "_HeightAmplitude";
		protected MaterialProperty heightCenter = new MaterialProperty();
		protected const string kHeightCenter = "_HeightCenter";
		protected MaterialProperty heightMin = new MaterialProperty();
		protected const string kHeightMin = "_HeightMin";
		protected MaterialProperty heightMax = new MaterialProperty();
		protected const string kHeightMax = "_HeightMax";
        protected MaterialProperty decalBlend = new MaterialProperty();
        protected const string kDecalBlend = "_DecalBlend";

        protected MaterialEditor m_MaterialEditor;

        // This is call by the inspector
        void FindMaterialProperties(MaterialProperty[] props)
        {
            baseColorMap = FindProperty(kBaseColorMap, props);
            normalMap = FindProperty(kNormalMap, props);
			maskMap = FindProperty(kMaskMap, props);
			heightMap = FindProperty(kHeightMap, props);
			heightAmplitude = FindProperty(kHeightAmplitude, props);
			heightCenter = FindProperty(kHeightCenter, props);
			heightMin = FindProperty(kHeightMin, props);
			heightMax = FindProperty(kHeightMax, props);
			decalBlend = FindProperty(kDecalBlend, props);
        }

        // All Setup Keyword functions must be static. It allow to create script to automatically update the shaders with a script if code change
        static public void SetupMaterialKeywordsAndPass(Material material)
        {
            CoreUtils.SetKeyword(material, "_COLORMAP", material.GetTexture(kBaseColorMap));
            CoreUtils.SetKeyword(material, "_NORMALMAP", material.GetTexture(kNormalMap));
			CoreUtils.SetKeyword(material, "_MASKMAP", material.GetTexture(kMaskMap));
			CoreUtils.SetKeyword(material, "_HEIGHTMAP", material.GetTexture(kHeightMap));
        }

        protected void SetupMaterialKeywordsAndPassInternal(Material material)
        {
            SetupMaterialKeywordsAndPass(material);
        }

        public void ShaderPropertiesGUI(Material material)
        {
            // Use default labelWidth
            EditorGUIUtility.labelWidth = 0f;

            // Detect any changes to the material
            EditorGUI.BeginChangeCheck();
            {
                EditorGUILayout.LabelField(Styles.InputsText, EditorStyles.boldLabel);

                EditorGUI.indentLevel++;

                m_MaterialEditor.TexturePropertySingleLine(Styles.baseColorText, baseColorMap);
                m_MaterialEditor.TexturePropertySingleLine(Styles.normalMapText, normalMap);
				m_MaterialEditor.TexturePropertySingleLine(Styles.maskMapText, maskMap);
				m_MaterialEditor.TexturePropertySingleLine(Styles.heightMapText, heightMap);
				if ((!heightMap.hasMixedValue) && (heightMap.textureValue != null))
				{
					EditorGUI.indentLevel++;
					m_MaterialEditor.ShaderProperty(heightMin, Styles.heightMapMinText);
					m_MaterialEditor.ShaderProperty(heightMax, Styles.heightMapMaxText);
					m_MaterialEditor.ShaderProperty(heightCenter, Styles.heightMapCenterText);
					EditorGUI.showMixedValue = false;
					EditorGUI.indentLevel--;
				}
                m_MaterialEditor.ShaderProperty(decalBlend, Styles.decalBlendText);

                EditorGUI.indentLevel--;
                
            }

            if (EditorGUI.EndChangeCheck())
            {
                foreach (var obj in m_MaterialEditor.targets)
                    SetupMaterialKeywordsAndPassInternal((Material)obj);
            }
        }

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] props)
        {
            m_MaterialEditor = materialEditor;
            // We should always do this call at the beginning
            m_MaterialEditor.serializedObject.Update();

            FindMaterialProperties(props);

            Material material = materialEditor.target as Material;
            ShaderPropertiesGUI(material);

            // We should always do this call at the end
            m_MaterialEditor.serializedObject.ApplyModifiedProperties();
        }
    }
}
