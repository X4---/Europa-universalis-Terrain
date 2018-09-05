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
		ProvinceColorMap("ProvinceColorMap", 2D) = "white"{}

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
		Tags { "RenderType"="Opaque" "TerrainTag" = "Use" }
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

			float _A;

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
				//vTileRepeat *= 0.98;
				//vTileRepeat += 0.01;
				//#endif

				float vTexelsPerTile = vMipTexels / NUM_TILES;

				vTileRepeat *= (vTexelsPerTile - 1.0f) / vTexelsPerTile;
				//return float4((float2(IndexU, IndexV) + vTileRepeat) / NUM_TILES + 0.5f / vMipTexels, 0.0f, lod);
				float2 temp = (float2(IndexU, IndexV) + vTileRepeat) / NUM_TILES;
				temp.y = 1.0 - temp.y;
				return float4(temp, 0.0f, lod);
			}

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
				//float2 vOffsets = float2(-0.5 / MAP_SIZE_X, -0. / MAP_SIZE_Y);

				clip(Input.prepos.y + TERRAIN_WATER_CLIP_HEIGHT - WATER_HEIGHT);


				float fTI;
				float4 vFoWColor, vTIColor;
				if (!GetFoWAndTI(Input.prepos, vFoWColor, fTI, vTIColor))
				{
					return float4(vTIColor.rgb, 1.0f);
				}

				vFoWColor = GetFoWColor(Input.prepos, FoWTexture);
				float TI = GetTI(vFoWColor);
				vTIColor = GetTIColor(Input.prepos, TITexture);
				//return (TI - 0.99f) * 1000.0f <= 0.0f;

				float vAllSame;
				float4 IndexU = float4(1, 1, 1, 1);
				float4 IndexV = float4(1, 1, 1, 1);

				float4 IDsample = tex2D(_TerrainIDMap, Input.uv);

				float deltax = abs(IDsample.x - IDsample.y);
				float deltay = abs(IDsample.y - IDsample.z);
				float deltaz = abs(IDsample.z - IDsample.w);
				float deltaw = abs(IDsample.w - IDsample.x);

				float t = deltax + deltay + deltaz + deltaw;

				//return IDsample;

				calculate_index(IDsample, IndexU, IndexV, vAllSame);

				float debuga = IndexV.r;

				//return vAllSame;

				//return float4(debuga, debuga, debuga, debuga);

				float2 vTileRepeat = Input.uv2 * TERRAIN_TILE_FREQ;

				//float MAPSCALE = MAP_SIZE_X /MAP_SIZE_Y;
				//return MAPSCALE;

				//TERRAIN_TILE_FREQ = 128.0f
				vTileRepeat.x *= (float)MAP_SIZE_X / (float)MAP_SIZE_Y; //由于使用了 非const float值, 所以 在代码中， 这段数值没有生效，导致的Bug
																		//****************---------------**************FUCK BUG ***********************/
																		//vTileRepeat.x *= 2.75; 

				float lod = clamp(trunc(mipmapLevel(vTileRepeat) - 0.5f), 0.0f, 6.0f);
				float vMipTexels = pow(2.0f, ATLAS_TEXEL_POW2_EXPONENT - lod);

				//高度图法线
				float3 vHeightNormalSample = normalize(tex2D(_HeightNormal, Input.uv2).rbg - 0.5f);

				//IndexU.x 范围 (0-3)
				//IndexU.y 范围 (0-3)
				//IndexU.z 范围 (0-3)
				//IndexU.w 范围 (0-3)

				//IndexV.x 范围 (0-4, 8)
				//IndexV.y 范围 (0-4, 8)
				//IndexV.z 范围 (0-4, 8)
				//IndexV.w 范围 (0-4, 8)
				//

				//Terrain Sample Position
				float4 vTerrainSamplePosition = sample_terrain(IndexU.w, IndexV.w, vTileRepeat, vMipTexels, lod);

				if (IndexV.w >_A && IndexV.w < (_A + 0.99f))
				{
					//return 1;
				}
				else
				{
					//return 0;
				}

				//采样地形


				// vTileRepeat = Input.uv2 * TERRAIN_TILE_FREQ ( 128.0f )

				// vTileRepeat = (Input.uv2 = A* 1/128.0f + b( 0 - 1/128.0f)  ) *  TERRAIN_TILE_FREQ
				// 垂直方向上 拥有 128个 Terrain块

				// vTileRepeat.x *= MAP_SIZE_X / MAP_SIZE_Y  (2.75f)

				//vTileRepeat = frac(vTileRepeat);
				//float vTexelsPerTile = vMipTexels / NUM_TILES;

				//lod =0 , vMipTexels = 2048;
				//vTexelsPerTile = 2048 / 4 = 512 ; // 每个TILES 拥有的像素值


				//vTileRepeat *= (vTexelsPerTile - 1.0f) / vTexelsPerTile;

				// UV / NUM_TILES  + vTileRepeat / NUM_TILES + 0.5f/ vMipTexels
				// UV / NUM_TILES (Tiles 块的 UV ， 0， 0.25， 0.5， 0.75) 

				//return float4((float2(IndexU, IndexV) + vTileRepeat) / NUM_TILES + 0.5f / vMipTexels, 0.0f, lod);
				//

				//float2 DebugUV = float2(IndexU.w, IndexV.w) / NUM_TILES;

				//float2 dd = float2(1 / (TERRAIN_TILE_FREQ * MAP_SIZE_X / MAP_SIZE_Y), 1 / TERRAIN_TILE_FREQ);
				//float2 testa = trunc(Input.uv2.xy / dd);
				//testa = Input.uv2.xy - testa * dd;


				//float2 DebugRepeat = Input.uv2.xy * TERRAIN_TILE_FREQ;
				//DebugRepeat.x *= MAP_SIZE_X / MAP_SIZE_Y;
				//DebugRepeat = frac(DebugRepeat) / NUM_TILES;

				//float2 ddda = Input.uv2.xy;
				//ddda.x *= 2.75;
				//float2 dddb = frac(ddda * TERRAIN_TILE_FREQ);
				//ddda.x *= (float)MAP_SIZE_X / MAP_SIZE_Y;
				//float4 vDebugSampe = tex2D(_TerrainDiffuse, dddb);

				//地形 Diffuse
				float4 vTerrainDiffuseSample = tex2Dlod(_TerrainDiffuse, vTerrainSamplePosition);
				float3 vTerrainNormalSample = tex2Dlod(_TerrainNormal, vTerrainSamplePosition).rbg - 0.5f;

				//return vTerrainDiffuseSample;

				//return vTerrainDiffuseSample;
				//return vTerrainSamplePosition;
				//return IndexV.x - _A;
				//return IndexU.w /4;
				//return vTerrainSamplePosition.r;
				//return vTerrainDiffuseSample.a;
				//float4 vTerrainDiffuseSample = tex2D(_TerrainDiffuse, vTerrainSamplePosition);
				//float3 vTerrainNormalSample = tex2D(_TerrainNormal, vTerrainSamplePosition).rbg - 0.5f;

				//if(false)
				//if (vAllSame < 1.0f)
				{
					float4 TerrainSampleX = sample_terrain(IndexU.x, IndexV.x, vTileRepeat, vMipTexels, lod);
					float4 TerrainSampleY = sample_terrain(IndexU.y, IndexV.y, vTileRepeat, vMipTexels, lod);
					float4 TerrainSampleZ = sample_terrain(IndexU.z, IndexV.z, vTileRepeat, vMipTexels, lod);
					float4 ColorRD = tex2Dlod(_TerrainDiffuse, TerrainSampleX);
					float4 ColorLU = tex2Dlod(_TerrainDiffuse, TerrainSampleY);
					float4 ColorRU = tex2Dlod(_TerrainDiffuse, TerrainSampleZ);

					//return ColorRD;
					//return ColorLU;
					//return ColorRU;

					float2 vFracVector = float2(Input.uv2.x * MAP_SIZE_X - 0.5f, Input.uv2.y * MAP_SIZE_Y - 0.5f);
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

					vTerrainDiffuseSample = lerp(lerp(ColorRU, ColorLU, vBlendFactors.x),lerp(ColorRD, vTerrainDiffuseSample, vBlendFactors.y),vBlendFactors.z);

					float3 terrain_normalRD = tex2Dlod(_TerrainNormal, TerrainSampleX).rbg - 0.5f;
					float3 terrain_normalLU = tex2Dlod(_TerrainNormal, TerrainSampleY).rbg - 0.5f;
					float3 terrain_normalRU = tex2Dlod(_TerrainNormal, TerrainSampleZ).rbg - 0.5f;

					vTerrainNormalSample =
						((1.0f - vBlendFactors.x) * terrain_normalRU + vBlendFactors.x * terrain_normalLU) * (1.0f - vBlendFactors.z) +
						((1.0f - vBlendFactors.y) * terrain_normalRD + vBlendFactors.y * vTerrainNormalSample) * vBlendFactors.z;

				}

				//return vTerrainDiffuseSample;

				//两张季节颜色贴图
				float4 TerrainColor1 = tex2D(_TerrainColorTint, Input.uv2);
				float4 TerrainColor2 = tex2D(_TerrainColorTintSecond, Input.uv2);

				float3 TerrainColor = lerp(TerrainColor1, TerrainColor2, _vBorderLookup_HeightScale_UseMultisample_SeasonLerp).rgb;

				float4 vOut = float4(TerrainColor, 1.0);

				//return float4(vTerrainDiffuseSample.rgb,1);
				//return vOut;
				{
					//高度法向
					vHeightNormalSample = CalcNormalForLighting(vHeightNormalSample, vTerrainNormalSample);

					//对地形DiffuseSamples 和 TerrainColor 应用颜色修正
					vTerrainDiffuseSample.rgb = GetOverlay(vTerrainDiffuseSample.rgb, TerrainColor, 0.75f);

					//应用Snow
					vTerrainDiffuseSample.rgb = ApplySnow(vTerrainDiffuseSample.rgb, Input.prepos, vHeightNormalSample, vFoWColor, FoWDiffuse);
					//return vTerrainDiffuseSample;
					//计算第二次
					vTerrainDiffuseSample.rgb = calculate_secondary_compressed(Input.uv, vTerrainDiffuseSample.rgb, Input.prepos.xz);

					vOut = float4(CalculateMapLighting(vTerrainDiffuseSample.rgb, vHeightNormalSample), 1.0);
				}
				//return vFoWColor;

				//应用光照

				float2 blendxy = float2( 0.4f, 0.45f);

				float3 result = vTerrainDiffuseSample.rgb;

				float4 Pcolor = tex2D(ProvinceColorMap, Input.uv2);

				if( Pcolor.a < 0.5)
				{
					return float4( result, 1);
				}

				float3 testc = dot(result, float3( 0.299, 0.587, 0.114)) * blendxy.x + Pcolor.xyz * blendxy.y;

				return float4(testc,1);
				}
			ENDCG
		}

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
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float2 uv2 : TEXCOORD1;
				float3 prepos : TEXCOORD2;
				float4 vertex : SV_POSITION;
			};

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex + float4(0, 1, 0,0));

				float2 offset = float2(0.5 / MAP_SIZE_X, 0.5 / MAP_SIZE_Y);

				o.uv = v.uv;//+offset;
				o.uv2 = o.uv;

				o.uv.y = 1 - o.uv.y;
				o.prepos = mul(unity_ObjectToWorld, v.vertex);

				return o;
			}

			float4 frag(v2f Input) : SV_Target
			{
				discard;
				float4 Pcolor  = tex2D(ProvinceColorMap, Input.uv2);

				float4 Pcolor0  = tex2D(ProvinceColorMap, Input.uv2 + ( 1,1) * ( 1/ MAP_SIZE_X, 1 /MAP_SIZE_Y));
				float4 Pcolor1 = tex2D(ProvinceColorMap, Input.uv2 + ( 1,-1) * ( 1/ MAP_SIZE_X, 1 /MAP_SIZE_Y));
				float4 Pcolor2 = tex2D(ProvinceColorMap, Input.uv2 + ( -1,-1) * ( 1/ MAP_SIZE_X, 1 /MAP_SIZE_Y));
				float4 Pcolor3 = tex2D(ProvinceColorMap, Input.uv2 + ( -1,1) * ( 1/ MAP_SIZE_X, 1 /MAP_SIZE_Y));

				float4 A = Pcolor0 + Pcolor1 + Pcolor2 + Pcolor3;
				A /= 4;


				float l = distance( A, Pcolor);
				
				clip(l- 0.1);

				return l;
			}

			ENDCG
		}
	}
}
