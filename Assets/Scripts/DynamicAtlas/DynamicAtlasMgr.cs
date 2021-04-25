using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GFrame
{
    public class DynamicAtlasMgr : Singleton<DynamicAtlasMgr>
    {
        private Dictionary<DynamicAtlasGroup, DynamicAtlas> m_DynamicAtlasMap = new Dictionary<DynamicAtlasGroup, DynamicAtlas>();

        private List<GetTextureData> m_GetTextureDataList = new List<GetTextureData>();
        private List<SaveTextureData> m_SaveTextureDataList = new List<SaveTextureData>();
        private List<IntegerRectangle> m_IntegerRectangleList = new List<IntegerRectangle>();

        public DynamicAtlas GetDynamicAtlas(DynamicAtlasGroup group)
        {
            DynamicAtlas atlas;
            if (m_DynamicAtlasMap.ContainsKey(group))
            {
                atlas = m_DynamicAtlasMap[group];
            }
            else
            {
                atlas = new DynamicAtlas(group);
                m_DynamicAtlasMap[group] = atlas;
            }
            return atlas;
        }


        public SaveTextureData AllocateSaveTextureData()
        {
            if (m_SaveTextureDataList.Count > 0)
            {
                return m_SaveTextureDataList.Pop();
            }
            SaveTextureData data = new SaveTextureData();
            return data;
        }

        public void ReleaseSaveTextureData(SaveTextureData data)
        {
            m_SaveTextureDataList.Add(data);
        }

        public GetTextureData AllocateGetTextureData()
        {
            if (m_GetTextureDataList.Count > 0)
            {
                return m_GetTextureDataList.Pop();
            }
            GetTextureData data = new GetTextureData();
            return data;
        }

        public void ReleaseGetTextureData(GetTextureData data)
        {
            m_GetTextureDataList.Add(data);
        }

        public IntegerRectangle AllocateIntegerRectangle(int x, int y, int width, int height)
        {
            if (m_IntegerRectangleList.Count > 0)
            {
                IntegerRectangle rectangle = m_IntegerRectangleList.Pop();
                rectangle.x = x;
                rectangle.y = y;
                rectangle.width = width;
                rectangle.height = height;
            }
            return new IntegerRectangle(x, y, width, height);
        }

        public void ReleaseIntegerRectangle(IntegerRectangle rectangle)
        {
            m_IntegerRectangleList.Add(rectangle);
        }

        public void ClearAllCache()
        {
            m_GetTextureDataList.Clear();
            m_SaveTextureDataList.Clear();
            m_IntegerRectangleList.Clear();
        }
    }


    public static class ListExtension
    {
        static public T Pop<T>(this List<T> list)
        {
            T result = default(T);
            int index = list.Count - 1;
            if (index >= 0)
            {
                result = list[index];
                list.RemoveAt(index);
                return result;
            }
            return result;
        }
    }
}