using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraRenderTest : MonoBehaviour
{
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
        cam.Render();
    }
}
