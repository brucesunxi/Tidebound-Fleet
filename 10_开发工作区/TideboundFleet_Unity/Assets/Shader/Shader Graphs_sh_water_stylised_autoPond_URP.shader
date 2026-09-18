Shader "Shader Graphs/sh_water_stylised_autoPond_URP" {
	Properties {
		Vector1_68B44540 ("Metallic", Float) = 0
		Vector1_E280F477 ("Smoothness", Float) = 0.5
		[NoScaleOffset] Texture2D_7255C4CD ("Water texture", 2D) = "white" {}
		Vector1_965BA36C ("- Water size (WorldCoord)", Float) = 0.75
		Color_8B0C7504 ("- Water color", Vector) = (0,0.4020718,0.7169812,0)
		Vector1_894FE3F1 ("- Water color/texture mix", Range(0, 1)) = 0.15
		Vector1_4ED49B17 ("- Water depth", Float) = 6.05
		Color_7CCE133F ("- Water depth color", Vector) = (0.1811143,0.4712874,0.6981132,0)
		[NoScaleOffset] Texture2D_87DF1992 ("Distorsion texture", 2D) = "white" {}
		Vector1_E7F1FCE4 ("- Distorsion size (WorldCoord)", Float) = 0.2
		Vector1_2BC1DA0F ("- Distorsion amount", Float) = 0.2
		Vector1_5CCADC29 ("- Distorsion speed", Float) = 0.38
		Vector1_52E31C97 ("- Distorsion amplitude", Float) = 0.1
		Vector1_33D6ED88 ("- Bump amount", Range(0, 5)) = 0
		[NoScaleOffset] Texture2D_5917C0F1 ("Foam texture", 2D) = "white" {}
		Vector1_B2C28A3D ("- Foam size (WorldCoord)", Float) = 1.5
		Color_DD5D9566 ("- Foam color", Vector) = (1,1,1,0)
		Vector1_E54BAFA9 ("- Foam speed", Float) = 0.2
		Vector1_41FAE320 ("- Foam thickness", Float) = 1.05
		Vector1_490079ED ("- Foam wideness", Float) = 5
		Vector1_8280C9BC ("- Foam cut (min.)", Float) = 0.4
		Vector1_A465D6C0 ("- Foam cut (max)", Float) = 0.6
		[HideInInspector] _QueueOffset ("_QueueOffset", Float) = 0
		[HideInInspector] _QueueControl ("_QueueControl", Float) = -1
		[HideInInspector] [NoScaleOffset] unity_Lightmaps ("unity_Lightmaps", 2DArray) = "" {}
		[HideInInspector] [NoScaleOffset] unity_LightmapsInd ("unity_LightmapsInd", 2DArray) = "" {}
		[HideInInspector] [NoScaleOffset] unity_ShadowMasks ("unity_ShadowMasks", 2DArray) = "" {}
	}
	//DummyShaderTextExporter
	SubShader{
		Tags { "RenderType" = "Opaque" }
		LOD 200
		CGPROGRAM
#pragma surface surf Standard
#pragma target 3.0

		struct Input
		{
			float2 uv_MainTex;
		};

		void surf(Input IN, inout SurfaceOutputStandard o)
		{
			o.Albedo = 1;
		}
		ENDCG
	}
	Fallback "Hidden/Shader Graph/FallbackError"
	//CustomEditor "UnityEditor.ShaderGraph.GenericShaderGraphMaterialGUI"
}