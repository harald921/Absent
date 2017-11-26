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
			float2 uv2_ShineTex : TEXCOORD1;

			float3 worldPos;
		};

		half _Glossiness;
		half _Metallic;
		fixed4 _Color;

		UNITY_INSTANCING_CBUFFER_START(Props)
		UNITY_INSTANCING_CBUFFER_END

		void surf (Input IN, inout SurfaceOutputStandard o) 
		{
			// THE WHTIE LINE IS A PROBLEM IN THE TEXTURE, NOT THE SHADER

			float4 color      = UNITY_SAMPLE_TEX2DARRAY(_MainTex, float3(IN.worldPos.xz * 1, IN.uv2_ShineTex.x));
			float4 blendColor = UNITY_SAMPLE_TEX2DARRAY(_MainTex, float3(IN.worldPos.xz * 1, IN.uv2_ShineTex.y));

			float2 direction = float2(0, 0);
			float blendWeight = 1;

			float xWeightedWorldPos = IN.worldPos.x * blendWeight;// - (0.5 * (fmod(IN.worldPos.x, 2)));
			float yWeightedWorldPos = IN.worldPos.z * blendWeight;// - (0.5 * (fmod(IN.worldPos.z, 2)));


			float xLerpValue = xWeightedWorldPos * direction.x;
			float yLerpValue = yWeightedWorldPos * direction.y;

			float lerpValue = xLerpValue + yLerpValue;

			// Make sure lerpValue is positive
			lerpValue = abs(lerpValue);

			// Modulo blendWeight
			lerpValue = fmod(lerpValue, blendWeight * 2);
			
			// Clamp between 0 and 1
			lerpValue = clamp(lerpValue, 0, 1);

			color.rgb = lerp(color.rgb, blendColor.rgb, lerpValue);

			o.Albedo	 = color.rgb;
			o.Metallic	 = _Metallic;
			o.Smoothness = _Glossiness;
		}
		ENDCG
	}
	FallBack "Diffuse"
}

// Give each tile a color
// (R * 255) + (G * 255) + (B * 255) = Texture Atlas Index to sample from