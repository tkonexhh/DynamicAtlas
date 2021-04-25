# DynamicAtlas
动态图集使用方案
算法主要来自https://github.com/DaVikingCode/UnityRuntimeSpriteSheetsGenerator

使用场景一般用于更新频率不大的UI界面，并且如果使用图集的话，往往会用到一张大图集上的几张图片而已。

使用方法：
想要使用动态图集的Image，使用DynamicImage组件，直接挂载的Sprite会在运行时打入动态图集中，
动态图集可以选择分辨率大小

以下是一些重要函数说明：
#DynamicImage
  public void SetImage(string name)     运行时动态加载资源 
  public void RemoveImage(bool clearRange = false)    当界面需要关闭时，需要移除动态图集上的资源，内部采用引用计数
  

#DynamicAtlasMgr
  public void GetTeture(string name, OnCallBackTexRect callback)  这里需要改成你自己的加载图片的方式，DEMO里面用的是Resources，肯定不合适
