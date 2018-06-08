Shader "X4/TerrainShader"
{
	Properties
	{
		_TerrainDiffuse("TerrainDiffuse", 2D) = "white" {}
		_TerrainIDMap("TerrainIDMap", 2D) = "white" {}
		_HeightNormal("HeightNormal", 2D) = "white" {}

		_TerrainNormal("TerrainNormal", 2D) = "white" {}

		_TerrainColorTint("TerrainColorTint", 2D) = "white"{}
		_TerrainColorTintSecond("TerrainColorTintSecond", 2D) = "white"{}

		FoWTexture("FoWTexture", 2D) = "white"{}
		TITexture("TITexture", 2D) = "white"{}
		FoWDiffuse("FoWDiffuse", 2D) = "white" {}
		OccupationMask("OccupationMask", 2D) = "white"{}
		ProvinceSecondaryColorMap("ProvinceSecondaryColorMap", 2D) = "white"{}
		ProvinceSecondaryColorMapPoint("ProvinceSecondaryColorMapPoint", 2D) = "white"{}

		_UV("UV", Color) = (1,1,1,1)
		_TILEDNUMEBR("TILLN", Float) = 4.0
		_A("A", Float) = 60.0

		_vBorderLookup_HeightScale_UseMultisample_SeasonLerp("SeasonFactor", Range(0,1)) = 0.35
		vFoWOpacity_Time("vFowOpacity Time", Vector) = (1, 1, 1, 1)
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
				float3 prepos : TEXCOORD2;
				float4 vertex : SV_POSITION;
			};

			sampler2D _TerrainDiffuse;
			sampler2D _TerrainIDMap;
			sampler2D _HeightNormal;
			sampler2D _TerrainColorTint;
			sampler2D _TerrainColorTintSecond;
			sampler2D _TerrainNormal;
			sampler2D FoWTexture;
			sampler2D TITexture;
			sampler2D FoWDiffuse;
			sampler2D OccupationMask;
			sampler2D ProvinceSecondaryColorMap;
			sampler2D ProvinceSecondaryColorMapPoint;


			float _A;

			float MAP_SIZE_X = 5632;
			float MAP_SIZE_Y = 2048;
			float MAP_POW2_X = 1;
			float MAP_POW2_Y = 1;
			float FOW_POW2_X = 1;
			float FOW_POW2_Y = 1;
			//float NUM_TILES = 4;
			//float TERRAIN_TILE_FREQ = 128;
			//float TEXELS_PER_TILE = 2048 / 4;
			//float ATLAS_TEXEL_POW2_EXPONENT = 11;
			float _vBorderLookup_HeightScale_UseMultisample_SeasonLerp;
			float4 vFoWOpacity_Time;

			float4 vLightDir = (0, 1, 0, 0);
			float4 AMBIENT = (0.5, 0.5, 0.5, 1);
			float4 LIGHT_DIFFUSE = (1, 1, 1, 1);
			float LIGHT_INTENSITY = 1;
			float4 MAP_AMBIENT = 1;
			float4 MAP_LIGHT_DIFFUSE = (1, 1, 1, 1);
			float4 MAP_LIGHT_INTENSITY = (1, 1, 1, 1);

			const static float SNOW_START_HEIGHT = 18.0f;
			const static float SNOW_RIDGE_START_HEIGHT = 22.0f;
			const static float SNOW_NORMAL_START = 0.7f;
			const static float3 SNOW_COLOR = float3(0.7f, 0.7f, 0.7f);
			const static float3 SNOW_WATER_COLOR = float3(0.5f, 0.7f, 0.7f);

			const static  float3 GREYIFY = float3(0.212671, 0.715160, 0.072169);
			const static  float NUM_TILES = 4.0f;
			const static  float TEXELS_PER_TILE = 512.0f;
			const static  float ATLAS_TEXEL_POW2_EXPONENT = 11.0f;
			const static  float TERRAIN_WATER_CLIP_HEIGHT = 3.0f;
			const static  float TERRAIN_UNDERWATER_CLIP_HEIGHT = 3.0f;

			//const static float TERRAIN_WATER_CLIP_HEIGHT = 3.0f;

			static const float 	TERRAIN_TILE_FREQ = 128.0f; // constants.fxh

			const static float WATER_HEIGHT = 19.0f;
			const static float WATER_HEIGHT_RECP = 1.0f / WATER_HEIGHT;
			const static float WATER_HEIGHT_RECP_SQUARED = WATER_HEIGHT_RECP * WATER_HEIGHT_RECP;
			const static float vTimeScale = 0.5f / 300.0f;

			float3 CalculateLighting(float3 vColor, float3 vNormal, float3 vLightDirection, float vAmbient, float3 vLightDiffuse, float vLightIntensity)
			{
				float NdotL = dot(vNormal, -vLightDirection);

				float vHalfLambert = NdotL * 0.5f + 0.5f;
				vHalfLambert *= vHalfLambert;

				vHalfLambert = vAmbient + (1.0f - vAmbient) * vHalfLambert;

				return  saturate(vHalfLambert * vColor * vLightDiffuse * vLightIntensity);
			}

			float3 CalculateLighting(float3 vColor, float3 vNormal)
			{
				return CalculateLighting(vColor, vNormal, vLightDir, AMBIENT, LIGHT_DIFFUSE, LIGHT_INTENSITY);
			}

			float3 CalculateMapLighting(float3 vColor, float3 vNormal)
			{
				return CalculateLighting(vColor, vNormal, vLightDir, MAP_AMBIENT, MAP_LIGHT_DIFFUSE, MAP_LIGHT_INTENSITY);
			}

			float3 calculate_secondary_compressed(float2 uv, float3 vColor, float2 vPos)
			{
				float4 vMask = tex2D(OccupationMask, vPos / 8.0).rgba;

				float4 vPointSample = tex2D(ProvinceSecondaryColorMapPoint, uv);

				float4 vLinearSample = tex2D(ProvinceSecondaryColorMap, uv);
				//Use color of point sample and transparency of linear sample
				float4 vSecondary = float4(
					vPointSample.rgb,
					vLinearSample.a);

				const int nDivisor = 6;
				int3 vTest = int3(vSecondary.rgb * 255.0);

				int3 RedParts = int3(vTest / (nDivisor * nDivisor));
				vTest -= RedParts * (nDivisor * nDivisor);

				int3 GreenParts = int3(vTest / nDivisor);
				vTest -= GreenParts * nDivisor;

				int3 BlueParts = int3(vTest);

				float3 vSecondColor =
					float3(RedParts.x, GreenParts.x, BlueParts.x) * vMask.b
					+ float3(RedParts.y, GreenParts.y, BlueParts.y) * vMask.g
					+ float3(RedParts.z, GreenParts.z, BlueParts.z) * vMask.r;

				vSecondary.a -= 0.5 * saturate(saturate(frac(vPos.x / 2.0) - 0.7) * 10000.0);
				vSecondary.a = saturate(saturate(vSecondary.a) * 3.0) * vMask.a;
				return vColor * (1.0 - vSecondary.a) + (vSecondColor / float(nDivisor)) * vSecondary.a;
			}

			float GetSnow(float4 vFoWColor)
			{
				return lerp(vFoWColor.b, vFoWColor.g, vFoWOpacity_Time.z); //Get winter;
			}

			float3 ApplySnow(float3 vColor, float3 vPos, inout float3 vNormal, float4 vFoWColor, in sampler2D FoWDiffuse, float3 vSnowColor)
			{
				float vSnowFade = saturate(vPos.y - SNOW_START_HEIGHT);
				float vNormalFade = saturate(saturate(vNormal.y - SNOW_NORMAL_START) * 10.0f);

				float vNoise = tex2D(FoWDiffuse, (vPos.xz + 0.5f) / 100.0f).r;
				float vSnowTexture = tex2D(FoWDiffuse, (vPos.xz + 0.5f) / 10.0f).r;

				float vIsSnow = GetSnow(vFoWColor);

				//Increase snow on ridges
				vNoise += saturate(vPos.y - SNOW_RIDGE_START_HEIGHT)*(saturate((vNormal.y - 0.9f) * 1000.0f)*vIsSnow);
				vNoise = saturate(vNoise);

				float vSnow = saturate(saturate(vNoise - (1.0f - vIsSnow)) * 5.0f);
				float vFrost = saturate(saturate(vNoise + 0.5f) - (1.0f - vIsSnow));

				vColor = lerp(vColor, vSnowColor * (0.9f + 0.1f * vSnowTexture), saturate(vSnow + vFrost) * vSnowFade * vNormalFade * (saturate(vIsSnow*2.25f)));

				vNormal.y += 1.0f * saturate(vSnow + vFrost) * vSnowFade * vNormalFade;
				vNormal = normalize(vNormal);

				return vColor;
			}

			float3 ApplySnow(float3 vColor, float3 vPos, inout float3 vNormal, float4 vFoWColor, in sampler2D FoWDiffuse)
			{
				return ApplySnow(vColor, vPos, vNormal, vFoWColor, FoWDiffuse, SNOW_COLOR);
			}

			float3 ApplyWaterSnow(float3 vColor, float3 vPos, inout float3 vNormal, float4 vFoWColor, in sampler2D FoWDiffuse)
			{
				return ApplySnow(vColor, vPos, vNormal, vFoWColor, FoWDiffuse, SNOW_WATER_COLOR);
			}

			float3 GetOverlay(float3 vColor, float3 vOverlay, float vOverlayPercent)
			{
				float3 res;
				res.r = vOverlay.r < .5 ? (2.0 * vOverlay.r * vColor.r) : (1.0 - 2.0 * (1.0 - vOverlay.r) * (1.0 - vColor.r));
				res.g = vOverlay.g < .5 ? (2.0 * vOverlay.g * vColor.g) : (1.0 - 2.0 * (1.0 - vOverlay.g) * (1.0 - vColor.g));
				res.b = vOverlay.b < .5 ? (2.0 * vOverlay.b * vColor.b) : (1.0 - 2.0 * (1.0 - vOverlay.b) * (1.0 - vColor.b));

				return lerp(vColor, res, vOverlayPercent);
			}

			float4 GetTIColor(float3 vPos, in sampler2D TITexture)
			{
				return tex2D(TITexture, (vPos.xz + 0.5f) / float2(1876.0f, 2048.0f));
			}

			float GetTI(float4 vFoWColor)
			{
				return vFoWColor.r;
				//return saturate( (vFoWColor.r-0.5f) * 1000.0f );
			}

			float4 GetFoWColor(float3 vPos, in sampler2D FoWTexture)
			{
				return tex2D(FoWTexture, float2(((vPos.x + 0.5f) / MAP_SIZE_X) * FOW_POW2_X, ((vPos.z + 0.5f) / MAP_SIZE_Y) * FOW_POW2_Y));
			}

			bool GetFoWAndTI(float3 PrePos, out float4 vFoWColor, out float TI, out float4 vTIColor)
			{
				vFoWColor = GetFoWColor(PrePos, FoWTexture);
				TI = GetTI(vFoWColor);
				vTIColor = GetTIColor(PrePos, TITexture);
				return (TI - 0.99f) * 1000.0f <= 0.0f;
			}

			float mipmapLevel(float2 uv)
			{
				float dx = fwidth(uv.x * TEXELS_PER_TILE);
				float dy = fwidth(uv.y * TEXELS_PER_TILE);
				float d = max(dot(dx, dx), dot(dy, dy));
				return 0.5 * log2(d);
			}
			
			float3 CalcNormalForLighting(float3 InputNormal, float3 TerrainNormal)
			{
				TerrainNormal = normalize(TerrainNormal);

				//Calculate normal
				float3 zaxis = InputNormal;
				float3 xaxis = cross(zaxis, float3(0, 0, 1)); //tangent
				xaxis = normalize(xaxis);
				float3 yaxis = cross(xaxis, zaxis); //bitangent
				yaxis = normalize(yaxis);
				return xaxis * TerrainNormal.x + zaxis * TerrainNormal.y + yaxis * TerrainNormal.z;
			}

			void calculate_index(float4 IDs, inout float4 IndexU, inout float4 IndexV, inout float vAllSame)
			{
				//Example IDs = (0,0,0,0.5)
				IDs *= 255.0f; //(0,0,0,128)
				vAllSame = saturate(IDs.z - 98.0f); // we've added 100 to first if all IDs are same  // (1.0)
				IDs -= vAllSame * 100.0f; //(0, 0, 0, 28)

				IndexV = trunc((IDs + 0.5f) / NUM_TILES); //(0, 0, 0, 7)
				IndexU = trunc(IDs - (IndexV * NUM_TILES) + 0.5f); //(0, 0, 0, 0)
			}

			float4 sample_terrain(float IndexU, float IndexV, float2 vTileRepeat, float vMipTexels, float lod)
			{
				vTileRepeat = frac(vTileRepeat);

				//#ifdef NO_SHADER_TEXTURE_LOD
				vTileRepeat *= 0.98;
				vTileRepeat += 0.01;
				//#endif

				float vTexelsPerTile = vMipTexels / NUM_TILES;

				vTileRepeat *= (vTexelsPerTile - 1.0f) / vTexelsPerTile;
				return float4((float2(IndexU, IndexV) + vTileRepeat) / NUM_TILES + 0.5f / vMipTexels, 0.0f, lod);
			}

			v2f vert (appdata v)
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

			float4 frag (v2f Input) : SV_Target
			{
				//float2 vOffsets = float2(-0.5 / MAP_SIZE_X, -0. / MAP_SIZE_Y);

				clip( Input.prepos.y + TERRAIN_WATER_CLIP_HEIGHT - WATER_HEIGHT);


				float fTI;
				float4 vFoWColor, vTIColor;
				if (!GetFoWAndTI(Input.prepos, vFoWColor, fTI, vTIColor))
				{
					return float4(vTIColor.rgb, 1.0f);
				}

				float vAllSame;
				float4 IndexU = float4(1, 1, 1, 1);
				float4 IndexV = float4(1, 1, 1, 1);

				float4 IDsample = tex2D(_TerrainIDMap, Input.uv);

				//return IDsample;

				calculate_index(IDsample, IndexU, IndexV, vAllSame);

				float debuga = IndexV.r;

				//return float4(debuga, debuga, debuga, debuga);

				float2 vTileRepeat = Input.uv2 * TERRAIN_TILE_FREQ;
				//TERRAIN_TILE_FREQ = 128.0f
				vTileRepeat.x *= MAP_SIZE_X / MAP_SIZE_Y;

				float lod = clamp(trunc(mipmapLevel(vTileRepeat) - 0.5f), 0.0f, 6.0f);
				float vMipTexels = pow(2.0f, ATLAS_TEXEL_POW2_EXPONENT - lod);
				
				//高度图法线
				float3 vHeightNormalSample = normalize(tex2D(_HeightNormal, Input.uv2).rbg - 0.5f);

				//Terrain Sample Position
				float4 vTerrainSamplePosition = sample_terrain(IndexU.w, IndexV.w, vTileRepeat, vMipTexels, lod);

				//地形 Diffuse
				float4 vTerrainDiffuseSample = tex2Dlod(_TerrainDiffuse, vTerrainSamplePosition);
				float3 vTerrainNormalSample = tex2Dlod(_TerrainNormal, vTerrainSamplePosition).rbg - 0.5f;

				//float4 vTerrainDiffuseSample = tex2D(_TerrainDiffuse, vTerrainSamplePosition);
				//float3 vTerrainNormalSample = tex2D(_TerrainNormal, vTerrainSamplePosition).rbg - 0.5f;

				if (vAllSame < 1.0f)
				{
					float4 TerrainSampleX = sample_terrain(IndexU.x, IndexV.x, vTileRepeat, vMipTexels, lod);
					float4 TerrainSampleY = sample_terrain(IndexU.y, IndexV.y, vTileRepeat, vMipTexels, lod);
					float4 TerrainSampleZ = sample_terrain(IndexU.z, IndexV.z, vTileRepeat, vMipTexels, lod);
					float4 ColorRD = tex2Dlod(_TerrainDiffuse, TerrainSampleX);
					float4 ColorLU = tex2Dlod(_TerrainDiffuse, TerrainSampleY);
					float4 ColorRU = tex2Dlod(_TerrainDiffuse, TerrainSampleZ);

					float2 vFracVector = float2(Input.uv.x * MAP_SIZE_X - 0.5f, Input.uv.y * MAP_SIZE_Y - 0.5f);
					float2 vFrac = frac(vFracVector);

					const float vAlphaFactor = 10.0f;
					float4 vTestFrac = float4(vFrac.x, 1.0f - vFrac.x, vFrac.x, 1.0f - vFrac.x);
					float4 vTestRemainder = float4(
						1.0f + ColorLU.a * vAlphaFactor,
						1.0f + ColorRU.a * vAlphaFactor,
						1.0f + vTerrainDiffuseSample.a * vAlphaFactor,
						1.0f + ColorRD.a * vAlphaFactor);
					float4 vTest = vTestFrac * vTestRemainder;
					float2 yWeights = float2((vTest.x + vTest.y) * vFrac.y, (vTest.z + vTest.w) * (1.0f - vFrac.y));
					float3 vBlendFactors = float3(vTest.x / (vTest.x + vTest.y),
						vTest.z / (vTest.z + vTest.w),
						yWeights.x / (yWeights.x + yWeights.y));

					vTerrainDiffuseSample = lerp(
						lerp(ColorRU, ColorLU, vBlendFactors.x),
						lerp(ColorRD, vTerrainDiffuseSample, vBlendFactors.y),
						vBlendFactors.z);

					
				}

				//return vTerrainDiffuseSample;

				//两张季节颜色贴图
				float4 TerrainColor1 = tex2D(_TerrainColorTint, Input.uv2);
				float4 TerrainColor2 = tex2D(_TerrainColorTintSecond, Input.uv2);

				float3 TerrainColor  = lerp(TerrainColor1, TerrainColor2, _vBorderLookup_HeightScale_UseMultisample_SeasonLerp).rgb;

				float4 vOut = float4(TerrainColor, 1.0);

				//高度法向
				vHeightNormalSample = CalcNormalForLighting(vHeightNormalSample, vTerrainNormalSample);

				//对地形DiffuseSamples 和 TerrainColor 应用颜色修正
				vTerrainDiffuseSample.rgb = GetOverlay(vTerrainDiffuseSample.rgb, TerrainColor, 0.75f);
				//应用Snow
				vTerrainDiffuseSample.rgb = ApplySnow(vTerrainDiffuseSample.rgb, Input.prepos, vHeightNormalSample, vFoWColor, FoWDiffuse);
				//计算第二次
				vTerrainDiffuseSample.rgb = calculate_secondary_compressed(Input.uv, vTerrainDiffuseSample.rgb, Input.prepos.xz);

				float3 TestNormal = (0, 1, 0);

				//应用光照
				vOut = float4(CalculateMapLighting(vTerrainDiffuseSample.rgb, vHeightNormalSample), 1.0);
				float3 result = vTerrainDiffuseSample.rgb;
			
				return vTerrainDiffuseSample;
			}
			ENDCG
		}
	}
}
