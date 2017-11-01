Shader "Custom/Terrain" 
{
	Properties 
	{
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Tile Texture", 2DArray) = "white" {}
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.0
	}

	SubShader 
	{
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		#pragma surface surf Standard fullforwardshadows

		#pragma target 3.5

		UNITY_DECLARE_TEX2DARRAY(_MainTex);

		struct Input 
		{
			float3 worldPos;
		};

		half _Glossiness;
		half _Metallic;
		fixed4 _Color;

		UNITY_INSTANCING_CBUFFER_START(Props)
		UNITY_INSTANCING_CBUFFER_END

		void surf (Input IN, inout SurfaceOutputStandard o) 
		{
			float2 uv = IN.worldPos.xz * 0.02;
			float4 c = UNITY_SAMPLE_TEX2DARRAY(_MainTex, float3(uv, 0));

			o.Albedo	 = c.rgb;
			o.Metallic	 = _Metallic;
			o.Smoothness = _Glossiness;
			o.Alpha	     = c.a;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
