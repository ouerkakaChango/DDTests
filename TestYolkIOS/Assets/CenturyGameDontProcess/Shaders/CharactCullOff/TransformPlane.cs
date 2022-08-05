using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TransformPlane : MonoBehaviour
{
    public Transform myTranform;
    void Start()
    {
        Matrix4x4 planematrix = transform.worldToLocalMatrix;
        Shader.SetGlobalMatrix("_MatrixClipplane", planematrix);
    }
    
}
