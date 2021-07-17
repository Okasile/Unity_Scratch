using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GJ : MonoBehaviour
{
    static GJ instance;
    public static GJ Instance
    {
        get
        {
            if (instance == null)
                instance = FindObjectOfType<GJ>();
            return instance;
        }
    }

    public TrailRenderer wiper;
    public Shader gjshader;

    [Range(1, 20)]
    public int downSample = 2;

    Camera cam, uiCam;
    public Camera Cam { get { if (cam == null) cam = GetComponent<Camera>(); return cam; } }

    RenderTexture rt;

    Vector2Int size;// rtsize
    Vector2[] corners; // ima viewport point 

    private void Awake()
    {
        if (instance == null)
            instance = this;
    }

    private void Update()
    {
        if (rt == null || uiCam == null)
            return;
        //process input      
#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0))
        {
            Wiping(Input.mousePosition, true);          
        }
        if (Input.GetMouseButton(0))
        {
            Wiping(Input.mousePosition, false);            
        }
#endif
        if(Input.touchCount>0)
        {
            Touch t0 = Input.GetTouch(0);
            if (t0.phase == TouchPhase.Began)
            {
                Wiping(Input.mousePosition, true);
            }
            else
            {
                Wiping(Input.mousePosition, false);
            }
        }
    }

    /// <summary>
    /// 重新设置回去
    /// </summary>
    public void Clear()
    {
        if (rt == null)
            return;

        if (wiper != null)
        {
            wiper.Clear();
            wiper.gameObject.SetActive(false);
        }

        RenderTexture rtRaw = RenderTexture.active;
        RenderTexture.active = rt;
        GL.Clear(false, true, Color.black);
        RenderTexture.active = rtRaw;

        //another clear way
        //         Cam.clearFlags = CameraClearFlags.SolidColor;
        //         cam.backgroundColor = Color.black;
        //         Cam.Render();
        //         Cam.clearFlags = CameraClearFlags.Nothing;
    }

    Texture2D t2dForReadPixels;
    /// <summary>
    /// 获取进度  这个方法很慢的
    /// </summary>
    /// <returns></returns>
    public float GetProgress()
    {
        if (t2dForReadPixels == null)
            t2dForReadPixels = new Texture2D(rt.width, rt.height, TextureFormat.R8, false);
        RenderTexture rtRaw = RenderTexture.active;
        RenderTexture.active = rt;
        t2dForReadPixels.ReadPixels(new Rect(0, 0, t2dForReadPixels.width, t2dForReadPixels.height), 0, 0);
        RenderTexture.active = rtRaw;
        t2dForReadPixels.Apply();

        int i = 0;
        for (int x = 0; x < t2dForReadPixels.width; x++)
        {
            for (int y = 0; y < t2dForReadPixels.height; y++)
            {
                if (t2dForReadPixels.GetPixel(x, y).r > 0.001f)
                {
                    i++;
                }
            }
        }
        return i / (float)(rt.width * rt.height);
    }


    /// <summary>
    /// 设置刮奖图片
    /// </summary>
    public void BindIma(Image ima, Camera uiCamera)
    {
        if (ima == null || uiCamera == null)
            return;
        uiCam = uiCamera;
        RectTransform rectTransform = ima.rectTransform;
        Vector3[] worldPos = new Vector3[4];
        rectTransform.GetWorldCorners(worldPos);

        if (corners == null)
            corners = new Vector2[4];
        for (int i = 0; i < worldPos.Length; i++)
        {
            corners[i] = uiCamera.WorldToViewportPoint(worldPos[i]);
        }

        size = new Vector2Int((int)rectTransform.rect.width, (int)rectTransform.rect.height);
        ReCreateRt();
        ima.material = new Material(gjshader);
        ima.material.SetTexture("_AlphaTexture", rt);
    }
    /// <summary>
    /// 重新创建RT
    /// </summary>
    public void ReCreateRt()
    {        
        if (rt != null)
        {
            Clear();
            rt.DiscardContents();
            rt.Release();
            rt = null;
        }
        CreateRt();
    }

    /// <summary>
    /// 刮
    /// </summary>
    /// <param name="screenPos"></param>
    /// <param name="isBeginWipe"></param>
    void Wiping(Vector2 screenPos, bool isBeginWipe)
    {
        Vector3 viewportPos = uiCam.ScreenToViewportPoint(screenPos); //ui viewport
        //ui viewport to GJ viewport
        viewportPos.x = (viewportPos.x - corners[0].x) / (corners[3].x - corners[0].x);
        viewportPos.y = (viewportPos.y - corners[0].y) / (corners[1].y - corners[0].y);
        Vector3 p = Cam.ViewportToWorldPoint(viewportPos);
        p.z = 0;
        wiper.transform.position = p;

        if (isBeginWipe)
            wiper.Clear();
        if (!wiper.gameObject.activeSelf)
            wiper.gameObject.SetActive(true);
    }

    /// <summary>
    /// 创建RT
    /// </summary>
    void CreateRt()
    {
        if (size.x == 0 || size.y == 0)
        {
            Debug.LogError("Create failed with zero size :" + size);
            return;
        }
        if (downSample <= 0 || downSample >= 20)
            downSample = 1;
        rt = new RenderTexture(size.x / downSample, size.y / downSample, 0, RenderTextureFormat.R8, 0);
        rt.name = "GJ RT";
        rt.Create();
        Cam.targetTexture = rt;
        if (!Cam.enabled)
            Cam.enabled = true;
    }
}
