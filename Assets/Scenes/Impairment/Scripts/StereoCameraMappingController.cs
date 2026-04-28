using System.Collections;
using System.Collections.Generic;
using Meta.XR;
using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class StereoCameraMappingController : MonoBehaviour
{
    [SerializeField] private PassthroughCameraAccess leftCameraAccess;
    [SerializeField] private PassthroughCameraAccess rightCameraAccess;
    [SerializeField] private Material targetMaterial;
    [Header("Per-Eye UV Offset")]
    [SerializeField, Range(-0.2f, 0f)] private float leftUvOffsetX;
    [SerializeField, Range(-0.2f, 0f)] private float leftUvOffsetY;
    [SerializeField, Range(-0.2f, 0f)] private float rightUvOffsetX;
    [SerializeField, Range(-0.2f, 0f)] private float rightUvOffsetY;

    private static readonly int LeftTexId = Shader.PropertyToID("_LeftTex");
    private static readonly int RightTexId = Shader.PropertyToID("_RightTex");

    private static readonly int PriorLeftTexId = Shader.PropertyToID("_PriorLeftTex");
    private static readonly int PriorRightTexId = Shader.PropertyToID("_PriorRightTex");

    private static readonly int LeftCameraPosId = Shader.PropertyToID("_LeftCameraPos");
    private static readonly int RightCameraPosId = Shader.PropertyToID("_RightCameraPos");
    private static readonly int LeftCameraRotationMatrixId = Shader.PropertyToID("_LeftCameraRotationMatrix");
    private static readonly int RightCameraRotationMatrixId = Shader.PropertyToID("_RightCameraRotationMatrix");

    private static readonly int LeftFocalLengthId = Shader.PropertyToID("_LeftFocalLength");
    private static readonly int RightFocalLengthId = Shader.PropertyToID("_RightFocalLength");
    private static readonly int LeftPrincipalPointId = Shader.PropertyToID("_LeftPrincipalPoint");
    private static readonly int RightPrincipalPointId = Shader.PropertyToID("_RightPrincipalPoint");

    private static readonly int LeftSensorResolutionId = Shader.PropertyToID("_LeftSensorResolution");
    private static readonly int RightSensorResolutionId = Shader.PropertyToID("_RightSensorResolution");
    private static readonly int LeftCurrentResolutionId = Shader.PropertyToID("_LeftCurrentResolution");
    private static readonly int RightCurrentResolutionId = Shader.PropertyToID("_RightCurrentResolution");
    private static readonly int LeftUvOffsetId = Shader.PropertyToID("_LeftUvOffset");
    private static readonly int RightUvOffsetId = Shader.PropertyToID("_RightUvOffset");

    private static readonly Vector4[,] machadoRGB =
        {
            { new Vector4(1f,0f,-0f,1),   new Vector4(0f,1f,0f,1f), new Vector4(-0f,0f,1f,1) },                                                                      // Normal
            { new Vector4(0.152286f, 1.052583f, -0.204868f,1f), new Vector4(0.114503f, 0.786281f, 0.099216f,1f), new Vector4(-0.003882f, -0.048116f, 1.051998f,1f) },  // Protanopia
            { new Vector4(0.367322f, 0.860646f, -0.227968f,1f), new Vector4(0.280085f, 0.672501f, 0.047413f,1f), new Vector4(-0.011820f, 0.042940f, 0.968881f,1f) },   // Deuteranopia
            { new Vector4(1.255528f, -0.076749f, -0.178779f,1f), new Vector4(-0.078411f, 0.930809f, 0.147602f,1f), new Vector4(0.004733f, 0.691367f, 0.303900f,1f) },  // Tritanopia
            { new Vector4(.299f, .587f, .114f), new Vector4(.299f, .587f, .114f,1f), new Vector4(.299f, .587f, .114f,1f) },                                         // Achromatopsia
        };

    private static readonly Vector4[,] coblisV1RGB =
        {
            { new Vector4(1f,0f,0f,1f),   new Vector4(0f,1f,0f,1f), new Vector4(0f,0f,1f,1f) },                                    // Normal
            { new Vector4(.56667f, .43333f, 0f,1f), new Vector4(.55833f, .44167f, 0f,1f), new Vector4(0f, .24167f, .75833f,1f) },  // Protanopia
            { new Vector4(.625f, .375f, 0f,1f), new Vector4(.70f, .30f, 0f,1f), new Vector4(0f, .30f, .70f,1f) },                  // Deuteranopia
            { new Vector4(.95f, .05f, 0,1f), new Vector4(0f, .43333f, .56667f,1f), new Vector4(0f, .475f, .525f,1f) },             // Tritanopia
            { new Vector4(.299f, .587f, .114f,1f), new Vector4(.299f, .587f, .114f,1f), new Vector4(.299f, .587f, .114f,1f) },     // Achromatopsia
            //{ new Vector4(.81667f, .18333f, 0f,1f), new Vector4(.33333f, .66667f, 0f,1f), new Vector4(0f, .125f, .875f,1f) },      // Protanomaly
            //{ new Vector4(.80f, .20f, 0f,1f), new Vector4(.25833f, .74167f, 0,1f), new Vector4(0f, .14167f, .85833f,1f) },         // Deuteranomaly
            //{ new Vector4(.96667f, .03333f, 0,1f), new Vector4(0f, .73333f, .26667f,1f), new Vector4(0f, .18333f, .81667f,1f) },   // Tritanomaly
            //{ new Vector4(.618f, .32f, .062f,1f), new Vector4(.163f, .775f, .062f,1f), new Vector4(.163f, .320f, .516f,1f)  }      // Achromatomaly
        };

    public readonly List<Vector4[,]> matrixSwitch = new()
        {
            coblisV1RGB,
            machadoRGB,
        };

    public enum ColorBlindType
    {
        CoblisV1,
        Machado,
    }

    public ColorBlindType type;

    public enum ColorBlindMode
    {
        Normal,
        Protanopia,
        Deuteranopia,
        Tritanopia,
        Achromatopsia,
    }

    public ColorBlindMode mode;

    public float severity = 1f;

    private IEnumerator Start()
    {
        leftCameraAccess = ResolveCamera(leftCameraAccess, PassthroughCameraAccess.CameraPositionType.Left);
        rightCameraAccess = ResolveCamera(rightCameraAccess, PassthroughCameraAccess.CameraPositionType.Right);

        if (!leftCameraAccess || !rightCameraAccess)
        {
            Debug.LogError("[StereoCameraMappingController] Left/Right PassthroughCameraAccess components are required.");
            yield break;
        }

        if (!targetMaterial)
        {
            Debug.LogError("[StereoCameraMappingController] Target material is not assigned.");
            yield break;
        }

        yield return new WaitUntil(() => leftCameraAccess.IsPlaying && rightCameraAccess.IsPlaying);

        GetComponent<Renderer>().material = new Material(targetMaterial);
        var material = GetComponent<Renderer>().material;
        material.SetTexture(LeftTexId, leftCameraAccess.GetTexture());
        material.SetTexture(RightTexId, rightCameraAccess.GetTexture());
        ApplyCalibrationToMaterial(material);

        UpdateEyeData(leftCameraAccess, true, material);
        UpdateEyeData(rightCameraAccess, false, material);
    }

    private void Update()
    {
        var material = GetComponent<Renderer>().material;
        if (!material || !leftCameraAccess || !rightCameraAccess || !leftCameraAccess.IsPlaying || !rightCameraAccess.IsPlaying)
        {
            return;
        }

        ApplyCalibrationToMaterial(material);

        var leftTexture = leftCameraAccess.GetTexture();
        if (leftTexture)
        {
            material.SetTexture(LeftTexId, leftTexture);
        }

        var rightTexture = rightCameraAccess.GetTexture();
        if (rightTexture)
        {
            material.SetTexture(RightTexId, rightTexture);
        }

        UpdateEyeData(leftCameraAccess, true, material);
        UpdateEyeData(rightCameraAccess, false, material);

        UpdateMaterialProperties(material);
        UpdatePriorTexture(material);
    }

    private void UpdateEyeData(PassthroughCameraAccess cameraAccess, bool leftEye, Material material)
    {
        var pose = cameraAccess.GetCameraPose();
        var intrinsics = cameraAccess.Intrinsics;
        var currentResolution = cameraAccess.CurrentResolution;

        var cameraPositionId = leftEye ? LeftCameraPosId : RightCameraPosId;
        var cameraRotationId = leftEye ? LeftCameraRotationMatrixId : RightCameraRotationMatrixId;
        var focalLengthId = leftEye ? LeftFocalLengthId : RightFocalLengthId;
        var principalPointId = leftEye ? LeftPrincipalPointId : RightPrincipalPointId;
        var sensorResolutionId = leftEye ? LeftSensorResolutionId : RightSensorResolutionId;
        var currentResolutionId = leftEye ? LeftCurrentResolutionId : RightCurrentResolutionId;

        material.SetVector(cameraPositionId, pose.position);
        material.SetMatrix(cameraRotationId, Matrix4x4.Rotate(Quaternion.Inverse(pose.rotation)));
        material.SetVector(focalLengthId, intrinsics.FocalLength);
        material.SetVector(principalPointId, intrinsics.PrincipalPoint);
        material.SetVector(sensorResolutionId, new Vector4(intrinsics.SensorResolution.x, intrinsics.SensorResolution.y, 0f, 0f));
        material.SetVector(currentResolutionId, new Vector4(currentResolution.x, currentResolution.y, 0f, 0f));
    }

    private void UpdateMaterialProperties(Material material)
    {
        var current_material = material;
        Vector4 current_r = current_material.GetVector("_R");

        int requested_type = (int)type;
        int requested_mode = (int)mode;
        var requested_matrix = matrixSwitch[requested_type];

        //Debug.Log("current R" + current_r + " current G: " + current_material.GetVector("_G") + " current B: " + current_material.GetVector("_B"));
        //Debug.Log("type: " + requested_type + " mode: " + requested_mode);
        //Debug.Log("requested R: " + requested_matrix[requested_mode, 0] + " requested G: " + requested_matrix[requested_mode, 1] + " requested B: " + requested_matrix[requested_mode, 2]);

        if (current_r != requested_matrix[requested_mode, 0])
        {
            //Debug.Log("changed");
            current_material.SetVector("_R", requested_matrix[requested_mode, 0]);
            current_material.SetVector("_G", requested_matrix[requested_mode, 1]);
            current_material.SetVector("_B", requested_matrix[requested_mode, 2]);
        }

        float current_severity = current_material.GetFloat("_Severity");
        float requested_severity = severity;
        if (requested_severity != current_severity)
            current_material.SetFloat("_Severity", requested_severity);
    }

    private void UpdatePriorTexture(Material material)
    {
        Texture leftTex = material.GetTexture(LeftTexId);
        Texture rightTex = material.GetTexture(RightTexId);

        material.SetTexture(PriorLeftTexId, leftTex);
        material.SetTexture(PriorRightTexId, rightTex);
    }

    private void ApplyCalibrationToMaterial(Material material)
    {
        material.SetVector(LeftUvOffsetId, new Vector2(leftUvOffsetX, leftUvOffsetY));
        material.SetVector(RightUvOffsetId, new Vector2(rightUvOffsetX, rightUvOffsetY));
    }

    private static PassthroughCameraAccess ResolveCamera(PassthroughCameraAccess configuredAccess, PassthroughCameraAccess.CameraPositionType cameraPosition)
    {
        if (configuredAccess)
        {
            if (configuredAccess.CameraPosition == cameraPosition)
            {
                return configuredAccess;
            }

            Debug.LogWarning($"[StereoCameraMappingController] Assigned camera has position {configuredAccess.CameraPosition} but {cameraPosition} was expected.");
        }

        var allCameras = FindObjectsByType<PassthroughCameraAccess>(FindObjectsInactive.Include);
        foreach (var cameraAccess in allCameras)
        {
            if (cameraAccess && cameraAccess.CameraPosition == cameraPosition)
            {
                return cameraAccess;
            }
        }

        return null;
    }
}

