// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "X4/TerrainWater"
{
	Properties
	{
		HeightMap ("HeightMap", 2D) = "white" {}
		ReflectionCubeMap("ReflectionCubeMap", Cube) = "_Skybox" {}

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

		DebugUV("DebugUV", vector) = (1,1,1,1)

	}
	SubShader
	{
		Tags { "Queue"="Transparent" "IgnoreProjector" = "True" "RenderType"="Transparent" }
		LOD 100

		Blend SrcAlpha OneMinusSrcAlpha

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
				float2 uv_ice : TEXCOORD2;
				float4 pos : TEXCOORD1;
				float4 vertex : SV_POSITION;
			};

			samplerCUBE ReflectionCubeMap;
			sampler2D HeightMap;

			sampler2D WaterColor;
			sampler2D WaterNoise;

			sampler2D IceDiffuse;
			sampler2D IceNormal;

			//sampler2D ProvinceColorMap;

			float4 DebugUV;

			float3 CalcWaterNormal( float2 uv, float vTimeSpeed )
			{
				float vScaledTime = _Time.x * vTimeSpeed;
				float vScaleUV =25.0f;
				float2 time1 = vScaledTime * float2( 0.3f, 0.7f ) * DebugUV.x;
				float2 time2 = -vScaledTime * 0.75f * float2( 0.8f, 0.2f ) * DebugUV.y;
				float2 uv1 = vScaleUV * uv * DebugUV.z;
				float2 uv2 = vScaleUV * uv * 1.3 * DebugUV.w;
				float noiseScale = 12.0f;
				float3 noiseNormal1 = tex2D( WaterNoise, uv1 * noiseScale + time1 * 3.0f ).rgb - 0.5f;
				float3 noiseNormal2 = tex2D( WaterNoise, uv2 * noiseScale + time2 * 3.0f ).rgb - 0.5f;		
				float3 normalNoise = noiseNormal1 + noiseNormal2 + float3( 0.0f, 0.0f, 1.5f );
				return normalize( normalNoise ).xzy;
			}
			
			float4 ApplyIce( float4 vColor, float2 vPos, inout float3 vNormal, float4 vFoWColor, float2 vIceUV, out float vIceFade )
			{
				float vSnow = saturate( GetSnow(vFoWColor) - 0.2f ); 
				vIceFade = vSnow*8.0f;
				float vIceNoise = tex2D( FoWDiffuse, ( vPos + 0.5f ) / 64.0f  ).r;
				vIceFade *= vIceNoise;
				float vMapLimitFade = saturate( saturate( (vPos.y/MAP_SIZE_Y) - 0.74f )*800.0f );
				vIceFade *= vMapLimitFade;
				vIceFade = saturate( ( vIceFade-0.5f ) * 10.0f );
				//vNormal = lerp( vNormal, tex2D( IceNormal, vIceUV ).rgb, vIceFade );
				vNormal = normalize( lerp( vNormal, normalize( tex2D( IceNormal, vIceUV ).rbg - 0.5f ), vIceFade ) );
				float4 vIceColor = tex2D( IceDiffuse, vIceUV );
				vColor = lerp( vColor, vIceColor, saturate(vIceFade) );
				return vColor;
			}

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.pos = mul(unity_ObjectToWorld, v.vertex);
				o.uv = float2(o.pos.x / MAP_SIZE_X,  o.pos.z / MAP_SIZE_Y);
				o.uv_ice = o.uv * float2( MAP_SIZE_X, MAP_SIZE_Y ) * 0.1f;
				return o;
			}
			
			float4 frag (v2f Input) : SV_Target
			{
				float waterHeight = tex2D( HeightMap, Input.uv ).a;

				//return float4(waterHeight,waterHeight,waterHeight,1);
				waterHeight /= ( 93.7f / 255.0f );
				waterHeight = saturate( ( waterHeight - 0.995f ) * 50.0f );

				float4 vFoWColor = GetFoWColor( Input.pos, FoWTexture);	
				float TI = GetTI( vFoWColor );	
				float4 vTIColor = GetTIColor( Input.pos, TITexture );

				if( ( TI - 0.99f ) * 1000.0f > 0.0f )
				{
					return float4( vTIColor.rgb, 1.0f - waterHeight );
				}

				float3 normal = CalcWaterNormal( Input.uv * WATER_TILE, vTimeScale * WATER_TIME_SCALE );

				float4 waterColor = tex2D( WaterColor, Input.uv );
				//return waterColor;
				// Region colors (provinces)
				float2 flippedUV = Input.uv;
				flippedUV.y = 1.0f - flippedUV.y;
				float4 vSample = tex2D( ProvinceColorMap, flippedUV );
				waterColor.rgb = lerp( waterColor.rgb, vSample.rgb, saturate( vSample.a ) );

				float vIceFade = 0.0f;
				waterColor = ApplyIce( waterColor, Input.pos.xz, normal, vFoWColor, Input.uv_ice, vIceFade );
			
				float3 vEyeDir = normalize( Input.pos - _WorldSpaceCameraPos.xyz );
				float3 reflection = reflect( vEyeDir, normal );

				float3 reflectiveColor = texCUBE( ReflectionCubeMap, reflection ).rgb;

				//return float4(reflectiveColor, 1 - waterHeight);
				//col.a = 0.5;
				// sample the texture
				//fixed4 col = tex2D(_MainTex, i.uv);

				float3 refractiveColor = float3(0.1, 0.3, 0.5);
				float vRefractionScale = saturate( 5.0f - ( Input.vertex.z / Input.vertex.w ) * 5.0f );

				float fresnelBias = 0.25f;
				float fresnel = saturate( dot( -vEyeDir, normal ) ) * 0.5f;
				fresnel = saturate( fresnelBias + ( 1.0f - fresnelBias ) * pow( 1.0f - fresnel, 10.0f ) );
				fresnel *= (1.0f-vIceFade); //No fresnel when we have snow
			
				float3 H = normalize( -vLightDir + -vEyeDir );
				float vSpecWidth = MAP_SIZE_X*0.9f;
			
				float vSpecMultiplier = 3.0f;
				float specular = saturate( pow( saturate( dot( H, normal ) ), vSpecWidth ) * vSpecMultiplier );
			
				refractiveColor = lerp( refractiveColor, waterColor.rgb, 0.3f+(0.7f*vIceFade) );
				float3 outColor = refractiveColor * ( 1.0f - fresnel ) + reflectiveColor * fresnel;
			
				outColor = ApplySnow( outColor, Input.pos, normal, vFoWColor, FoWDiffuse );		
			
				float vFoW = GetFoW( Input.pos, vFoWColor, FoWDiffuse );
				
				return float4(lerp( ComposeSpecular( outColor, specular * vFoW ), vTIColor.rgb, TI ), 1.0f - waterHeight);
			}
			ENDCG
		}
	}
}
