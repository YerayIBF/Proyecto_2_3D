namespace BlurShadersPro.BuiltIn
{
    using System;
    using UnityEngine;
    using UnityEngine.Rendering.PostProcessing;

    [Serializable]
    [PostProcess(typeof(RadialBlurRenderer), PostProcessEvent.AfterStack, "Blur Shaders Pro/RadialBlur")]
    public class RadialBlur : PostProcessEffectSettings
    {
        [Range(3, 500), Tooltip("Blur Strength. Higher values require more system resources.")]
        public IntParameter strength = new IntParameter { value = 5 };

        [Range(1, 20), Tooltip("Distance between samples. Larger values may result in artefacts.")]
        public FloatParameter stepSize = new FloatParameter { value = 5 };
    }

    public sealed class RadialBlurRenderer : PostProcessEffectRenderer<RadialBlur>
    {
        public override void Render(PostProcessRenderContext context)
        {
            var sheet = context.propertySheets.Get(Shader.Find("Hidden/BlurShadersPro/RadialBlur"));
            sheet.properties.SetInt("_KernelSize", settings.strength);
            sheet.properties.SetFloat("_Spread", settings.strength / 7.5f);
            sheet.properties.SetFloat("_StepSize", settings.stepSize / 1000.0f);

            context.command.BlitFullscreenTriangle(context.source, context.destination, sheet, 0);
        }
    }
}
