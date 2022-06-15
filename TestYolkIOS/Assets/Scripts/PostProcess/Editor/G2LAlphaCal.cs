using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;

public class G2LAlphaCalWindow : EditorWindow
{
    private static G2LAlphaCalWindow window;
    int numStr = 0;
    string resultByte = "0", resultFloat = "0";

    [MenuItem("资源管理/透明度计算G2L")]
    static void Init()
    {
        window = (G2LAlphaCalWindow)EditorWindow.GetWindow(typeof(G2LAlphaCalWindow));
        window.titleContent = new GUIContent("G2L Alpha");
        window.position = new Rect((Screen.currentResolution.width - 400) / 2, (Screen.currentResolution.height - 300) / 2, 400, 300);
        window.Show();
    }

    void OnGUI()
    {
        numStr = EditorGUILayout.IntField("百分比(%):", numStr);
        if (numStr > 100) numStr = 100;
        if (numStr < 0) numStr = 0;
        if (GUILayout.Button("Cal"))
        {
            float f = numStr / 100.0f;
            f = Mathf.LinearToGammaSpace(f);
            resultByte = (255 * f).ToString("f0");
            resultFloat = f.ToString("f3");
        }
        EditorGUILayout.TextField("透明度数值[0-255]:", resultByte);
        EditorGUILayout.TextField("透明度数值[0-1]:", resultFloat);
    }
}
