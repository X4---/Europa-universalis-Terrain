Shader "X4/TerrainWater"
{
	Properties
	{
		HeightMap ("HeightMap", 2D) = "white" {}
		ReflectionCubeMap("ReflectionCubeMap", Cube) = "white" {}

		WaterColor("WaterColor", 2D) = "white" {}
		WaterNoise("WaterNoise", 2D) = "white" {}
		WaterRefraction("WaterRefraction", 2D) = "white" {}

		IceDiffuse("IceDiffuse", 2D) = "white" {}
		IceNormal("IceNormal", 2D) = "white" {}
		
		FoWTexture("FoWTexture", 2D) = "white"{}
		FoWDiffuse("FoWDiffuse", 2D) = "white"{}

		ShadowMap("ShadowMap", 2D) = "white" {}
		TITexture("TITexture", 2D) = "white" {}
		ProvinceColorMap("ProvinceColorMap", 2D) = "black"{}

	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			#include "TerrainConstant.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.vertex.xy / (MAP_SIZE_X, MAP_SIZE_Y);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				// sample the texture
				//fixed4 col = tex2D(_MainTex, i.uv);
				
				return 1;
			}
			ENDCG
		}
	}
}
