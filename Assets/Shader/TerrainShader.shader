Shader "X4/TerrainShader"
{
	Properties
	{
		_TerrainMap("TerrainMap", 2D) = "white" {}
		_TillMap("TillMap", 2D) = "white" {}

		_TerrainDiffuse("TerrainDiffuse", 2D) = "white" {}
		_UV("UV", Color) = (1,1,1,1)
		_TILEDNUMEBR("TILLN", Float) = 4.0
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
			#pragma enable_d3d11_debug_symbols
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			sampler2D _TerrainMap;
			sampler2D _TillMap;
			sampler2D _TerrainDiffuse;
			float4 _TerrainMap_ST;
			float4 _UV;
			float _TILEDNUMEBR;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _TerrainMap);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				// sample the texture
				float4 uv = tex2D(_TerrainMap, i.uv);
				
				int a = trunc(uv.a * 16.0f);
				//float4 vAllSame = saturate(uv.z - 98.0f); // we've added 100 to first if all IDs are same
				//uv -= vAllSame * 100.0f;

				float percents = 1.0 / _TILEDNUMEBR;
				int u = a % _TILEDNUMEBR;
				int v = a / _TILEDNUMEBR;
				float2 newuv = ((u, v) + i.uv) * percents;

				//float4 IndexV = trunc((uv + 0.5f) / _TILEDNUMEBR);
				//float4 IndexU = trunc(uv - (IndexV * _TILEDNUMEBR) + 0.5f);

				fixed4 col = tex2D(_TillMap, newuv);
				fixed4 diffusecol = tex2D(_TerrainDiffuse, i.uv);
				//return fixed4(pos / 8.0, pos / 8.0, pos/ 8.0,1.0);
				return col*0 + diffusecol;
			}
			ENDCG
		}
	}
}
