using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShaderDebugger : MonoBehaviour
{
    public int maxLOD = 300;
    public bool runtimeToggle = true;
    public List<int> toggleList;
    int toggleInx = 0;

    // Start is called before the first frame update
    void Start()
    {
        Shader.globalMaximumLOD = maxLOD;
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void OnGUI()
    {
        Vector2Int screenSize = new Vector2Int(Screen.width,Screen.height);

        RenderTexture.active = null;
        if (runtimeToggle && toggleList.Count>0)
        {
            if (GUI.Button(new Rect(screenSize.x * 0.6f, screenSize.y * 0.3f, screenSize.x * 0.3f, screenSize.y * 0.3f), "toggleShaderLOD:"+ Shader.globalMaximumLOD))
            {
                Shader.globalMaximumLOD = toggleList[toggleInx];
                toggleInx++;
                if (toggleInx == toggleList.Count)
                {
                    toggleInx = 0;
                }
            }
        }
        //RenderTexture.active = ori;
    }
}
