using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GFrame
{
    public class DynamicAtlas
    {
        private int m_Width = 0;
        private int m_Height = 0;
        private int m_Padding = 3;
        // private DynamicAtlasGroup m_DynamicAtlasGroup;

        //-------
        //private Color32[] m_TempColor;
        // private DynamicAtlasPage m_Page;
        private List<DynamicAtlasPage> m_PageList = new List<DynamicAtlasPage>();
        private List<GetTextureData> m_GetTextureTaskList = new List<GetTextureData>();

        private Dictionary<string, SaveTextureData> m_UsingTexture = new Dictionary<string, SaveTextureData>();

        public DynamicAtlas(DynamicAtlasGroup group)
        {
            // m_DynamicAtlasGroup = group;

            int length = (int)group;
            // m_TempColor = new Color32[length * length];
            // for (int i = 0; i < m_TempColor.Length; i++)
            // {
            //     m_TempColor[i] = Color.clear;
            // }

            m_Width = length;
            m_Height = length;
            CreateNewPage();
        }

        DynamicAtlasPage CreateNewPage()
        {
            var page = new DynamicAtlasPage(m_PageList.Count, m_Width, m_Height, m_Padding);//, m_TempColor);
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
                    callback(tex2D, data.rect);
                }
            }
            else
            {
                //FIXME! 这里需要改成自己的加载方式
                var texture = Resources.Load<Texture2D>(name);
                //---------------
                if (texture == null)
                {
                    Debug.LogError("Failed To load Texture:" + name);
                    callback(null, new Rect(0, 0, 0, 0));
                    return;
                }

                GetTextureData data = DynamicAtlasMgr.S.AllocateGetTextureData();
                data.name = texture.name;
                data.callback = callback;
                m_GetTextureTaskList.Add(data);

                OnRenderTexture(data.name, texture);
            }
        }

        /// <summary>
        /// Image组件用完之后
        /// </summary>
        /// <param name="name"></param>
        public void RemoveTexture(string name, bool clearRange = false)
        {
            if (m_UsingTexture.ContainsKey(name))
            {
                SaveTextureData data = m_UsingTexture[name];
                data.referenceCount--;
                if (data.referenceCount <= 0)//引用计数0
                {
                    if (clearRange)
                    {
                        Debug.Log("Remove Texture:" + name);
                        m_PageList[data.texIndex].RemoveTexture(data.rect);
                    }

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
            // Debug.LogError(name + ":Index:" + index);

            if (useArea == null)
            {
                Debug.LogError("No Area");
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
                int fIndex = m_PageList[i].FindFreeArea(width, height);
                if (fIndex >= 0)
                {
                    page = m_PageList[i];
                    freeArea = m_PageList[i].freeAreasList[fIndex];
                    break;
                }
            }

            if (freeArea == null)
            {
                Debug.LogError("No Free Area----Create New Page");
                page = CreateNewPage();
                freeArea = page.freeAreasList[0];
                //直接拿到空白区域
                // page.RemoveFreeArea(freeArea);
                // index = page.index;
                // return freeArea;
            }

            bool justRightSize = false;
            if (justRightSize == false)
            {
                int paddedWidth = width + m_Padding;
                int paddedHeight = height + m_Padding;
                int resultWidth = paddedWidth > freeArea.width ? freeArea.width : paddedWidth;
                int resultHeight = paddedHeight > freeArea.height ? freeArea.height : paddedHeight;

                result = DynamicAtlasMgr.S.AllocateIntegerRectangle(freeArea.x, freeArea.y, resultWidth, resultHeight);
                if (DynamicAtlasConfig.kTopFirst)
                {
                    GenerateDividedAreasTopFirst(page, result, freeArea);
                }
                else
                {
                    GenerateDividedAreasRightFirst(page, result, freeArea);
                }

            }
            else
            {
                result = DynamicAtlasMgr.S.AllocateIntegerRectangle(freeArea.x, freeArea.y, freeArea.width, freeArea.height);
            }
            page.RemoveFreeArea(freeArea);
            index = page.index;
            return result;
        }

        private void GenerateDividedAreasTopFirst(DynamicAtlasPage page, IntegerRectangle divider, IntegerRectangle freeArea)
        {
            int rightDelta = freeArea.right - divider.right;
            if (rightDelta > 0)
            {
                IntegerRectangle area = DynamicAtlasMgr.S.AllocateIntegerRectangle(divider.right, divider.y, rightDelta, divider.height);
                page.AddFreeArea(area);
            }

            int topDelta = freeArea.top - divider.top;
            if (topDelta > 0)
            {
                IntegerRectangle area = DynamicAtlasMgr.S.AllocateIntegerRectangle(freeArea.x, divider.top, freeArea.width, topDelta);
                page.AddFreeArea(area);
            }
        }

        private void GenerateDividedAreasRightFirst(DynamicAtlasPage page, IntegerRectangle divider, IntegerRectangle freeArea)
        {
            int rightDelta = freeArea.right - divider.right;
            if (rightDelta > 0)
            {
                IntegerRectangle area = DynamicAtlasMgr.S.AllocateIntegerRectangle(divider.right, divider.y, rightDelta, freeArea.height);
                page.AddFreeArea(area);
            }

            int topDelta = freeArea.top - divider.top;
            if (topDelta > 0)
            {
                IntegerRectangle area = DynamicAtlasMgr.S.AllocateIntegerRectangle(divider.x, divider.top, divider.width, topDelta);
                page.AddFreeArea(area);
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

        public DynamicAtlasPage(int index, int width, int height, int padding)//, Color32[] tempColor)
        {
            m_Index = index;
            m_Width = width;
            m_Height = height;
            m_Padding = padding;

            m_Texture = new Texture2D(width, height, DynamicAtlasConfig.kTextureFormat, false, true);
            m_Texture.filterMode = FilterMode.Bilinear;
            // m_Texture.SetPixels32(0, 0, width, height, tempColor);
            m_Texture.Apply(false);
            m_Texture.name = string.Format("DynamicAtlas-{0}*{1}-{2}", width, height, index);

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
        public int FindFreeArea(int width, int height)
        {
            if (width > m_Width || height > m_Height)
            {
                Debug.LogError("To Large Texture for atlas");
                return -1;
            }
            IntegerRectangle best = DynamicAtlasMgr.S.AllocateIntegerRectangle(m_Width + 1, m_Height + 1, 0, 0);
            int index = -1;

            int paddedWidth = width + m_Padding;
            int paddedHeight = height + m_Padding;

            // IntegerRectangle tempArea = null;
            for (int i = m_FreeAreasList.Count - 1; i >= 0; i--)
            {
                IntegerRectangle free = m_FreeAreasList[i];

                if (free.x < paddedWidth || free.y < paddedHeight)
                {
                    if (free.x < best.x && paddedWidth <= free.width && paddedHeight <= free.height)
                    {
                        index = i;
                        if ((paddedWidth == free.width && free.width <= free.height && free.right < m_Width) || (paddedHeight == free.height && free.height <= free.width))
                            break;
                        best = free;
                    }
                    else
                    {
                        // Outside the current packed area, no padding required
                        if (free.x < best.x && width <= free.width && height <= free.height)
                        {
                            index = i;
                            if ((width == free.width && free.width <= free.height && free.right < m_Width) || (height == free.height && free.height <= free.width))
                                break;

                            best = free;
                        }
                    }
                }
            }

            return index;
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
}