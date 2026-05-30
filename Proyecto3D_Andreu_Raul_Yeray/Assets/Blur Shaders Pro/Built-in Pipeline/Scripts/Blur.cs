namespace BlurShadersPro.BuiltIn
{
    using System;
    using UnityEngine;
    using UnityEngine.Rendering.PostProcessing;

    [Serializable]
    [PostProcess(typeof(BlurRenderer), PostProcessEvent.AfterStack, "Blur Shaders Pro/Blur")]
    public class Blur : PostProcessEffectSettings
    {
        [Range(1, 500), Tooltip("Blur Strength")]
        public IntParameter strength = new IntParameter { value = 1 };

        [Range(1, 16), Tooltip("Higher values will skip pixels during blur passes. Increase for better performance.")]
        public IntParameter blurStepSize = new IntParameter { value = 1 };

        [Tooltip("Type of blur. Gaussian blur is slightly more expensive, but higher fidelity.")]
        public BlurTypeParameter blurType = new BlurTypeParameter { value = BlurType.Gaussian };
    }

    public sealed class BlurRenderer : PostProcessEffectRenderer<Blur>
    {
        public override void Render(PostProcessRenderContext context)
        {
            var sheet = context.propertySheets.Get(Shader.Find("Hidden/BlurShadersPro/Blur"));
            sheet.properties.SetInt("_KernelSize", settings.strength);
            sheet.properties.SetFloat("_Spread", settings.strength / 7.5f);
            sheet.properties.SetInteger("_BlurStepSize", settings.blurStepSize);

            if(settings.strength > settings.blurStepSize * 2)
            {
                var tmp = RenderTexture.GetTemporary(Screen.width, Screen.height, 0);

                if (settings.blurType == BlurType.Gaussian)
                {
                    context.command.BlitFullscreenTriangle(context.source, tmp, sheet, 0);
                    context.command.BlitFullscreenTriangle(tmp, context.destination, sheet, 1);
                }
                else if (settings.blurType == BlurType.Box)
                {
                    context.command.BlitFullscreenTriangle(context.source, tmp, sheet, 2);
                    context.command.BlitFullscreenTriangle(tmp, context.destination, sheet, 3);
                }

                RenderTexture.ReleaseTemporary(tmp);
            }
        }
    }

    [Serializable]
    public enum BlurType
    {
        Gaussian, Box
    }

    [Serializable]
    public sealed class BlurTypeParameter : ParameterOverride<BlurType> { }
}
