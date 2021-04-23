using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DynamicAtlas
{
    private int m_Width = 0;
    private int m_Height = 0;
    private int m_Padding = 3;
    // private float m_UVXDiv, m_UVYDiv;
    private DynamicAtlasGroup m_DynamicAtlasGroup;

    //-------
    private Color32[] m_TempColor;
    // private DynamicAtlasPage m_Page;
    private List<DynamicAtlasPage> m_PageList = new List<DynamicAtlasPage>();
    private List<GetTextureData> m_GetTextureTaskList = new List<GetTextureData>();

    private Dictionary<string, SaveTextureData> m_UsingTexture = new Dictionary<string, SaveTextureData>();

    public DynamicAtlas(DynamicAtlasGroup group)
    {
        m_DynamicAtlasGroup = group;

        int length = (int)group;
        m_TempColor = new Color32[length * length];
        for (int i = 0; i < m_TempColor.Length; i++)
        {
            m_TempColor[i] = Color.clear;
        }

        m_Width = length;
        m_Height = length;
        // m_UVXDiv = 1 / m_Width;
        // m_UVYDiv = 1 / m_Height;
        CreateNewPage();
    }

    DynamicAtlasPage CreateNewPage()
    {
        var page = new DynamicAtlasPage(m_PageList.Count, m_Width, m_Height, m_Padding, m_TempColor);
        m_PageList.Add(page);
        return page;
    }

    #region Public Func

    public void Clear()
    {
        foreach (var data in m_UsingTexture)
        {
            DynamicAtlasMgr.S.ReleaseSaveTextureData(data.Value);
        }
        m_UsingTexture.Clear();
    }

    public void SetTexture(Texture texture, OnCallBackTexRect callBack)
    {
        if (texture == null)
        {
            Debug.Log("Texture is Null");
            callBack(null, new Rect(0, 0, 0, 0));
            return;
        }

        if (texture.width > m_Width || texture.height > m_Height)
        {
            Debug.Log("Texture is too big");
            callBack(null, new Rect(0, 0, 0, 0));
            return;
        }

        if (m_UsingTexture.ContainsKey(texture.name))
        {
            if (callBack != null)
            {
                SaveTextureData textureData = m_UsingTexture[texture.name];
                textureData.referenceCount++;
                Texture2D tex2D = m_PageList[textureData.texIndex].texture;
                // Texture2D tex2D = m_Page.texture;//m_Tex2DLst[textureData.texIndex];
                callBack(tex2D, textureData.rect);
            }

            return;
        }

        GetTextureData data = DynamicAtlasMgr.S.AllocateGetTextureData();
        data.name = texture.name;
        data.callback = callBack;
        m_GetTextureTaskList.Add(data);

        OnRenderTexture(data.name, (Texture2D)texture);
    }

    public void GetTeture(string name, OnCallBackTexRect callback)
    {
        if (m_UsingTexture.ContainsKey(name))
        {
            if (callback != null)
            {
                SaveTextureData data = m_UsingTexture[name];
                data.referenceCount++;
                Texture2D tex2D = m_PageList[data.texIndex].texture;
                // Texture2D tex2D = m_Page.texture;//m_Tex2DLst[data.texIndex];
                callback(tex2D, data.rect);
            }
        }
    }

    /// <summary>
    /// Image组件用完之后
    /// </summary>
    /// <param name="name"></param>
    public void RemoveImage(string name)
    {
        if (m_UsingTexture.ContainsKey(name))
        {
            SaveTextureData data = m_UsingTexture[name];
            data.referenceCount--;
            if (data.referenceCount <= 0)//引用计数0
            {
                // m_Page.RemoveTexture(data.rect);
                m_PageList[data.texIndex].RemoveTexture(data.rect);

                m_UsingTexture.Remove(name);
                DynamicAtlasMgr.S.ReleaseSaveTextureData(data);
            }
        }
    }

    #endregion

    private void OnRenderTexture(string name, Texture2D texture2D)
    {
        if (texture2D == null)
        {
            for (int i = m_GetTextureTaskList.Count - 1; i >= 0; i--)
            {
                GetTextureData task = m_GetTextureTaskList[i];
                if (task.name.Equals(name))
                {
                    if (task.callback != null)
                    {
                        task.callback(null, new Rect(0, 0, 0, 0));
                    }
                }

                DynamicAtlasMgr.S.ReleaseGetTextureData(task);
                m_GetTextureTaskList.RemoveAt(i);
            }

            return;
        }

        int index = 0;
        IntegerRectangle useArea = InsertArea(texture2D.width, texture2D.height, out index);
        Debug.LogError(name + ":Index:" + index);
        Debug.Assert(useArea != null);
        if (useArea == null)
        {
            return;
        }

        Rect uv = new Rect((useArea.x), (useArea.y), texture2D.width, texture2D.height);
        m_PageList[index].AddTexture(useArea.x, useArea.y, texture2D);
        // m_Page.AddTexture(useArea.x, useArea.y, texture2D);

        SaveTextureData saveTextureData = DynamicAtlasMgr.S.AllocateSaveTextureData();
        saveTextureData.texIndex = index;
        saveTextureData.rect = uv;
        m_UsingTexture[name] = saveTextureData;

        for (int i = m_GetTextureTaskList.Count - 1; i >= 0; i--)
        {
            GetTextureData task = m_GetTextureTaskList[i];
            if (task.name.Equals(name))
            {
                m_UsingTexture[name].referenceCount++;

                if (task != null)
                {
                    // Texture2D dstTex = m_Page.texture;//m_Tex2DLst[index];
                    Texture2D dstTex = m_PageList[index].texture;
                    task.callback(dstTex, uv);
                }
            }
            DynamicAtlasMgr.S.ReleaseGetTextureData(task);
            m_GetTextureTaskList.RemoveAt(i);
        }
    }

    private IntegerRectangle InsertArea(int width, int height, out int index)
    {
        IntegerRectangle result = null;

        IntegerRectangle freeArea = null;
        DynamicAtlasPage page = null;
        for (int i = 0; i < m_PageList.Count; i++)
        {
            var tempArea = m_PageList[i].GetFreeArea(width, height);
            if (tempArea != null)
            {
                page = m_PageList[i];
                freeArea = tempArea;

                break;
            }
        }

        if (freeArea == null)
        {
            Debug.LogError("No Free Area----Create New Page");
            page = CreateNewPage();
            index = page.index;
            freeArea = page.freeAreasList[0];
            page.RemoveFreeArea(freeArea);
            return freeArea;
        }

        bool justRightSize = false;
        if (justRightSize == false)
        {
            int paddedWidth = width + m_Padding;
            int paddedHeight = height + m_Padding;
            int resultWidth = paddedWidth > freeArea.width ? freeArea.width : paddedWidth;
            int resultHeight = paddedHeight > freeArea.height ? freeArea.height : paddedHeight;

            result = DynamicAtlasMgr.S.AllocateIntegerRectangle(freeArea.x, freeArea.y, resultWidth, resultHeight);
            GenerateDividedAreasTopFirst(result, freeArea);

        }
        else
        {
            result = DynamicAtlasMgr.S.AllocateIntegerRectangle(freeArea.x, freeArea.y, freeArea.width, freeArea.height);
        }
        page.RemoveFreeArea(freeArea);
        index = page.index;
        return result;
    }


    private void GenerateDividedAreasTopFirst(IntegerRectangle divider, IntegerRectangle freeArea)
    {
        int rightDelta = freeArea.right - divider.right;
        if (rightDelta > 0)
        {
            IntegerRectangle area = DynamicAtlasMgr.S.AllocateIntegerRectangle(divider.right, divider.y, rightDelta, freeArea.height);
            m_PageList[0].AddFreeArea(area);
        }

        int topDelta = freeArea.top - divider.top;
        if (topDelta > 0)
        {
            IntegerRectangle area = DynamicAtlasMgr.S.AllocateIntegerRectangle(divider.x, divider.top, divider.width, topDelta);
            m_PageList[0].AddFreeArea(area);
        }
    }


}


