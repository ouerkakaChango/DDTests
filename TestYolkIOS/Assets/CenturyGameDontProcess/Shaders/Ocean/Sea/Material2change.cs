using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class Material2change : MonoBehaviour
{

    public Material otherMaterial1; //声明一个需要替换的材质，在Unity页面进行赋值
    public Material otherMaterial2; //声明一个需要替换的材质，在Unity页面进行赋值
    public enum Sky2Night 
    {
        Sun,
        Night
    }
    public Sky2Night sky2 = Sky2Night.Sun;
    private MeshRenderer meshRender;  //声明MeshRenderer组件

    // Use this for initialization
    void Start()
    {
        meshRender = this.GetComponent<MeshRenderer>();  //得到挂载在物体上的MeshRenderer组件
    }

    // Update is called once per frame
    void Update()
    {
        switch(sky2)
        {  
            case Sky2Night.Sun:
            meshRender.material = otherMaterial1;  //就把原来的材质替换成otherMeterial材质
            break;
            case Sky2Night.Night:
            meshRender.material = otherMaterial2;  //就把原来的材质替换成otherMeterial材质
            break;
        }


    }


}
