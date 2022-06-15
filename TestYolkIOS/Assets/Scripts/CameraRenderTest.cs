using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using CenturyGame.PostProcess;

public class CameraRenderTest : MonoBehaviour
{
    public Camera mainCam;
    public Camera cam;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        StartCoroutine(Render());
    }

    private IEnumerator Render()
    {
        //等待渲染线程结束
        yield return new WaitForEndOfFrame();

        RenderTexture target = null;
        var ppm = mainCam.gameObject.GetComponent<PostProcessManager>();
        Material linearMat = ppm.GetUITarget(ref target);
        cam.SetTargetBuffers(target.colorBuffer, target.depthBuffer);
        //Debug.Log(ppm.GetUITarget);
        cam.Render();
    }
}
