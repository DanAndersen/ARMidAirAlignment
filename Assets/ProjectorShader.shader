// Upgrade NOTE: replaced 'UNITY_INSTANCE_ID' with 'UNITY_VERTEX_INPUT_INSTANCE_ID'

Shader "Cg projector shader for adding light" {
	Properties{
	   _ShadowTex("Projected Image", 2D) = "white" {}
	}
	SubShader{
		Pass {
			Blend One One
				// add color of _ShadowTex to the color in the framebuffer 
			ZWrite Off // don't change depths
			Offset -1, -1 // avoid depth fighting (should be "Offset -1, -1")

			CGPROGRAM

			#pragma vertex vert  
			#pragma fragment frag 
			#pragma multi_compile_instancing
			#include "UnityCG.cginc"

			// User-specified properties
			uniform sampler2D _ShadowTex;

			// Projector-specific uniforms
			uniform float4x4 unity_Projector; // transformation matrix 
				// from object space to projector space 

			struct vertexInput {
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};
			struct vertexOutput {
				float4 pos : SV_POSITION;
				float4 posProj : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
					// position in projector space
			};

			vertexOutput vert(vertexInput input)
			{
				vertexOutput output;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				output.posProj = mul(unity_Projector, input.vertex);
				output.pos = UnityObjectToClipPos(input.vertex);
				return output;
			}

			//UNITY_DECLARE_SCREENSPACE_TEXTURE(_ShadowTex);

			float4 frag(vertexOutput input) : COLOR
			{
				UNITY_SETUP_INSTANCE_ID(input);

				if (input.posProj.w > 0.0) // in front of projector?
				{
					float2 uv = input.posProj.xy / input.posProj.w;

					// alternatively: return tex2Dproj(  
					//    _ShadowTex, input.posProj);

					if (uv.x >= 0.0 && uv.x <= 1.0 && uv.y >= 0.0 && uv.y <= 1.0)
					{
						return tex2D(_ShadowTex, uv);
						//return UNITY_SAMPLE_SCREENSPACE_TEXTURE(_ShadowTex, uv);
					}
					else
					{
						return float4(0.0, 0.0, 0.0, 0.0);
					}
				}
				else // behind projector
				{
					return float4(0.0, 0.0, 0.0, 0.0);
				}
			}

			ENDCG
		}
	}
	Fallback "Projector/Light"
}