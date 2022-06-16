using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CenturyGame.PostProcess
{
    public abstract class CameraPipeline : MonoBehaviour
    {
        public CameraPipeline frontPipeline;
        public abstract RenderTexture ColorRT { get; }
        public virtual void ChangeDoBlit(bool doBlit) { }
    }
}