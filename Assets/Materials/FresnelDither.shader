Shader "Custom/FresnelDither" {
	Properties{
		_Color("Color", Color) = (1,1,1,1)
		_RimPower("RimPower", Range(0,10)) = 0.0
		_DitherPattern("Dithering Pattern", 2D) = "white" {}
	}
		SubShader{
			Tags{ "RenderType" = "Opaque" "Queue" = "Geometry"}
			LOD 200

			Cull Off
			//Cull Back
			//Cull Front

			CGPROGRAM
			// Physically based Standard lighting model, and enable shadows on all light types
			//#pragma surface surf Standard fullforwardshadows alpha:blend
			//#pragma surface surf Standard alpha:blend
			#pragma surface surf Standard

			// Use shader model 3.0 target, to get nicer looking lighting
			//#pragma target 3.0
			#if defined(SHADER_API_D3D11)
			#pragma target 5.0
			#endif

			struct Input {
				float3 viewDir;
				float4 screenPos;
			};

			half _Glossiness;
			half _Metallic;
			fixed4 _Color;
			half _RimPower;

			//The dithering pattern
			sampler2D _DitherPattern;
			float4 _DitherPattern_TexelSize;

			void surf(Input IN, inout SurfaceOutputStandard o) {

				fixed4 c = _Color;
				o.Albedo = c.rgb;
			
				half rim = 1.0 - saturate(abs(dot(normalize(IN.viewDir), o.Normal)));
			
				half power = pow(rim, _RimPower);

				if (_RimPower > 0)
				{
					o.Emission = _Color.a * _Color.rgb * pow(rim, _RimPower);
				}

				 //o.Alpha = power;

				 //value from the dither pattern
				 float2 screenPos = IN.screenPos.xy / IN.screenPos.w;
				 float2 ditherCoordinate = screenPos * _ScreenParams.xy * _DitherPattern_TexelSize.xy;
				 float ditherValue = tex2D(_DitherPattern, ditherCoordinate).r;

				 //discard pixels accordingly
				 clip(power - ditherValue);
			 }

			ENDCG
		}
			FallBack "Diffuse"
}