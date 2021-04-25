using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GFrame
{
    public enum DynamicAtlasGroup
    {
        Size_256 = 256,
        Size_512 = 512,
        Size_1024 = 1024,
        Size_2048 = 2048
    }


    public struct DynamicAtlasDefine
    {

    }


    public delegate void OnCallBackTexRect(Texture tex, Rect rect);
}