public class DynamicAtlasPage
{
    private int m_Index;
    private Texture2D m_Texture;
    private List<IntegerRectangle> m_FreeAreasList = new List<IntegerRectangle>();
    private int m_Width;
    private int m_Height;
    private int m_Padding;

    public int index => m_Index;
    public Texture2D texture => m_Texture;
    public List<IntegerRectangle> freeAreasList => m_FreeAreasList;

    public DynamicAtlasPage(int index, int width, int height, int padding, Color32[] tempColor)
    {
        m_Index = index;
        m_Width = width;
        m_Height = height;
        m_Padding = padding;

        m_Texture = new Texture2D(width, height, DynamicAtlasConfig.kTextureFormat, false, true);
        m_Texture.filterMode = FilterMode.Bilinear;
        m_Texture.SetPixels32(0, 0, width, height, tempColor);
        m_Texture.Apply(false);
        m_Texture.name = string.Format("DynamicAtlas-{0}-{1}:{2}", width, height, index);

        var area = DynamicAtlasMgr.S.AllocateIntegerRectangle(0, 0, m_Width, m_Height);
        m_FreeAreasList.Add(area);
    }


    public void AddTexture(int posX, int posY, Texture2D srcTex)
    {
        //可以把一张贴图画到另一张贴图上
        Graphics.CopyTexture(srcTex, 0, 0, 0, 0, srcTex.width, srcTex.height, m_Texture, 0, 0, posX, posY);
    }

    public void RemoveTexture(Rect rect)
    {
        int width = (int)rect.width;
        int height = (int)rect.height;
        Color32[] colors = new Color32[width * height];
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = Color.clear;
        }

        m_Texture.SetPixels32((int)rect.x, (int)rect.y, width, height, colors);
        m_Texture.Apply();
    }


    /// <summary>
    /// 搜寻空白区域
    /// </summary>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <returns></returns>
    public IntegerRectangle GetFreeArea(int width, int height)
    {
        if (width > m_Width || height > m_Height)
        {
            Debug.LogError("To Large Texture for atlas");
            return null;
        }

        int paddedWidth = width + m_Padding;
        int paddedHeight = height + m_Padding;

        IntegerRectangle tempArea = null;
        foreach (var area in m_FreeAreasList)
        {
            bool isFitWidth = paddedWidth <= area.width;
            bool isFitHeight = paddedHeight <= area.height;
            if (isFitHeight && isFitWidth)
            {
                // if (tempArea != null)
                // {
                //     if (tempArea.height > area.height)
                //     {
                //         tempArea = area;
                //     }
                // }
                // else
                {
                    tempArea = area;
                    break;
                }

            }
        }

        return tempArea;
    }


    public void AddFreeArea(IntegerRectangle area)
    {
        m_FreeAreasList.Add(area);
    }

    public void RemoveFreeArea(IntegerRectangle area)
    {
        DynamicAtlasMgr.S.ReleaseIntegerRectangle(area);
        m_FreeAreasList.Remove(area);
    }
}