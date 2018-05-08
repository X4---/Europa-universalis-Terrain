Shader "X4/TerrainShader"
{
	Properties
	{
		_TerrainMap("TerrainMap", 2D) = "white" {}
		_TillMap("TillMap", 2D) = "white" {}

		_TerrainDiffuse("TerrainDiffuse", 2D) = "white" {}
		_UV("UV", Color) = (1,1,1,1)
		_TILEDNUMEBR("TILLN", Float) = 4.0
		_A("A", Float) = 60.0
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
				float2 uv2 : TEXCOORD1;
				float4 vertex : SV_POSITION;
			};

			sampler2D _TerrainMap;
			sampler2D _TillMap;
			sampler2D _TerrainDiffuse;
			float4 _TerrainMap_ST;
			float4 _UV;
			float _TILEDNUMEBR;
			float _A;
			
			float MAP_SIZE_X = 5632;
			float MAP_SIZE_Y = 2048;
			float MAP_POW2_X = 6;
			float MAP_POW2_Y = 6;
			float TERRAIN_TILE_FREQ = 1/1024;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _TerrainMap);
				o.uv2 = o.uv;
				o.uv2.y = 1.0 - o.uv2.y;
				o.uv2 *= float2(MAP_POW2_X, MAP_POW2_Y);
				return o;
			}
			
			float4 frag (v2f i) : SV_Target
			{
				// sample the texture
				float4 uv = tex2D(_TerrainMap, i.uv);
				
				float4 temp = uv * 255.0f;
				
				float vAllSame = saturate(uv.z - 98.0f); // we've added 100 to first if all IDs are same
				temp -= vAllSame * 100.0f;

				float4 IndexV = trunc((temp + 0.5f) / _TILEDNUMEBR); // 0 - 64
				float4 IndexU = trunc(temp - (IndexV * _TILEDNUMEBR) + 0.5f); // 0-3

				float2 vTileRepeat = i.uv2 * TERRAIN_TILE_FREQ;
				vTileRepeat.x *= MAP_SIZE_X / MAP_SIZE_Y;

				float2 tt = (IndexU.a, IndexV.a);

				float4 newBaseUV = ( (float2(IndexU.a, IndexV.a)) / _TILEDNUMEBR, 0.0f, 0);
				float4 moidfyUV = float4(frac(vTileRepeat) / _TILEDNUMEBR, 0, 0);

				newBaseUV += moidfyUV;

				float4 BaseUVColor = tex2D(_TillMap, newBaseUV);
				

				float c = vTileRepeat.x / _A;
				float4 cClolor = float4(c, c, c, c);

				float4 uv2Color = float4(i.uv2.y, i.uv2.y, i.uv2.y, i.uv2.x);
				float4 uv2SampleColor = tex2D(_TerrainMap, moidfyUV);

				float4 uvColor = float4(uv.w, uv.w, uv.w, uv.w);
				float4 tempColor = float4(temp.w, temp.w, temp.w, temp.w) /255.0f;

				float a = IndexU.w;
				float b = IndexV.w - _A;
				float4 IndexColor = (0.25*a, 0.25*a, 0.25*a, 0.25 *a);
				float4 IndexUColor = (b, b, b, b);

				return IndexColor;

				//fixed4 col = tex2D(_TillMap, newuv);
				//fixed4 diffusecol = tex2D(_TerrainDiffuse, i.uv);
				//return fixed4(pos / 8.0, pos / 8.0, pos/ 8.0,1.0);
				//return col*0 + diffusecol;
			}
			ENDCG
		}
	}
}
