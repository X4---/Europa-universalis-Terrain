Shader "X4/TerrainUnderWater"
{
	Properties
	{
		_HeightNormal("HeightNormal", 2D) = "white" {}
		_TerrainUnderWater("TerrainUnderWater", 2D) = "white"{}
		_TerrainUnderWaterNoise("TerrainUnderWaterNoise", 2D) = "white"{}

		_UV("UV", Color) = (1,1,1,1)
		_TILEDNUMEBR("TILLN", Float) = 4.0
		_A("A", Float) = 60.0

		_vBorderLookup_HeightScale_UseMultisample_SeasonLerp("SeasonFactor", Range(0,1)) = 0.35
		vFoWOpacity_Time("vFowOpacity Time", Vector) = (1, 1, 1, 1)
	}
		SubShader
	{
		Tags{ "RenderType" = "Opaque"  "TerrainTag" = "Use"}
		LOD 100

		Pass
	{
		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag
		#pragma enable_d3d11_debug_symbols
		#include "UnityCG.cginc"
		#include "TerrainConstant.cginc"

		struct appdata
		{
			float4 vertex : POSITION;
			float2 uv : TEXCOORD0;
		};

		struct v2f
		{
			float2 uv : TEXCOORD0;
			float2 uv2 : TEXCOORD1;
			float3 prepos : TEXCOORD2;
			float4 vertex : SV_POSITION;
		};

		sampler2D _TerrainUnderWater;
		sampler2D _TerrainUnderWaterNoise;

		v2f vert(appdata v)
		{
			v2f o;
			o.vertex = UnityObjectToClipPos(v.vertex);

			float2 offset = float2(0.5 / MAP_SIZE_X, 0.5 / MAP_SIZE_Y);

			o.uv = v.uv;//+offset;
			o.uv2 = o.uv;

			o.uv.y = 1 - o.uv.y;
			o.prepos = mul(unity_ObjectToWorld, v.vertex);

			return o;
		}

		float4 frag(v2f Input) : SV_Target
		{
			clip(WATER_HEIGHT - Input.prepos.y + TERRAIN_WATER_CLIP_HEIGHT);
			float3 normal = normalize(tex2D(_HeightNormal,Input.uv2).rbg - 0.5f);
			float3 diffuseColor = tex2D(_TerrainUnderWaterNoise, Input.uv2 * float2((MAP_SIZE_X / 32.0f), (MAP_SIZE_Y / 32.0f))).rgb;
			float3 waterColorTint = tex2D(_TerrainUnderWater, Input.uv2).rgb;

			float vMin = 17.0f;
			float vMax = 18.5f;
			float vWaterFog = saturate(1.0f - (Input.prepos.y - vMin) / (vMax - vMin));

			diffuseColor = lerp(diffuseColor, waterColorTint, vWaterFog);
			float vFog = saturate(Input.prepos.y * Input.prepos.y * Input.prepos.y * WATER_HEIGHT_RECP_SQUARED * WATER_HEIGHT_RECP);
			float3 vOut = CalculateMapLighting(diffuseColor, normal * vFog);

			return float4(vOut, 1.0f);
		}
		ENDCG
	}
	}
	
}
