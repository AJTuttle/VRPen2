Shader "Custom/MarkerFill"
{
	Properties
	{
		_Color("Color", Color) = (1,1,1,1)
		_Glossiness("Smoothness", Range(0,1)) = 0.5
		_Metallic("Metallic", Range(0,1)) = 0.0
		_Pressure("Pressure", Range(0,1)) = 0.0
		_TipOffset("Tip Offset", float) = -0.0012
		_Length("Length", float) = .01
	}
		SubShader
		{
			Tags { "Queue" = "Transparent" "RenderType" = "Transparent"  }
			LOD 200
			ZWrite On

			CGPROGRAM
			// Physically based Standard lighting model, and enable shadows on all light types
			#pragma surface surf Standard fullforwardshadows vertex:vert alpha

			// Use shader model 3.0 target, to get nicer looking lighting
			#pragma target 3.0
		
			struct Input
			{
				float3 worldPos;
				float3 localPos;
			};

			half _Glossiness;
			half _Metallic;
			half _Pressure;
			fixed4 _Color;
			half _TipOffset;
			half _Length;

			// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
			// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
			// #pragma instancing_options assumeuniformscaling
			UNITY_INSTANCING_BUFFER_START(Props)
				// put more per-instance properties here
			UNITY_INSTANCING_BUFFER_END(Props)

			void vert(inout appdata_full v, out Input o) {
				UNITY_INITIALIZE_OUTPUT(Input, o);
				o.localPos = v.vertex.xyz;
			}

			void surf(Input IN, inout SurfaceOutputStandard o)
			{
				//float3 localPos = IN.worldPos - mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).xyz;

				//float4 localPos = mul(unity_WorldToObject, IN.worldPos);

				_Pressure = 1 - _Pressure;

				half dist = IN.localPos.y - _TipOffset;

				half mult = step(dist, _Pressure * _Length);



				// Albedo comes from a texture tinted by color
				fixed4 c = _Color * mult;
				o.Albedo = c.rgb;
				// Metallic and smoothness come from slider variables
				o.Metallic = _Metallic;
				o.Smoothness = _Glossiness;
				o.Alpha = c.a * mult + (1-mult) * .5;
			}
			ENDCG
		}
			FallBack "Transparent/Diffuse"
}
