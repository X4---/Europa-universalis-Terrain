Shader "Change/MapLut"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_MapTex("Texture2D", 2D) = "white"{}
		_BaseColor("BaseColor", COLOR) = (1,1,1,1)
		_TarColor("TarColor", COLOR) = (1,1,1,1)
	}
	SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest Always

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
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}
			
			sampler2D _MainTex;
			sampler2D _MapTex;
			float4 _BaseColor;
			float4 _TarColor;

			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = tex2D(_MapTex, i.uv);
				fixed4 oricol = tex2D(_MainTex, i.uv);

				float xx = col.r - _BaseColor.r;
				xx*=xx;

				float yy = col.g - _BaseColor.g;
				yy*=yy;

				float zz = col.b - _BaseColor.b;
				zz*=zz;

				float d = xx + yy + zz;

				if(d > 0)
				{
					discard;
					return oricol;
				}else
				{	
					return _TarColor;
				}

				return d;
				

				return col;
			}
			ENDCG
		}
	}
}
