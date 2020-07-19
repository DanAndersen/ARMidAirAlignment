// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Unlit/DiscardBubbleShader"
{
    Properties
    {
		_Color("Color", Color) = (1,1,1,1)
		_RimPower("RimPower", Range(0,10)) = 0.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

		Cull Off
		//Cull Back
		//Cull Front
		ZTest Always

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#if defined(SHADER_API_D3D11)
			#pragma target 5.0
			#endif

			// include file that contains UnityObjectToWorldNormal helper function
			#include "UnityCG.cginc"

			struct vertexInput {
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct vertexOutput {
				// we'll output world space normal as one of regular ("texcoord") interpolators
				half3 worldNormal : TEXCOORD0;
				float4 pos : SV_POSITION;
				float3 worldViewDir : TEXCOORD1;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			// vertex shader: takes object space normal as input too
			vertexOutput  vert(vertexInput input)
			{
				vertexOutput output;

				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
				
				output.pos = UnityObjectToClipPos(input.vertex);
				// UnityCG.cginc file contains function to transform
				// normal from object to world space, use that
				output.worldNormal = UnityObjectToWorldNormal(input.normal);

				// compute world space position of the vertex
				float3 worldPos = mul(unity_ObjectToWorld, input.vertex).xyz;

				output.worldViewDir = normalize(UnityWorldSpaceViewDir(worldPos));

				return output;
			}

			fixed4 _Color;
			half _RimPower;

			fixed4 frag(vertexOutput input) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(input);

				half rim = 1.0 - saturate(abs(dot(normalize(input.worldViewDir), input.worldNormal)));

				half power = pow(rim, _RimPower);

				fixed4 c = 0;
				//c.rgb = _Color.rgb * power;
				c.rgb = _Color.rgb;

				if (power < 0.5) {
					discard;
				}

				return c;
			}
		ENDCG
		}
    }
}
