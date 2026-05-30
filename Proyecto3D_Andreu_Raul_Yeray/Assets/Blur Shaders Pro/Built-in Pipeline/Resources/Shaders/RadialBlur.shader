Shader "Hidden/BlurShadersPro/RadialBlur"
{
	HLSLINCLUDE
	#include "Packages/com.unity.postprocessing/PostProcessing/Shaders/StdLib.hlsl"

	TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);
	float2 _MainTex_TexelSize;
	uint _KernelSize;
	float _Spread;
	float _StepSize;

	// Define Gaussian function constants.
	static const float E = 2.71828f;

	float gaussian(int x)
	{
		float sigmaSqu = _Spread * _Spread;
		return (1 / sqrt(TWO_PI * sigmaSqu)) * pow(E, -(x * x) / (2 * sigmaSqu));
	}
	ENDHLSL

	SubShader
	{
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			HLSLPROGRAM

			#pragma vertex VertDefault
			#pragma fragment FragHorizontal

			float4 FragHorizontal(VaryingsDefault i) : SV_Target
			{
				float3 col = 0.0f;
				float kernelSum = 0.0f;

				float2 offset = i.texcoord - 0.5f;

				int upper = ((_KernelSize - 1) / 2);
				int lower = -upper;

				for (int x = lower; x <= upper; ++x)
				{
					float2 uv = i.texcoord + offset * x * _StepSize;

					float gauss = gaussian(x);
					kernelSum += gauss;

					col += gauss * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).rgb;
				}

				col /= kernelSum;

				return float4(col, 1.0f);
			}

			ENDHLSL
		}
	}
}
