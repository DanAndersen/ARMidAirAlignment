Shader "Custom/FresnelShaderOnTop" {
	Properties{
		_Shininess("Shininess", Range(0.01, 3)) = 1

		_Color("Shine Color", Color) = (1,1,1,1)

		_MainTex("Base (RGB)", 2D) = "white" {}

	_Bump("Bump", 2D) = "bump" {}

	}
		SubShader{
		Tags{ "RenderType" = "Opaque" }
		LOD 200

		Lighting Off
		ZWrite Off  // uncomment if you have problems like the sprite disappear in some rotations.
		ZTest Always
		Cull Back

		CGPROGRAM
#pragma surface surf Lambert

		sampler2D _MainTex;
	sampler2D _Bump;
	float _Shininess;
	fixed4 _Color;

	struct Input {
		float2 uv_MainTex;
		float2 uv_Bump;
		float3 viewDir;
	};

	void surf(Input IN, inout SurfaceOutput o) {
		half4 c = tex2D(_MainTex, IN.uv_MainTex);
		o.Normal = UnpackNormal(tex2D(_Bump, IN.uv_Bump));
		half factor = dot(normalize(IN.viewDir),o.Normal);
		o.Albedo = c.rgb + _Color * (_Shininess - factor * _Shininess);
		o.Emission.rgb = _Color * (_Shininess - factor * _Shininess);
		o.Alpha = c.a;
	}
	ENDCG
	}
		FallBack "Diffuse"
}