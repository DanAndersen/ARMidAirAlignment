Shader "Unlit/CompositeShader"
{
    Properties
    {
        _WebcamTex ("Webcam Texture", 2D) = "red" {}
	    _VirtualContentTex("Virtual Content Texture", 2D) = "white" {}

		_VC_Width("Virtual Content width", Float) = 0.0
		_VC_Height("Virtual Content height", Float) = 0.0

		_VC_Fx("Virtual Content Fx", Float) = 0.0
		_VC_Fy("Virtual Content Fy", Float) = 0.0
		_VC_Cx("Virtual Content Cx", Float) = 0.0
		_VC_Cy("Virtual Content Cy", Float) = 0.0

		_WC_Width("Webcam width", Float) = 0.0
		_WC_Height("Webcam height", Float) = 0.0

		_WC_Fx("Webcam Fx", Float) = 0.0
		_WC_Fy("Webcam Fy", Float) = 0.0
		_WC_Cx("Webcam Cx", Float) = 0.0
		_WC_Cy("Webcam Cy", Float) = 0.0

		_WC_K1("Webcam K1", Float) = 0.0
		_WC_K2("Webcam K2", Float) = 0.0
		_WC_P1("Webcam P1", Float) = 0.0
		_WC_P2("Webcam P2", Float) = 0.0
		_WC_K3("Webcam K3", Float) = 0.0
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
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _WebcamTex;
			sampler2D _VirtualContentTex;

			float _VC_Width;
			float _VC_Height;

			float _VC_Fx;
			float _VC_Fy;
			float _VC_Cx;
			float _VC_Cy;

			float _WC_Width;
			float _WC_Height;

			float _WC_Fx;
			float _WC_Fy;
			float _WC_Cx;
			float _WC_Cy;

			float _WC_K1;
			float _WC_K2;
			float _WC_P1;
			float _WC_P2;
			float _WC_K3;
            
            v2f vert (float4 pos : POSITION, float2 uv : TEXCOORD0)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(pos);
                o.uv = uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
				float2 out_img_uv = i.uv;
				float2 out_img_uv_pixels = out_img_uv * float2(_VC_Width, _VC_Height);

				// this out_image represents something being rendered from the perspective of the virtual content camera.
				// find the original x' and y'

				float x_prime = (out_img_uv_pixels.x - _VC_Cx) / _VC_Fx;
				float y_prime = (out_img_uv_pixels.y - _VC_Cy) / _VC_Fy;

				// now use the webcam's intrinsics to determine the webcam UV coordinates.

				float x_prime2 = x_prime * x_prime;
				float y_prime2 = y_prime * y_prime;

				float r2 = x_prime2 + y_prime2;

				float r4 = r2 * r2;
				float r6 = r4 * r2;

				float k1 = _WC_K1;
				float k2 = _WC_K2;
				float p1 = _WC_P1;
				float p2 = _WC_P2;
				float k3 = _WC_K3;

				//float k1 = 0;
				//float k2 = 0;
				//float p1 = 0;
				//float p2 = 0;
				//float k3 = 0;

				float k_total = (1 + k1 * r2 + k2 * r4 + k3 * r6);

				float x_prime_y_prime = x_prime * y_prime;

				float x_prime_prime = x_prime * k_total + 2 * p1*x_prime_y_prime + p2 * (r2 + 2 * x_prime2);

				float y_prime_prime = y_prime * k_total + p1 * (r2 + 2 * y_prime2) + 2 * p2*x_prime_y_prime;

				float wc_u = _WC_Fx * x_prime_prime + _WC_Cx;
				float wc_v = _WC_Fy * y_prime_prime + _WC_Cy;

				float2 wc_uv_pixels = float2(wc_u, wc_v);

				float2 wc_uv = wc_uv_pixels / float2(_WC_Width, _WC_Height);


                // sample the texture
                fixed4 virtual_content_col = tex2D(_VirtualContentTex, out_img_uv);

				fixed4 webcam_col = tex2D(_WebcamTex, wc_uv);

				//fixed4 col = virtual_content_col + webcam_col;

				fixed4 col = lerp(webcam_col, virtual_content_col, virtual_content_col.a);

				//if (virtual_content_col.a < 1) {
					//col = fixed4(1, 0, 1, 1);
				//}

				//fixed4 col = fixed4(i.uv, 0, 1);
				//fixed4 col = fixed4(i.uv.x, i.uv.y, 0, 1);
                return col;

				//float2 c = i.uv * 10;
				//c = floor(c) / 2;
				//float checker = frac(c.x + c.y) * 2;
				//return checker;
            }
            ENDCG
        }
    }
}
