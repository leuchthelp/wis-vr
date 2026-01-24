using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public enum ColorBlindMatrixType
{
    CoblisV1,
    Machado,
}
public enum ColorBlindMode
{
    Normal,
    Protanopia,
    Deuteranopia,
    Tritanopia,
    Achromatopsia,
    Protanomaly,
    Deuteranomaly,
    Tritanomaly,
    Achromatomaly,
}

[Serializable]
public sealed class ColorBlindModeParameter : VolumeParameter<ColorBlindMode>
{
    /// <summary>
    /// Creates a new <see cref="ColorBlindModeParameter"/> instance.
    /// </summary>
    /// <param name="value">The initial value to store in the parameter.</param>
    /// <param name="overrideState">The initial override state for the parameter.</param>
    public ColorBlindModeParameter(ColorBlindMode value, bool overrideState = true) : base(value, overrideState) { }
}

[Serializable]
public sealed class ColorBlindMatrixTypeParameter : VolumeParameter<ColorBlindMatrixType>
{
    /// <summary>
    /// Creates a new <see cref="ColorBlindMatrixTypeParameter"/> instance.
    /// </summary>
    /// <param name="value">The initial value to store in the parameter.</param>
    /// <param name="overrideState">The initial override state for the parameter.</param>
    public ColorBlindMatrixTypeParameter(ColorBlindMatrixType value, bool overrideState = true) : base(value, overrideState) { }
}


// Defines a custom Volume Override component that controls the intensity of the URP Post-processing effect on a Scriptable Renderer Feature.
// For more information about the VolumeComponent API, refer to https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@17.2/api/UnityEngine.Rendering.VolumeComponent.html

// Add the Volume Override to the list of available Volume Override components in the Volume Profile.
[VolumeComponentMenu("Custom/Color Blind Effect")]

// If the related Scriptable Renderer Feature doesn't exist, display a warning about adding it to the renderer.
[VolumeRequiresRendererFeatures(typeof(ColorBlindPostProcessEffectRendererFeature))]

// Make the Volume Override active in the Universal Render Pipeline.
[SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]

// Create the Volume Override by inheriting from VolumeComponent
public sealed class ColorBlindEffectComponent : VolumeComponent, IPostProcessComponent
{
    // Set the name of the volume component in the list in the Volume Profile.
    public ColorBlindEffectComponent()
    {
        displayName = "ColorBlindPostProcessEffect";
    }

    // Create a property to control the intesity of the effect, with a tooltip description.
    // You can set the default value in the project-wide Graphics settings window. For more information, refer to https://docs.unity3d.com/Manual/urp/urp-global-settings.html
    // You can override the value in a local or global volume. For more information, refer to https://docs.unity3d.com/Manual/urp/volumes-landing-page.html
    // To access the value in a script, refer to the VolumeManager API: https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@latest/index.html?subfolder=/api/UnityEngine.Rendering.VolumeManager.html 
    [Header("Color Blind Effect Settings")]
    [Tooltip("Enter the description for the property that is shown when hovered")]

    [SerializeField]
    public ClampedFloatParameter intensity = new(1f, 0f, 1f, true);

    public ColorBlindMatrixTypeParameter type = new(ColorBlindMatrixType.CoblisV1);

    public ColorBlindModeParameter mode = new(ColorBlindMode.Normal);

    public ClampedFloatParameter severity = new(1f, 0f, 1f);

    // Optional: Implement the IsActive() method of the IPostProcessComponent interface, and get the intensity value.
    public bool IsActive()
    {
        return true;
    }
}
