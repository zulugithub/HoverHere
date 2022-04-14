using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;


public class Camera_And_GroundMaterial : MonoBehaviour
{
    public Camera Sub_Camera;
    public Material ground_material;

    private Camera Main_Camera;

    private RenderTexture render_texture;


    void Awake()
    {

       // UnityEngine.XR.XRSettings.gameViewRenderMode = UnityEngine.XR.GameViewRenderMode.RightEye;

        Main_Camera = GetComponent<Camera>();

        if (Sub_Camera.targetTexture != null)
            Sub_Camera.targetTexture.Release();

        Main_Camera.ResetAspect();
        Sub_Camera.ResetAspect();

        render_texture = Sub_Camera.targetTexture;
        render_texture = new RenderTexture(Screen.width*2, Screen.height*2, 24, RenderTextureFormat.ARGB32);
        render_texture.wrapMode = TextureWrapMode.Repeat;
        Sub_Camera.targetTexture = render_texture;
        //Debug.Log("Screen.width " + Screen.width + " Screen.height " + Screen.height);
        ground_material.SetTexture("_MainTex", render_texture);
        Sub_Camera.fieldOfView = Main_Camera.fieldOfView;

    }




    void OnPreRender()
    {
        //Sub_Camera.nearClipPlane = Main_Camera.nearClipPlane;

        //Sub_Camera.farClipPlane = Main_Camera.farClipPlane;

        //Sub_Camera.orthographic = Main_Camera.orthographic;
        //Sub_Camera.orthographicSize = Main_Camera.orthographicSize;
        //Sub_Camera.fieldOfView = Main_Camera.fieldOfView;


        //Sub_Camera.cullingMatrix = Main_Camera.cullingMatrix;
        Sub_Camera.nonJitteredProjectionMatrix = Main_Camera.nonJitteredProjectionMatrix;
        Sub_Camera.projectionMatrix = Main_Camera.projectionMatrix;
        //Sub_Camera.useJitteredProjectionMatrixForTransparentRendering = Main_Camera.useJitteredProjectionMatrixForTransparentRendering;
        //Sub_Camera.worldToCameraMatrix = Main_Camera.worldToCameraMatrix;
        ////////Sub_Camera.stereoConvergence = Main_Camera.stereoConvergence;
        ////////Sub_Camera.stereoSeparation = Main_Camera.stereoSeparation;
        //UnityEngine.Debug.Log("Sub_Camera.stereoSeparation " +Sub_Camera.stereoSeparation);
        //Sub_Camera.stereoTargetEye = Main_Camera.stereoTargetEye;


        //Sub_Camera.sensorSize = Main_Camera.sensorSize;
        //Sub_Camera.gateFit = Main_Camera.gateFit;

        ////////////Sub_Camera.projectionMatrix = Main_Camera.projectionMatrix;
    }

    void Update()
    {
        //Sub_Camera.nonJitteredProjectionMatrix = Main_Camera.nonJitteredProjectionMatrix;
        //Sub_Camera.projectionMatrix = Main_Camera.projectionMatrix;

        //Sub_Camera.nearClipPlane = Main_Camera.nearClipPlane;

        //Sub_Camera.farClipPlane = Main_Camera.farClipPlane;

        //Sub_Camera.orthographic = Main_Camera.orthographic;
        //Sub_Camera.orthographicSize = Main_Camera.orthographicSize;
        //Sub_Camera.fieldOfView = Main_Camera.fieldOfView;

        //Sub_Camera.cullingMatrix = Main_Camera.cullingMatrix;
        //Sub_Camera.nonJitteredProjectionMatrix = Main_Camera.nonJitteredProjectionMatrix;
        //Sub_Camera.projectionMatrix = Main_Camera.projectionMatrix;
        //Sub_Camera.useJitteredProjectionMatrixForTransparentRendering = Main_Camera.useJitteredProjectionMatrixForTransparentRendering;
        //Sub_Camera.worldToCameraMatrix = Main_Camera.worldToCameraMatrix;

        //Sub_Camera.stereoConvergence = Main_Camera.stereoConvergence;
        //Sub_Camera.stereoSeparation = Main_Camera.stereoSeparation;
        //Sub_Camera.stereoTargetEye = Main_Camera.stereoTargetEye;

        //Sub_Camera.sensorSize = Main_Camera.sensorSize;
        //Sub_Camera.gateFit = Main_Camera.gateFit;

    }

}

