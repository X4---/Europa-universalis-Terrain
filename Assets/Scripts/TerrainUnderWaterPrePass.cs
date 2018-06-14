using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class TerrainUnderWaterPrePass : MonoBehaviour
{
    public Material kTargetMaterial;
    public RenderTexture kRenderTarget;
    
    public Camera m_Cam;
    

    private static int kLastFrameCount = 0;

    public void Awake()
    {
        //m_Cam = GetComponent<Camera>();
        if(m_Cam != null)
        {
            m_Cam.targetTexture = RenderTexture.GetTemporary(Screen.width, Screen.height, 24);

            kRenderTarget = m_Cam.targetTexture;
        }
    }
    
    public void OnEnable()
    {
        
    }

    public void OnDisable()
    {
    }

    public void Update()
    {
        m_Cam.RenderWithShader(kTargetMaterial.shader, "TerrainTag");
    }
}
