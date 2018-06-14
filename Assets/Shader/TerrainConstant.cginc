#ifndef TerrainConstant
#define TerrainConstant

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


			const static float MAP_SIZE_X = 5632;
			const static float MAP_SIZE_Y = 2048;
			const static float MAP_POW2_X = 1;
			const static float MAP_POW2_Y = 1;
			const static float FOW_POW2_X = 1;
			const static float FOW_POW2_Y = 1;

			static float4 vLightDir = (0, -1, 0, 0);
			static float4 AMBIENT = (0.5, 0.5, 0.5, 1);
			static float4 LIGHT_DIFFUSE = (1, 1, 1, 1);
			static float LIGHT_INTENSITY = 1;
			static float4 MAP_AMBIENT = 1;
			static float4 MAP_LIGHT_DIFFUSE = (1, 1, 1, 1);
			static float4 MAP_LIGHT_INTENSITY = (1, 1, 1, 1);

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

			static const float 	TERRAIN_TILE_FREQ = 128; // constants.fxh

			const static float WATER_HEIGHT = 19.0f;
			const static float WATER_HEIGHT_RECP = 1.0f / WATER_HEIGHT;
			const static float WATER_HEIGHT_RECP_SQUARED = WATER_HEIGHT_RECP * WATER_HEIGHT_RECP;
			const static float vTimeScale = 0.5f / 300.0f;	


			float _vBorderLookup_HeightScale_UseMultisample_SeasonLerp;
			float4 vFoWOpacity_Time;


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

			float GetSnow(float4 vFoWColor)
			{
				return lerp(vFoWColor.b, vFoWColor.g, vFoWOpacity_Time.z); //Get winter;
			}

			float3 ApplySnow(float3 vColor, float3 vPos, inout float3 vNormal, float4 vFoWColor, in sampler2D FoWDiffuse, float3 vSnowColor)
			{
				//ApplySnow(vTerrainDiffuseSample.rgb, Input.prepos, vHeightNormalSample, vFoWColor, FoWDiffuse);
				float vSnowFade = saturate(vPos.y - SNOW_START_HEIGHT);
				float vNormalFade = saturate(saturate(vNormal.y - SNOW_NORMAL_START) * 10.0f);

				float vNoise = tex2D(FoWDiffuse, (vPos.xz + 0.5f) / 100.0f).r;
				float vSnowTexture = tex2D(FoWDiffuse, (vPos.xz + 0.5f) / 10.0f).r;

				float vIsSnow = GetSnow(vFoWColor);
				//float vIsSnow = 1.0f;

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
				return tex2D(FoWTexture, float2(((vPos.x + 0.5f) / MAP_SIZE_X) * FOW_POW2_X, 1 - ((vPos.z + 0.5f) / MAP_SIZE_Y) * FOW_POW2_Y));
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


#endif