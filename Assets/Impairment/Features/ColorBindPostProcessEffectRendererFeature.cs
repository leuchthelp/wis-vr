using System;
using System.Collections.Generic;
using System.Xml.Schema;
using Unity.VisualScripting;
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
                    new Color(0.856167f, 0.182038f, -0.038205f), new Color(0.029342f, 0.955115f, 0.015544f), new Color(-0.002880f, -0.001563f, 1.004443f) // Protanopia @ 10%
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
                    new Color(0.152286f, 1.052583f, -0.204868f), new Color(0.114503f, 0.786281f, 0.099216f), new Color(-0.003882f, -0.048116f, 1.051998f)  // Deuteronopia @ 10%
                },
                {
                    new Color(), new Color(), new Color()  // Deuteronopia @ 20%
                },
                {
                    new Color(), new Color(), new Color()  // Deuteronopia @ 30%
                },
                {
                    new Color(), new Color(), new Color()  // Deuteronopia @ 40%
                },
                {
                    new Color(), new Color(), new Color()  // Deuteronopia @ 50%
                },
                {
                    new Color(), new Color(), new Color()  // Deuteronopia @ 60%
                },
                {
                    new Color(), new Color(), new Color()  // Deuteronopia @ 70%
                },
                {
                    new Color(), new Color(), new Color()  // Deuteronopia @ 80%
                },
                {
                    new Color(), new Color(), new Color()  // Deuteronopia @ 90%
                },
            },
            {
                {
                    new Color(0.152286f, 1.052583f, -0.204868f), new Color(0.114503f, 0.786281f, 0.099216f), new Color(-0.003882f, -0.048116f, 1.051998f)  // Tritanopia @ 10%
                },
                {
                    new Color(), new Color(), new Color()  // Tritanopia @ 20%
                },
                {
                    new Color(), new Color(), new Color()  // Tritanopia @ 30%
                },
                {
                    new Color(), new Color(), new Color()  // Tritanopia @ 40%
                },
                {
                    new Color(), new Color(), new Color()  // Tritanopia @ 50%
                },
                {
                    new Color(), new Color(), new Color()  // Tritanopia @ 60%
                },
                {
                    new Color(), new Color(), new Color()  // Tritanopia @ 70%
                },
                {
                    new Color(), new Color(), new Color()  // Tritanopia @ 80%
                },
                {
                    new Color(), new Color(), new Color()  // Tritanopia @ 90%
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

                if (mode + 1 <= matrix.GetLength(0)) { lowerMatrix = new[] { matrix[mode, 0], matrix[mode, 1], matrix[mode, 2] }; }


                if (matrixType == 0)
                {
                    // Do something with severity override in original volume. Disabling it entirely would make sense, need to figure out how.
                    s_SharedPropertyBlock.SetFloat("_Severity", 0.0f);
                }
                else
                {
                    if (myVolume.severity.overrideState == false)
                    {
                        severity = 0.0f;
                    }
                    s_SharedPropertyBlock.SetFloat("_Severity", severity);



                    string convertable = severity.ToString();
                    int lower;
                    if (convertable.Length == 1)
                    {
                        lower = Convert.ToInt32(convertable.ToString());

                        if (lower == 1) lower = 10;
                    }
                    else
                    {
                        lower = Convert.ToInt32(severity.ToString()[2].ToString());
                    }

                    int upper = lower + 1;

                    Debug.Log("severity: " + severity + " string: " + severity.ToString() + " lower: " + lower + " upper: " + upper);

                    if (mode + 1 >= matrix.GetLength(0))
                    {
                        int severityMode = mode - 4; // -opia will be exactly 4 modes above, therefore we know which one it is.

                        var normal = new[] { matrix[0, 0], matrix[0, 1], matrix[0, 2] };
                        var opia = new[] { matrix[severityMode, 0], matrix[severityMode, 1], matrix[severityMode, 2] };



                        Color[] upperMatrix;
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
                            lowerMatrix = new[] { machadoSeverityRGB[severityMode, lower, 0], machadoSeverityRGB[severityMode, lower, 1], machadoSeverityRGB[severityMode, lower, 2] };
                            upperMatrix = opia;
                        }
                        else
                        {
                            lowerMatrix = new[] { machadoSeverityRGB[severityMode, lower, 0], machadoSeverityRGB[severityMode, lower, 1], machadoSeverityRGB[severityMode, lower, 2] };
                            upperMatrix = new[] { machadoSeverityRGB[severityMode, upper, 0], machadoSeverityRGB[severityMode, upper, 1], machadoSeverityRGB[severityMode, upper, 2] };
                        }


                        s_SharedPropertyBlock.SetColor("_upperR", upperMatrix[0]);
                        s_SharedPropertyBlock.SetColor("_upperG", upperMatrix[1]);
                        s_SharedPropertyBlock.SetColor("_upperB", upperMatrix[2]);


                    }
                }

                s_SharedPropertyBlock.SetColor("_lowerR", lowerMatrix[0]);
                s_SharedPropertyBlock.SetColor("_lowerG", lowerMatrix[1]);
                s_SharedPropertyBlock.SetColor("_lowerB", lowerMatrix[2]);
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
