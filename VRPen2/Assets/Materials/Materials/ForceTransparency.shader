Shader "Unlit/ForceTransparency"
{
	Properties{
			_MainTex("Base (RGB)", 2D) = "white" {}
			BG_Color ("Background Color", Color) = (1,1,1,1)
	}

	SubShader{
		Blend SrcAlpha OneMinusSrcAlpha

		Pass {
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag

			#include "UnityCG.cginc"


			fixed4 BG_Color;

			uniform sampler2D _MainTex;

			fixed4 frag(v2f_img i) : SV_Target {
				fixed4 r = tex2D(_MainTex, i.uv);

				half3 diff = r.xyz - BG_Color.xyz;
				half diff_squared = dot(diff, diff);

				if (diff_squared < 0.01f)
				{
					r.a = 0;
				}
				return r;
			}
			ENDCG
		}
	}
}
