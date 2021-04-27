using UnityEngine;
using UnityEngine.UI;

// [ExecuteInEditMode]
public class DynamicImage : Image
{
    public DynamicAtlasGroup atlasGroup = DynamicAtlasGroup.Size_1024;

    private DynamicAtlasGroup m_Group;
    private DynamicAtlas m_Atlas;
    private Sprite m_DefaultSprite;

    protected override void Start()
    {
        base.Start();
#if UNITY_EDITOR
        //在编辑器下 退出playmode会再走一次start
        if (Application.isPlaying)
        {
            OnPreDoImage();
        }
#else
       OnPreDoImage();
#endif
    }

    private void OnPreDoImage()
    {
        if (sprite != null)//事先挂载了一张图片
        {
            //可以先放入到图集中去，在使用这一张图集里面的图片
            SetGroup(atlasGroup);
            SetImage();
        }

    }

    private void SetGroup(DynamicAtlasGroup group)
    {
        if (m_Atlas != null)
        {
            return;
        }

        m_Group = group;
        m_Atlas = DynamicAtlasMgr.S.GetDynamicAtlas(group);
    }

    private void SetImage()
    {
        m_DefaultSprite = sprite;
        m_Atlas.SetTexture(mainTexture, OnGetImageCallBack);
    }




    private void OnGetImageCallBack(Texture tex, Rect rect)
    {
        Debug.LogError(111);
        int length = (int)m_Group;
        Rect spriteRect = rect;
        // spriteRect.x *= length;
        // spriteRect.y *= length;
        // spriteRect.width *= length;
        // spriteRect.height *= length;

        sprite = Sprite.Create((Texture2D)tex, spriteRect, m_DefaultSprite.pivot, m_DefaultSprite.pixelsPerUnit, 1, SpriteMeshType.Tight, m_DefaultSprite.border);
    }



    #region Public Func
    public void SetImage(string name)
    {
        if (m_Atlas == null)
        {
            SetGroup(atlasGroup);
        }

        // m_Atlas.GetImage();
    }
    #endregion
}
