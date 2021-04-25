using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using GFrame;

public class GetImageTest : MonoBehaviour
{
    [SerializeField] private DynamicImage m_Image;
    [SerializeField] private DynamicImage m_EmptyImage;
    [Header("动态加载外部资源并添加到动态图集中")]
    [SerializeField] private DynamicImage m_EmptyImage2;

    //-------------------------
    [Header("RemoveImage")]
    [SerializeField] private Button m_BtnClear;
    [SerializeField] private DynamicImage m_EmptyImage3;

    private void Awake()
    {
        m_BtnClear.onClick.AddListener(RemoveClear);
    }

    void Start()
    {
        //加载存在的图片
        LoadExistImg();
        //加载不存在的图片
        LoadNotExistImg();
    }


    private void LoadExistImg()
    {
        m_EmptyImage.SetImage("EquipIcon_13");
        m_EmptyImage3.SetImage("EquipIcon_13");
        // m_EmptyImage.RemoveImage();
        // m_Image.RemoveImage();
        // m_EmptyImage.SetImage("EquipIcon_12");
        // m_EmptyImage.SetImage("EquipIcon_13");
    }

    private void LoadNotExistImg()
    {
        m_EmptyImage2.SetImage("EquipIcon_14_Res");
    }

    private void RemoveClear()
    {
        m_EmptyImage3.SetImage("EquipIcon_13");
        m_EmptyImage.RemoveImage();
        m_Image.RemoveImage();
        m_EmptyImage3.RemoveImage(true);
    }

}
