using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GFrame
{
    public class DynamicAtlasConfig
    {
        // public static bool kTopFirst = false;
        public const TextureFormat kTextureFormat = TextureFormat.ARGB32;

        // #if UNITY_ANDROID
        //     public const RenderTextureFormat kRenderTextureFormat = RenderTextureFormat.ARGB32;//android,ios的图片RenderTextureFormat
        // #else
        //     public const RenderTextureFormat kRenderTextureFormat = RenderTextureFormat.ARGB32;
        // #endif
    }
}