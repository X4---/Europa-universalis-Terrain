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
		_MainTex ("Texture", 2D) = "white" {}
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

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float3 cor : COLOR;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float3 cor : TEXCOORD1;
				float4 vertex : SV_POSITION;
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.cor = v.cor;
				o.uv = v.uv;

				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				//return 1;
				// sample the texture
				//fixed4 col = tex2D(_MainTex, i.uv);
				// apply fog
				//UNITY_APPLY_FOG(i.fogCoord, col);
				fixed4 col = fixed4(i.cor, 1);

				return col;
			}
			ENDCG
		}

		Pass
		{
			Cull front
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float3 cor : COLOR;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float3 cor : TEXCOORD1;
				float4 vertex : SV_POSITION;
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.cor = v.cor;
				o.uv = v.uv;

				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				//return 1;
				// sample the texture
				//fixed4 col = tex2D(_MainTex, i.uv);
				// apply fog
				//UNITY_APPLY_FOG(i.fogCoord, col);
				//fixed4 col = fixed4(i.cor, 1);

				return 0;
			}
			ENDCG
		}
	}
}
