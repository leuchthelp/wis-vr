using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

// Create a Scriptable Renderer Feature that implements a post-processing effect when the camera is inside a custom volume.
// For more information about creating scriptable renderer features, refer to https://docs.unity3d.com/Manual/urp/customizing-urp.html
public sealed class ColorBlindPostProcessEffectRendererFeature : ScriptableRendererFeature
{
    #region FEATURE_FIELDS

    // Declare the material used to render the post-processing effect.
    // Add a [SerializeField] attribute so Unity serializes the property and includes it in builds.
    [SerializeField]
    //[HideInInspector]
    private Material m_Material;

    // Declare the render pass that renders the effect.
    private ColorBlindPostRenderPass m_FullScreenPass;

    #endregion

    #region FEATURE_METHODS

    // Override the Create method.
    // Unity calls this method when the Scriptable Renderer Feature loads for the first time, and when you change a property.
    public override void Create()
    {
#if UNITY_EDITOR
        // Assign a material asset to m_Material in the Unity Editor.
        if (m_Material == null)
            m_Material = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Testing/ColorBlind.mat");
#endif

        if (m_Material)
        {
            m_FullScreenPass = new ColorBlindPostRenderPass(name, m_Material);
        }
    }

    // Override the AddRenderPasses method to inject passes into the renderer. Unity calls AddRenderPasses once per camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        // Skip rendering if m_Material or the pass instance are null.
        if (m_Material == null || m_FullScreenPass == null)
            return;

        // Skip rendering if the target is a Reflection Probe or a preview camera.
        if (renderingData.cameraData.cameraType == CameraType.Preview || renderingData.cameraData.cameraType == CameraType.Reflection)
            return;

        // Skip rendering if the camera is outside the custom volume.
        ColorBlindEffectComponent myVolume = VolumeManager.instance.stack?.GetComponent<ColorBlindEffectComponent>();
        if (myVolume == null || !myVolume.IsActive())
            return;

        // Specify when the effect will execute during the frame.
        // For a post-processing effect, the injection point is usually BeforeRenderingTransparents, BeforeRenderingPostProcessing, or AfterRenderingPostProcessing.
        // For more information, refer to https://docs.unity3d.com/Manual/urp/customize/custom-pass-injection-points.html 
        m_FullScreenPass.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;

        // Specify that the effect doesn't need scene depth, normals, motion vectors, or the color texture as input.
        m_FullScreenPass.ConfigureInput(ScriptableRenderPassInput.None);

