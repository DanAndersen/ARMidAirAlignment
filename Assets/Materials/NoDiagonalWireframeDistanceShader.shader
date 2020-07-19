Shader "Custom/Geometry/WireframeDistance"
{
	Properties
	{
		[PowerSlider(3.0)]
		_WireThickness("Wire thickness", Range(0, 800)) = 100
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

			float _WireThickness;

			struct v2g {
				float4 worldPos : SV_POSITION;
				float4 viewPos : TEXCOORD3;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			struct g2f {
				float4 pos : SV_POSITION;
				float3 bary : TEXCOORD0;
				float4 viewPos : TEXCOORD3;
				float inverseW : TEXCOORD1;
				float3 dist : TEXCOORD2;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			v2g vert(appdata_base v) {
				UNITY_SETUP_INSTANCE_ID(v);
				v2g o;
				o.worldPos = mul(unity_ObjectToWorld, v.vertex);
				o.viewPos = UnityObjectToClipPos(v.vertex);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				return o;
			}

			[maxvertexcount(3)]
			void geom(triangle v2g IN[3], inout TriangleStream<g2f> triStream) {
				float3 param = float3(0., 0., 0.);

				float3 pointA = IN[0].worldPos.xyz;
				float3 pointB = IN[1].worldPos.xyz;
				float3 pointC = IN[2].worldPos.xyz;

				float3 AB = pointB - pointA;
				float3 AC = pointC - pointA;
				float3 BC = pointC - pointB;

				float area = length(cross(AB, AC)) / 2;




				
				// represents:
				// distance from BC, distance from AC, distance from AB
				float3 distScale[3];
				distScale[0] = float3(2 * area / length(BC), 0, 0);
				distScale[1] = float3(0, 2 * area / length(AC), 0);
				distScale[2] = float3(0, 0, 2 * area / length(AB));

				float length_AB = length(AB);
				float length_AC = length(AC);
				float length_BC = length(BC);

				if (length_AB > length_AC && length_AB > length_BC) {
					// AB is the longest edge

					distScale[0].z = 10000;
					distScale[1].z = 10000;
					distScale[2].z = 10000;
				}
				else if (length_AC > length_AB && length_AC > length_BC) {
					// AC is the longest edge

					distScale[0].y = 10000;
					distScale[1].y = 10000;
					distScale[2].y = 10000;
				}
				else {
					// BC is the longest edge

					//distScale[0].x = 10000;
					//distScale[1].x = 10000;
					//distScale[2].x = 10000;
				}


				/*
				// Calculate the vectors that define the triangle from the input points.
				float2 point0 = IN[0].viewPos.xy / IN[0].viewPos.w;
				float2 point1 = IN[1].viewPos.xy / IN[1].viewPos.w;
				float2 point2 = IN[2].viewPos.xy / IN[2].viewPos.w;

				// Calculate the area of the triangle.
				float2 vector0 = point2 - point1;
				float2 vector1 = point2 - point0;
				float2 vector2 = point1 - point0;
				float area = abs(vector1.x * vector2.y - vector1.y * vector2.x);

				float3 distScale[3];
				distScale[0] = float3(area / length(vector0), 0, 0);
				distScale[1] = float3(0, area / length(vector1), 0);
				distScale[2] = float3(0, 0, area / length(vector2));
				*/

				//float wireScale = 800 - _WireThickness;



				#if _REMOVEDIAG_ON
				float EdgeA = length(IN[0].worldPos - IN[1].worldPos);
				float EdgeB = length(IN[1].worldPos - IN[2].worldPos);
				float EdgeC = length(IN[2].worldPos - IN[0].worldPos);

				if (EdgeA > EdgeB && EdgeA > EdgeC) {
					param.y = 1.;
				}
				else if (EdgeB > EdgeC && EdgeB > EdgeA) {
					param.x = 1.;
				}
				else {
					param.z = 1.;
				}
				#endif




				g2f o;

				o.viewPos = IN[0].viewPos;
				o.inverseW = 1.0 / o.viewPos.w;
				//o.dist = distScale[0] * o.viewPos.w * wireScale;
				o.dist = distScale[0];
				o.pos = mul(UNITY_MATRIX_VP, IN[0].worldPos);
				o.bary = float3(1., 0., 0.) + param;
				UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(IN[0], o);
				triStream.Append(o);

				o.viewPos = IN[1].viewPos;
				o.inverseW = 1.0 / o.viewPos.w;
				//o.dist = distScale[1] * o.viewPos.w * wireScale;
				o.dist = distScale[1];
				o.pos = mul(UNITY_MATRIX_VP, IN[1].worldPos);
				o.bary = float3(0., 0., 1.) + param;
				UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(IN[1], o);
				triStream.Append(o);

				o.viewPos = IN[2].viewPos;
				o.inverseW = 1.0 / o.viewPos.w;
				//o.dist = distScale[2] * o.viewPos.w * wireScale;
				o.dist = distScale[2];
				o.pos = mul(UNITY_MATRIX_VP, IN[2].worldPos);
				o.bary = float3(0., 1., 0.) + param;
				UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(IN[2], o);
				triStream.Append(o);
			}

			float _WireframeVal;
			fixed4 _BackInnerColor;
			fixed4 _BackOuterColor;

			fixed4 frag(g2f i) : SV_Target {

				//if (!any(bool3(i.dist.x < _WireframeVal, i.dist.y < _WireframeVal, i.dist.z < _WireframeVal)))
				//	discard;

				float min_dist = min(min(i.dist.x, i.dist.y), i.dist.z);

				return lerp(_BackInnerColor, _BackOuterColor, min_dist / _WireframeVal);
				//return _BackInnerColor;

				/*
				// Calculate  minimum distance to one of the triangle lines, making sure to correct
				// for perspective-correct interpolation.
				//float dist = min(i.dist[0], min(i.dist[1], i.dist[2])) * i.inverseW;
				float dist = min(i.dist[0], min(i.dist[1], i.dist[2]));

				// Make the intensity of the line very bright along the triangle edges but fall-off very
				// quickly.
				float I = exp2(-2 * dist * dist);

				//if (I < 0.05)
					//discard;

				return I * _BackInnerColor + (1 - I) * _BackOuterColor;
				*/

				/*
				if (!any(bool3(i.bary.x < _WireframeVal, i.bary.y < _WireframeVal, i.bary.z < _WireframeVal)))
					 discard;

				float min_bary = min(min(i.bary.x, i.bary.y), i.bary.z);

				return lerp(_BackInnerColor, _BackOuterColor, min_bary / _WireframeVal);
				//return _BackColor;
				*/
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