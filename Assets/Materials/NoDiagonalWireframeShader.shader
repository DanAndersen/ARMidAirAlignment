Shader "Custom/Geometry/Wireframe"
{
	Properties
	{
		[PowerSlider(3.0)]
		_WireframeVal("Wireframe width", Range(0., 0.5)) = 0.05
		_FrontInnerColor("Front inner color", color) = (1., 1., 1., 1.)
		_FrontOuterColor("Front outer color", color) = (1., 1., 1., 1.)
		_BackInnerColor("Back inner color", color) = (1., 1., 1., 1.)
		_BackOuterColor("Back outer color", color) = (1., 1., 1., 1.)
		[Toggle] _RemoveDiag("Remove diagonals?", Float) = 0.
	}
		SubShader
		{
			Tags { "Queue" = "Geometry" "RenderType" = "Opaque" }

			Pass
			{
				Cull Front
				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma geometry geom

			// Change "shader_feature" with "pragma_compile" if you want set this keyword from c# code
			#pragma shader_feature __ _REMOVEDIAG_ON

			#if defined(SHADER_API_D3D11)
			#pragma target 5.0
			#endif

			#include "UnityCG.cginc"

			struct v2g {
				float4 worldPos : SV_POSITION;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			struct g2f {
				float4 pos : SV_POSITION;
				float3 bary : TEXCOORD0;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			v2g vert(appdata_base v) {
				UNITY_SETUP_INSTANCE_ID(v);
				v2g o;
				o.worldPos = mul(unity_ObjectToWorld, v.vertex);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				return o;
			}

			[maxvertexcount(3)]
			void geom(triangle v2g IN[3], inout TriangleStream<g2f> triStream) {
				float3 param = float3(0., 0., 0.);

				#if _REMOVEDIAG_ON
				float EdgeA = length(IN[0].worldPos - IN[1].worldPos);
				float EdgeB = length(IN[1].worldPos - IN[2].worldPos);
				float EdgeC = length(IN[2].worldPos - IN[0].worldPos);

				if (EdgeA > EdgeB && EdgeA > EdgeC)
					param.y = 1.;
				else if (EdgeB > EdgeC && EdgeB > EdgeA)
					param.x = 1.;
				else
					param.z = 1.;
				#endif

				g2f o;
				o.pos = mul(UNITY_MATRIX_VP, IN[0].worldPos);
				o.bary = float3(1., 0., 0.) + param;
				UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(IN[0], o);
				triStream.Append(o);
				o.pos = mul(UNITY_MATRIX_VP, IN[1].worldPos);
				o.bary = float3(0., 0., 1.) + param;
				UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(IN[1], o);
				triStream.Append(o);
				o.pos = mul(UNITY_MATRIX_VP, IN[2].worldPos);
				o.bary = float3(0., 1., 0.) + param;
				UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(IN[2], o);
				triStream.Append(o);
			}

			float _WireframeVal;
			fixed4 _BackInnerColor;
			fixed4 _BackOuterColor;

			fixed4 frag(g2f i) : SV_Target {
			if (!any(bool3(i.bary.x < _WireframeVal, i.bary.y < _WireframeVal, i.bary.z < _WireframeVal)))
				 discard;

				float min_bary = min(min(i.bary.x, i.bary.y), i.bary.z);

				return lerp(_BackInnerColor, _BackOuterColor, min_bary / _WireframeVal);
				//return _BackColor;
			}

			ENDCG
		}

		Pass
		{
			Cull Back
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma geometry geom

				// Change "shader_feature" with "pragma_compile" if you want set this keyword from c# code
				#pragma shader_feature __ _REMOVEDIAG_ON

				#if defined(SHADER_API_D3D11)
				#pragma target 5.0
				#endif

				#include "UnityCG.cginc"

				struct v2g {
					float4 worldPos : SV_POSITION;
					UNITY_VERTEX_OUTPUT_STEREO
				};

				struct g2f {
					float4 pos : SV_POSITION;
					float3 bary : TEXCOORD0;
					UNITY_VERTEX_OUTPUT_STEREO
				};

				v2g vert(appdata_base v) {
					UNITY_SETUP_INSTANCE_ID(v);
					v2g o;
					o.worldPos = mul(unity_ObjectToWorld, v.vertex);
					UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
					return o;
				}

				[maxvertexcount(3)]
				void geom(triangle v2g IN[3], inout TriangleStream<g2f> triStream) {
					float3 param = float3(0., 0., 0.);

					#if _REMOVEDIAG_ON
					float EdgeA = length(IN[0].worldPos - IN[1].worldPos);
					float EdgeB = length(IN[1].worldPos - IN[2].worldPos);
					float EdgeC = length(IN[2].worldPos - IN[0].worldPos);

					if (EdgeA > EdgeB && EdgeA > EdgeC)
						param.y = 1.;
					else if (EdgeB > EdgeC && EdgeB > EdgeA)
						param.x = 1.;
					else
						param.z = 1.;
					#endif

					g2f o;
					o.pos = mul(UNITY_MATRIX_VP, IN[0].worldPos);
					o.bary = float3(1., 0., 0.) + param;
					UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(IN[0], o);
					triStream.Append(o);
					o.pos = mul(UNITY_MATRIX_VP, IN[1].worldPos);
					o.bary = float3(0., 0., 1.) + param;
					UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(IN[1], o);
					triStream.Append(o);
					o.pos = mul(UNITY_MATRIX_VP, IN[2].worldPos);
					o.bary = float3(0., 1., 0.) + param;
					UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(IN[2], o);
					triStream.Append(o);
				}

				float _WireframeVal;
				fixed4 _FrontInnerColor;
				fixed4 _FrontOuterColor;

				fixed4 frag(g2f i) : SV_Target {

					if (!any(bool3(i.bary.x <= _WireframeVal, i.bary.y <= _WireframeVal, i.bary.z <= _WireframeVal)))
						discard;

					float min_bary = min(min(i.bary.x, i.bary.y), i.bary.z);

					return lerp(_FrontInnerColor, _FrontOuterColor, min_bary / _WireframeVal);

					//return _FrontColor;
				}

				ENDCG
			}
		}
}