        // Add the render pass to the renderer.
        renderer.EnqueuePass(m_FullScreenPass);
    }

    #endregion

    // Create the custom render pass.
    private class ColorBlindPostRenderPass : ScriptableRenderPass
    {
        #region PASS_FIELDS

        // Declare the material used to render the post-processing effect.
        private readonly Material m_Material;

        // Declare a property block to set additional properties for the material.
        private static readonly MaterialPropertyBlock s_SharedPropertyBlock = new();

        // Declare a property that enables or disables the render pass that samples the color texture.
        private static readonly bool kSampleActiveColor = true;

        // Create shader properties in advance, which is more efficient than referencing them by string.
        private static readonly int kBlitTexturePropertyId = Shader.PropertyToID("_BlitTexture");
        private static readonly int kBlitScaleBiasPropertyId = Shader.PropertyToID("_BlitScaleBias");

        private static readonly Color[,] coblisV1RGB =
        {
            { new Color(1f,0f,0f),   new Color(0f,1f,0f), new Color(0f,0f,1f) },                                    // Normal
            { new Color(.56667f, .43333f, 0f), new Color(.55833f, .44167f, 0f), new Color(0f, .24167f, .75833f) },  // Protanopia
            { new Color(.625f, .375f, 0f), new Color(.70f, .30f, 0f), new Color(0f, .30f, .70f) },                  // Deuteranopia
            { new Color(.95f, .05f, 0), new Color(0f, .43333f, .56667f), new Color(0f, .475f, .525f) },             // Tritanopia
            { new Color(.299f, .587f, .114f), new Color(.299f, .587f, .114f), new Color(.299f, .587f, .114f) },     // Achromatopsia
            { new Color(.81667f, .18333f, 0f), new Color(.33333f, .66667f, 0f), new Color(0f, .125f, .875f) },      // Protanomaly
            { new Color(.80f, .20f, 0f), new Color(.25833f, .74167f, 0), new Color(0f, .14167f, .85833f) },         // Deuteranomaly
            { new Color(.96667f, .03333f, 0), new Color(0f, .73333f, .26667f), new Color(0f, .18333f, .81667f) },   // Tritanomaly
            { new Color(.618f, .32f, .062f), new Color(.163f, .775f, .062f), new Color(.163f, .320f, .516f)  }      // Achromatomaly
        };

        private static readonly Color[,] machadoRGB =
        {
            { new Color(1f,0f,-0f),   new Color(0f,1f,0f), new Color(-0f,0f,1f) },                                                                      // Normal
            { new Color(0.152286f, 1.052583f, -0.204868f), new Color(0.114503f, 0.786281f, 0.099216f), new Color(-0.003882f, -0.048116f, 1.051998f) },  // Protanopia
            { new Color(0.367322f, 0.860646f, -0.227968f), new Color(0.280085f, 0.672501f, 0.047413f), new Color(-0.011820f, 0.042940f, 0.968881f) },   // Deuteranopia
            { new Color(1.255528f, -0.076749f, -0.178779f), new Color(-0.078411f, 0.930809f, 0.147602f), new Color(0.004733f, 0.691367f, 0.303900f) },  // Tritanopia
            { new Color(.299f, .587f, .114f), new Color(.299f, .587f, .114f), new Color(.299f, .587f, .114f) },                                         // Achromatopsia
            //{ new Color(.56667f, .43333f, 0f), new Color(.55833f, .44167f, 0f), new Color(0f, .24167f, .75833f) },                                    // Protanomaly
            //{ new Color(.625f, .375f, 0f), new Color(0.029342f, 0.955115f, 0.015544f), new Color(0f, .30f, .70f) },                                   // Deuteranomaly
            //{ new Color(.95f, .05f, 0), new Color(0f, .43333f, .56667f), new Color(0f, .475f, .525f) },                                               // Tritanomaly
            //{ new Color(.618f, .32f, .062f), new Color(.163f, .775f, .062f), new Color(.163f, .320f, .516f) }                                         // Achromatomaly
        };

        private static readonly Color[,,] machadoSeverityRGB =
        {
            {
                {
                    new Color(0.856167f, 0.182038f, -0.038205f), new Color(0.029342f, 0.955115f, 0.015544f), new Color(-0.002880f, -0.001563f, 1.004443f)             // Protanopia @ 10%
                },
                {
                    new Color(0.734766f,  0.334872f,  -0.069637f), new Color(0.051840f,   0.919198f,  0.028963f), new Color(-0.004928f,   -0.004209f,     1.009137f)  // Protanopia @ 20%
                },
                {
                    new Color(0.630323f,  0.465641f,  -0.095964f), new Color(0.069181f,   0.890046f,  0.040773f), new Color(-0.006308f,   -0.007724f,     1.014032f)  // Protanopia @ 30%
                },
                {
                    new Color(0.539009f,  0.579343f,  -0.118352f), new Color(0.082546f,   0.866121f,  0.051332f), new Color(-0.007136f,   -0.011959f,     1.019095f)  // Protanopia @ 40%
                },
                {
                    new Color(0.458064f,  0.679578f,  -0.137642f), new Color(0.092785f,   0.846313f,  0.060902f), new Color(-0.007494f,   -0.016807f,     1.024301f)  // Protanopia @ 50%
                },
                {
                    new Color(0.385450f,  0.769005f,  -0.154455f), new Color(0.100526f,   0.829802f,  0.069673f), new Color(-0.007442f,   -0.022190f,     1.029632f)  // Protanopia @ 60%
                },
                {
                    new Color(0.319627f,  0.849633f,  -0.169261f), new Color(0.106241f,   0.815969f,  0.077790f), new Color(-0.007025f,   -0.028051f,     1.035076f)  // Protanopia @ 70%
                },
                {
                    new Color(0.259411f,  0.923008f,  -0.182420f), new Color(0.110296f,   0.804340f,  0.085364f), new Color(-0.006276f,   -0.034346f,     1.040622f)  // Protanopia @ 80%
                },
                {
                    new Color(0.203876f,  0.990338f,  -0.194214f), new Color(0.112975f,   0.794542f,  0.092483f), new Color(-0.005222f,   -0.041043f,     1.046265f)  // Protanopia @ 90%
                },
            },
            {
                {
                    new Color(0.866435f, 	0.177704f, 	-0.044139f), new Color(0.049567f, 	0.939063f, 	0.011370f), new Color(-0.003453f, 	0.007233f, 	0.996220f)  // Deuteronopia @ 10%
                },
                {
                    new Color(0.760729f, 	0.319078f, 	-0.079807f), new Color(0.090568f, 	0.889315f, 	0.020117f), new Color(-0.006027f, 	0.013325f, 	0.992702f)  // Deuteronopia @ 20%
                },
                {
                    new Color(0.675425f, 	0.433850f, 	-0.109275f), new Color(0.125303f, 	0.847755f, 	0.026942f), new Color(-0.007950f, 	0.018572f, 	0.989378f)  // Deuteronopia @ 30%
                },
                {
                    new Color(0.605511f, 	0.528560f, 	-0.134071f), new Color(0.155318f, 	0.812366f, 	0.032316f), new Color(-0.009376f, 	0.023176f, 	0.986200f)  // Deuteronopia @ 40%
                },
                {
                    new Color(0.547494f, 	0.607765f, 	-0.155259f), new Color(0.181692f, 	0.781742f, 	0.036566f), new Color(-0.010410f, 	0.027275f, 	0.983136f)  // Deuteronopia @ 50%
                },
                {
                    new Color(0.498864f, 	0.674741f, 	-0.173604f), new Color(0.205199f, 	0.754872f, 	0.039929f), new Color(-0.011131f, 	0.030969f, 	0.980162f)  // Deuteronopia @ 60%
                },
                {
                    new Color(0.457771f, 	0.731899f, 	-0.189670f), new Color(0.226409f, 	0.731012f, 	0.042579f), new Color(-0.011595f, 	0.034333f, 	0.977261f)  // Deuteronopia @ 70%
                },
                {
                    new Color(0.422823f, 	0.781057f, 	-0.203881f), new Color(0.245752f, 	0.709602f, 	0.044646f), new Color(-0.011843f, 	0.037423f, 	0.974421f)  // Deuteronopia @ 80%
                },
                {
                    new Color(0.392952f, 	0.823610f, 	-0.216562f), new Color(0.263559f, 	0.690210f, 	0.046232f), new Color(-0.011910f, 	0.040281f, 	0.971630f)  // Deuteronopia @ 90%
                },
            },
            {
                {
                    new Color(0.926670f, 	0.092514f, 	-0.019184f), new Color(0.021191f, 	0.964503f, 	0.014306f), new Color(0.008437f, 	0.054813f, 	0.936750f)  // Tritanopia @ 10%
                },
                {
                    new Color(0.895720f, 	0.133330f, 	-0.029050f), new Color(0.029997f, 	0.945400f, 	0.024603f), new Color(0.013027f, 	0.104707f, 	0.882266f)  // Tritanopia @ 20%
                },
                {
                    new Color(0.905871f, 	0.127791f, 	-0.033662f), new Color(0.026856f, 	0.941251f, 	0.031893f), new Color(0.013410f, 	0.148296f, 	0.838294f)  // Tritanopia @ 30%
                },
                {
                    new Color(0.948035f, 	0.089490f, 	-0.037526f), new Color(0.014364f, 	0.946792f, 	0.038844f), new Color(0.010853f, 	0.193991f, 	0.795156f)  // Tritanopia @ 40%
                },
                {
                    new Color(1.017277f, 	0.027029f, 	-0.044306f), new Color(-0.006113f, 	0.958479f, 	0.047634f), new Color(0.006379f, 	0.248708f, 	0.744913f)  // Tritanopia @ 50%
                },
                {
                    new Color(1.104996f, 	-0.046633f, 	-0.058363f), new Color(-0.032137f, 	0.971635f, 	0.060503f), new Color(0.001336f, 	0.317922f, 	0.680742f)  // Tritanopia @ 60%
                },
                {
                    new Color(1.193214f, 	-0.109812f, 	-0.083402f), new Color(-0.058496f, 	0.979410f, 	0.079086f), new Color(-0.002346f, 	0.403492f, 	0.598854f)  // Tritanopia @ 70%
                },
                {
                    new Color(1.257728f, 	-0.139648f, 	-0.118081f), new Color(-0.078003f, 	0.975409f, 	0.102594f), new Color(-0.003316f, 	0.501214f, 	0.502102f)  // Tritanopia @ 80%
                },
                {
                    new Color(1.278864f, 	-0.125333f, 	-0.153531f), new Color(-0.084748f, 	0.957674f, 	0.127074f), new Color(-0.000989f, 	0.601151f, 	0.399838f)  // Tritanopia @ 90%
                },
            },
        };

        private static readonly List<Color[,]> matrixSwitch = new()
        {
            coblisV1RGB,
            machadoRGB,
        };

        #endregion

        public ColorBlindPostRenderPass(string passName, Material material)
        {
            // Add a profiling sampler.
            profilingSampler = new ProfilingSampler(passName);

            // Assign the material to the render pass.
            m_Material = material;

            // To make sure the render pass can sample the active color buffer, set URP to render to intermediate textures instead of directly to the backbuffer.
            requiresIntermediateTexture = kSampleActiveColor;
        }

        #region PASS_SHARED_RENDERING_CODE

        // Add commands to render the effect.
        // This method is used in both the render graph system path and the Compatibility Mode path.
        private static void ExecuteMainPass(RasterCommandBuffer cmd, RTHandle sourceTexture, Material material)
        {
            // Clear the material properties.
            s_SharedPropertyBlock.Clear();
            if (sourceTexture != null)
                s_SharedPropertyBlock.SetTexture(kBlitTexturePropertyId, sourceTexture);

            // Set the scale and bias so shaders that use Blit.hlsl work correctly.
            s_SharedPropertyBlock.SetVector(kBlitScaleBiasPropertyId, new Vector4(1, 1, 0, 0));

            // Set the material properties based on the blended values of the custom volume.
            // For more information, refer to https://docs.unity3d.com/Manual/urp/post-processing/custom-post-processing-with-volume.html
            ColorBlindEffectComponent myVolume = VolumeManager.instance.stack?.GetComponent<ColorBlindEffectComponent>();
            if (myVolume != null)
            {
                s_SharedPropertyBlock.SetFloat("_Intensity", myVolume.intensity.value);
                int matrixType = (int)myVolume.type.value;
                int mode = (int)myVolume.mode.value;

                var severity = myVolume.severity.value;
                var matrix = matrixSwitch[matrixType];

                Color[] lowerMatrix = new Color[3];
                Color[] upperMatrix = new[] { new Color(1f, 0f, 0f), new Color(0f, 1f, 0f), new Color(0f, 0f, 1f) };

                if (mode + 1 <= matrix.GetLength(0)) { lowerMatrix = new[] { matrix[mode, 0], matrix[mode, 1], matrix[mode, 2] }; }


                if (matrixType == 0)
                {
                    // Do something with severity override in original volume. Disabling it entirely would make sense, need to figure out how.
                    s_SharedPropertyBlock.SetFloat("_Severity", 1.0f);
                }
                else
                {
                    if (myVolume.severity.overrideState == false)
                    {
                        severity = 0.0f;
                    }



                    string convertable = severity.ToString();
                    int lower;
                    float actual;
                    if (convertable.Length == 1)
                    {
                        lower = Convert.ToInt32(convertable.ToString());
                        actual = 1.0f;

                        if (lower == 1) lower = 10;
                    }
                    else
                    {
                        lower = Convert.ToInt32(severity.ToString()[2].ToString());
                        actual = (float) Convert.ToDecimal(severity.ToString().Remove(2, 1));
                    }

                    //Debug.Log("severity: " + severity + " actual: " + actual);
                    s_SharedPropertyBlock.SetFloat("_Severity", actual);

                    int upper = lower + 1;


                    if (mode + 1 > matrix.GetLength(0))
                    {
                        int severityMode = mode - 4; // -opia will be exactly 4 modes above, therefore we know which one it is.
                        int severityModeIndex = severityMode -1;
                        int lowerIndex = lower -1;
                        int upperIndex = upper -1;

                        var normal = new[] { matrix[0, 0], matrix[0, 1], matrix[0, 2] };
                        var opia = new[] { matrix[severityMode, 0], matrix[severityMode, 1], matrix[severityMode, 2] };

                        //Debug.Log("severity: " + severity + " string: " + severity.ToString() + " lower: " + lower + " upper: " + upper + " smode: " + severityMode + " mode: " + mode);

                        if (lower == 0)
                        {
                            lowerMatrix = normal;
                            upperMatrix = normal;
                        }
                        else if (lower == 10)
                        {
                            lowerMatrix = opia;
                            upperMatrix = opia;
                        }
                        else if (upper == 10)
                        {
                            lowerMatrix = new[] { machadoSeverityRGB[severityModeIndex, lowerIndex, 0], machadoSeverityRGB[severityModeIndex, lowerIndex, 1], machadoSeverityRGB[severityModeIndex, lowerIndex, 2] };
                            upperMatrix = opia;
                        }
                        else
                        {
                            lowerMatrix = new[] { machadoSeverityRGB[severityModeIndex, lowerIndex, 0], machadoSeverityRGB[severityModeIndex, lowerIndex, 1], machadoSeverityRGB[severityModeIndex, lowerIndex, 2] };
                            upperMatrix = new[] { machadoSeverityRGB[severityModeIndex, upperIndex, 0], machadoSeverityRGB[severityModeIndex, upperIndex, 1], machadoSeverityRGB[severityModeIndex, upperIndex, 2] };
                        }
                    }
                }

                s_SharedPropertyBlock.SetColor("_lowerR", lowerMatrix[0]);
                s_SharedPropertyBlock.SetColor("_lowerG", lowerMatrix[1]);
                s_SharedPropertyBlock.SetColor("_lowerB", lowerMatrix[2]);

                s_SharedPropertyBlock.SetColor("_upperR", upperMatrix[0]);
                s_SharedPropertyBlock.SetColor("_upperG", upperMatrix[1]);
                s_SharedPropertyBlock.SetColor("_upperB", upperMatrix[2]);
            }

            // Draw to the current render target.
            cmd.DrawProcedural(Matrix4x4.identity, material, 0, MeshTopology.Triangles, 3, 1, s_SharedPropertyBlock);
        }

        #endregion

        #region PASS_RENDER_GRAPH_PATH

        // Declare the resources the main render pass uses.
        // This method is used only in the render graph system path.
        private class MainPassData
        {
            public Material material;

            public TextureHandle inputTexture;
        }

        private static void ExecuteMainPass(MainPassData data, RasterGraphContext context)
        {
            ExecuteMainPass(context.cmd, data.inputTexture.IsValid() ? data.inputTexture : null, data.material);
        }

        // Override the RecordRenderGraph method to implement the rendering logic.
        // This method is used only in the render graph system path.
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {

            // Get the resources the pass uses.
            UniversalResourceData resourcesData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            // Sample from the current color texture.
            using (var builder = renderGraph.AddRasterRenderPass<MainPassData>(passName, out var passData, profilingSampler))
            {
                passData.material = m_Material;

                TextureHandle destination;

                // Copy cameraColor to a temporary texture, if the kSampleActiveColor property is set to true. 
                if (kSampleActiveColor)
                {
                    var cameraColorDesc = renderGraph.GetTextureDesc(resourcesData.cameraColor);
                    cameraColorDesc.name = "_CameraColorColorBlindPostProcessing";
                    cameraColorDesc.clearBuffer = false;

                    destination = renderGraph.CreateTexture(cameraColorDesc);
                    passData.inputTexture = resourcesData.cameraColor;

                    // If you use framebuffer fetch in your material, use builder.SetInputAttachment to reduce GPU bandwidth usage and power consumption. 
                    builder.UseTexture(passData.inputTexture, AccessFlags.Read);
                }
                else
                {
                    destination = resourcesData.cameraColor;
                    passData.inputTexture = TextureHandle.nullHandle;
                }


                // Set the render graph to render to the temporary texture.
                builder.SetRenderAttachment(destination, 0, AccessFlags.Write);

                // Set the render method.
                builder.SetRenderFunc((MainPassData data, RasterGraphContext context) => ExecuteMainPass(data, context));

                // Set cameraColor to the new temporary texture so the next render pass can use it. You don't need to blit to and from cameraColor if you use the render graph system.
                if (kSampleActiveColor)
                {
                    resourcesData.cameraColor = destination;
                }
            }
        }

        #endregion
    }
}
