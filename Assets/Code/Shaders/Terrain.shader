Shader "Custom/Terrain" 
{
	Properties 
	{
		_MainTex ("Tile Texture", 2DArray) = "white" {}
		_ShineTex("Shine (RGB)", 2D) = "white" {}
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.0
	}

	SubShader 
	{
		Tags { "RenderType"="Opaque" }
		
		CGPROGRAM
		#pragma surface surf Standard fullforwardshadows

		#pragma target 3.5
		
		UNITY_DECLARE_TEX2DARRAY(_MainTex);

		struct Input 
		{
			float2 uv_MainTex   : TEXCOORD0;
			float2 uv2_ShineTex : TEXCOORD1;
		};

		half _Glossiness;
		half _Metallic;
		fixed4 _Color;

		UNITY_INSTANCING_CBUFFER_START(Props)
		UNITY_INSTANCING_CBUFFER_END

		void surf (Input IN, inout SurfaceOutputStandard o) 
		{
			float4 c = UNITY_SAMPLE_TEX2DARRAY(_MainTex, float3(IN.uv_MainTex, IN.uv2_ShineTex.x));

			o.Albedo	 = c.rgb;
			o.Metallic	 = _Metallic;
			o.Smoothness = _Glossiness;
			o.Alpha	     = c.a;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
