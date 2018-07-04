// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

//float4 mul( unity_ObjectToWorld, float4); //位置变换，  local -> world 
//float3 UnityObjectToViewPos(float4 / in float3); //位置变换，local -> view
//float4 UnityObjectToClipPos(float4 / in float3); //位置变换，local -> Clip

//float3 UnityWorldToViewPos( in float3 pos )//位置变换, world -> View
//float4 UnityWorldToClipPos( in float3 pos )//位置变换，world -> Clip

//float4 UnityViewToClipPos( in float3 pos ) //位置变换，将 view -> Clip


//float3 UnityObjectToWorldNormal( in float3 norm )//法向变换 locl -> world

//float3 UnityWorldSpaceViewDir( in float3 worldPos )// 视线方向， world



Shader "X4/River"
{
	Properties
	{
		DiffuseMap ("DiffuseMap", 2D) = "white" {}
		NormalMap ("NormalMap", 2D) = "white" {}

		DiffuseBottomMap ("DiffuseBottomMap", 2D) = "white" {}
		SurfaceNormalMap ("SurfaceNormalMap", 2D) = "white" {}

		ColorOverlay ("ColorOverlay", 2D) = "white" {}
		ColorOverlaySecond ("ColorOverlaySecond", 2D) = "white" {}

		HeightNormal ("HeightNormal", 2D) = "white" {}

		FoWTexture ("FoWTexture", 2D) = "white" {}
		FoWDiffuse ("FoWDiffuse", 2D) = "white" {}
		vTimeDirectionSeasonLerp("vTimeDirectionSeasonLerp", vector) = (24.40537, -1, 0.354, 0)

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

			sampler2D DiffuseMap;
			sampler2D NormalMap;
			sampler2D DiffuseBottomMap;
			sampler2D SurfaceNormalMap;

			sampler2D ColorOverlay;
			sampler2D ColorOverlaySecond;
			sampler2D HeightNormal;

			float4 vTimeDirectionSeasonLerp;

			struct appdata
			{
				float4 vPosition : POSITION;
				float4 vUV_Tangent : TEXCOORD0;
			};

			struct v2f
			{
				float4 vPosition	    : SV_POSITION;
				float4 vUV			    : TEXCOORD0;
				float4 vWorldUV_Tangent	: TEXCOORD1;
				float4 vPrePos_Fade		: TEXCOORD2;
				float4 vScreenCoord		: TEXCOORD3;		
				float2 vSecondaryUV		: TEXCOORD4;
			};

			v2f vert (appdata v)
			{
				v2f Out;

				float4 vTmpPos = float4( v.vPosition.xyz, 1.0f );
				float4 vCamLookAtDir = normalize(float4(_WorldSpaceCameraPos  - v.vPosition.xyz, 1.0f));
				float4 vDistortedPos = vTmpPos + float4( (vCamLookAtDir * 0.05f).xyz, 0.0f );

				float4 vProjectPosDistorted = UnityWorldToClipPos(vDistortedPos);
				float4 vProjectPos = UnityWorldToClipPos(vTmpPos);

				Out.vPosition = float4( vProjectPos.xy, vProjectPosDistorted.z, vProjectPos.w );
				//world pos
				Out.vPrePos_Fade.xyz = vTmpPos.xyz;
			
				Out.vUV.x = (  v.vPosition.x + 0.5f ) / MAP_SIZE_X;
				Out.vUV.y =	(  v.vPosition.z + 0.5f ) / MAP_SIZE_Y;


				//Out.vUV.yx = v.vUV_Tangent.xy;
				Out.vUV.x += vTimeDirectionSeasonLerp.x * 1.0f * vTimeDirectionSeasonLerp.y;
				Out.vUV.y += vTimeDirectionSeasonLerp.x * 0.2f;
				Out.vUV.x *= 0.05f;
				Out.vSecondaryUV.yx = v.vUV_Tangent.xy;
				Out.vSecondaryUV.x += vTimeDirectionSeasonLerp.x * 0.9f * vTimeDirectionSeasonLerp.y;
				Out.vSecondaryUV.y -= vTimeDirectionSeasonLerp.x * 0.1f;
				Out.vSecondaryUV.x *= 0.05f;
				Out.vUV.wz = v.vUV_Tangent.xy;
				Out.vUV.z *= 0.05f;
				Out.vWorldUV_Tangent.x = (  v.vPosition.x + 0.5f ) / MAP_SIZE_X;
				Out.vWorldUV_Tangent.y = (  v.vPosition.z + 0.5f ) / MAP_SIZE_Y;
				Out.vWorldUV_Tangent.xy *= float2( MAP_POW2_X, MAP_POW2_Y ); //POW2
				Out.vWorldUV_Tangent.zw = v.vUV_Tangent.zw;
				//Out.vPrePos_Fade.w = saturate( 1.0f - v.vUV_Tangent.y );
				Out.vPrePos_Fade.w = saturate( 1.0f - ( ( 0.1f + v.vUV_Tangent.y ) * 4.0f ) );
				// Output the screen-space texture coordinates
				Out.vScreenCoord.x = ( Out.vPosition.x * 0.5 + Out.vPosition.w * 0.5 );
				Out.vScreenCoord.y = ( Out.vPosition.w * 0.5 - Out.vPosition.y * 0.5 );
				
				
				Out.vScreenCoord.z = Out.vPosition.w;
				Out.vScreenCoord.w = Out.vPosition.w;
			
				return Out;
			}
			
			fixed4 frag (v2f In) : SV_Target
			{
				float4 vFoWColor = GetFoWColor( In.vPrePos_Fade.xyz, FoWTexture);
				float TI = GetTI( vFoWColor );	
				//clip( 0.99f - TI );
			
				float4 vWaterSurface = tex2D( DiffuseMap, float2( In.vUV.x, In.vUV.w ) );
				float3 vHeightNormal = normalize( tex2D( HeightNormal, In.vWorldUV_Tangent.xy ).rbg - 0.5f );
				float3 vSurfaceNormal1 = normalize( tex2D( SurfaceNormalMap, In.vUV.xy ).rgb - 0.5f );
				float3 vSurfaceNormal2 = normalize( tex2D( SurfaceNormalMap, In.vSecondaryUV ).rgb - 0.5f );
				float3 vSurfaceNormal = normalize( vSurfaceNormal1 + vSurfaceNormal2 );
				vSurfaceNormal.xzy = float3( vSurfaceNormal.x * In.vWorldUV_Tangent.zw + vSurfaceNormal.y * float2( -In.vWorldUV_Tangent.w, In.vWorldUV_Tangent.z ), vSurfaceNormal.z );
			
				float3 zaxis = vSurfaceNormal; //normal
				float3 xaxis = cross( zaxis, float3( 0, 0, 1 ) ); //tangent
				xaxis = normalize( xaxis );
				float3 yaxis = cross( xaxis, zaxis ); //bitangent
				yaxis = normalize( yaxis );
				vSurfaceNormal = xaxis * vHeightNormal.x + zaxis * vHeightNormal.y + yaxis * vHeightNormal.z;
				float3 vEyeDir = normalize( In.vPrePos_Fade.xyz - _WorldSpaceCameraPos );
				float3 H = normalize( -vLightDir + -vEyeDir );
				float vSpecRemove = 1.0f - abs( 0.5f - In.vUV.w ) * 2.0f;
				float vSpecWidth = 70.0f;
				float vSpecMultiplier = 0.25f;
				float specular = saturate( pow( saturate( dot( H, vSurfaceNormal ) ), vSpecWidth ) * vSpecMultiplier ) * vSpecRemove/*  dot( vWaterSurface, vWaterSurface )*/;

				float4 vCamLookAtDir = float4(In.vPrePos_Fade.xyz - _WorldSpaceCameraPos, 1.0f);

				float2 vDistort = refract( vCamLookAtDir, vSurfaceNormal, 0.66f ).xz;
				vDistort = vDistort.x * In.vWorldUV_Tangent.zw + vDistort.y * float2( -In.vWorldUV_Tangent.w, In.vWorldUV_Tangent.z );
				float3 vBottom = tex2D( DiffuseBottomMap, In.vUV.zw + vDistort * 0.05f ).rgb;
				float  vBottomAlpha = tex2D( DiffuseBottomMap, In.vUV.zw ).a;
				float3 ColorMap = lerp( tex2D( ColorOverlay, In.vWorldUV_Tangent.xy ), tex2D( ColorOverlaySecond, In.vWorldUV_Tangent.xy ), vTimeDirectionSeasonLerp.z).rgb;
			
				vBottom = GetOverlay( vBottom, ColorMap, 0.5f );
				float3 vBottomNormal = normalize( tex2D( NormalMap, In.vUV.zw ).rgb - 0.5f );
				vBottomNormal.xzy = float3( vBottomNormal.x * In.vWorldUV_Tangent.zw + vBottomNormal.y * float2( -In.vWorldUV_Tangent.w, In.vWorldUV_Tangent.z ), vBottomNormal.z );
				//Calculate normal
				zaxis = vBottomNormal; //normal
				xaxis = cross( zaxis, float3( 0, 0, 1 ) ); //tangent
				xaxis = normalize( xaxis );
				yaxis = cross( xaxis, zaxis ); //bitangent
				yaxis = normalize( yaxis );
				vBottomNormal = xaxis * vHeightNormal.x + zaxis * vHeightNormal.y + yaxis * vHeightNormal.z;
							
				float3 vColor = lerp( vBottom, vWaterSurface.xyz, vWaterSurface.a * 0.8f );
				vColor = ApplyWaterSnow( vColor, In.vPrePos_Fade.xyz, vSurfaceNormal, vFoWColor, FoWDiffuse );
				vColor = CalculateLighting( vColor, vBottomNormal );
			
				float vFoW = GetFoW( In.vPrePos_Fade.xyz, vFoWColor, FoWDiffuse );
			
				// Grab the shadow term
				//float fShadowTerm = GetShadowScaled( SHADOW_WEIGHT_RIVER, In.vScreenCoord, ShadowMap );		
				//vColor *= fShadowTerm;	
			
				//vColor = ApplyDistanceFog( vColor, In.vPrePos_Fade.xyz ) * vFoW;
				return float4( ComposeSpecular( vColor, specular * ( 1.0f - In.vPrePos_Fade.w ) * vWaterSurface.a * vFoW ), vBottomAlpha * ( 1.0f - In.vPrePos_Fade.w ) * (1.0f - TI ) );

				//return 1;
			}
			ENDCG
		}

	}
}
