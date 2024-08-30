using System;
using System.Collections.Generic;

using UnityEngine;


/**
 * animation播放器
 * @author Cat
 * @version 1.0.1
 * 
 * ===============
 * 有改动，非原版，仅用于资源读用
 */

public class CatPlayerr
{



    private MeshGroupRender meshGroup;



    [HideInInspector]
    public static bool autoRelease = false;//自动释放资源（不需要调用clear）


    [HideInInspector]
    public int currActionId = -1;//当前动作ID
    [HideInInspector]
    public int currentFrameID;// 当前帧ID


    //[HideInInspector]
    //public bool isEnd = true;// 当前动画是否结束 
    //[HideInInspector]
    //public bool isCurrEnd = true;// 当前动画序列是否结束
    //[HideInInspector]
    //public bool isLastFrame = true;// 当前动画是否到达本次结束(归零时为true)


    //[HideInInspector]
    //public int currLast;//当前帧持续时间
    //[HideInInspector]
    //public int playCount = 0;// 播放次数


    //是否需要刷新
    public bool needRefresh = false;


    [HideInInspector]
    public CatActionGroup ag;//actionGroup 动作数据组



    public CatPlayerr()
    {

        meshGroup = new MeshGroupRender();
    }

    public GameObject depandObject;

    public CatPlayerr(GameObject obj)
    {

        this.depandObject = obj;

        MeshRenderer meshRender = obj.GetComponent<MeshRenderer>();
        if (meshRender == null)
            meshRender = obj.AddComponent<MeshRenderer>();

        MeshFilter meshFilter = obj.GetComponent<MeshFilter>();
        if (meshFilter == null)
            meshFilter = obj.AddComponent<MeshFilter>();


        meshGroup = new MeshGroupRender(meshRender, meshFilter);
    }

    public Material[] materials;

    private static Dictionary<string, object> dictMaterials = new Dictionary<string, object>();
    public static Material[] getDict(string key)
    {
        if (!dictMaterials.ContainsKey(key))
            return null;
        return (Material[])dictMaterials[key];
    }





    //存储动作文件
    public static List<String> loadedActionFile = new List<String>();
    public static List<CatActionGroup> loadedActionGroup = new List<CatActionGroup>();
    public static List<int> loadedActionFileSum = new List<int>();//计数器

    public void setCatPlayerr(String path)
    {
        init(path, CatSys.DEFAULT_ENABLE_FREE_ROTATE, false, false);
    }

    public void setCatPlayerr(String path, bool centerRotate)
    {
        init(path, CatSys.DEFAULT_ENABLE_FREE_ROTATE, false, false);
        setCenterRotateMode(centerRotate);
    }

    public void setCatPlayerr(String path, bool smoth = false, bool useURPLit = false,bool _bilinear = false, bool minmap = false, bool linear = false)
    {
        bool centerRotate = true;
        bool enableFreeRotate = true;

        init(path, enableFreeRotate, smoth, true, useURPLit, _bilinear, minmap, linear);
        setCenterRotateMode(enableFreeRotate && centerRotate);
    }

    //设置排序
    public void setSorting(int sortingLayer, int sortingOrder)
    {
        if (sortingOrder < -32768 || sortingOrder > 32767)
        {
            Debug.LogError("超出sortingOrder范围  Note that the value is -32768 to 32767.");
        }
        MeshRenderer render = meshGroup.getMeshRenderer();
        render.sortingLayerID = sortingLayer;
        render.sortingOrder = sortingOrder;
    }

    //设置排序
    public void setSorting(string sortingLayerName)
    {
        getMeshGroup().getMeshRenderer().sortingLayerName = sortingLayerName;
    }



    //设置排序
    public void setUIRenderQueue(int rendQueue, int sortingOrder)
    {
        if (sortingOrder < -32768 || sortingOrder > 32767)
        {
            Debug.LogError("超出sortingOrder范围  Note that the value is -32768 to 32767.");
        }

        for(int i = 0; i < materials.Length; i++)
        {
            materials[i].renderQueue = rendQueue;
        }

        MeshRenderer render = meshGroup.getMeshRenderer();

        if (render.sharedMaterial != null)
            render.sharedMaterial.renderQueue = rendQueue;
        render.sortingOrder = sortingOrder;
    }


    public MeshGroupRender getMeshGroup()
    {
        return meshGroup;
    }


    public void init(string path, bool enableFreeRotate, bool smooth, bool longImageArray, bool useURPLit = false,bool _bilinear = false, bool minmap = false, bool linear = false)
    {
        path = path.EndsWith(".bin") ? path : path + ".bin";
        key = path;

        int index = loadedActionFile.IndexOf(path);
        if (index != -1)
        {
            //直接调用模块，不读入。
            CatActionGroup agTemp = loadedActionGroup[index];
            int sum = (loadedActionFileSum[index]);
            loadedActionFileSum[index] = sum + 1;
            this.ag = agTemp;
        }
        else
        {

            CatActionGroup agTemp = new CatActionGroup(path, enableFreeRotate, smooth, longImageArray, useURPLit, _bilinear, minmap, linear);
            this.ag = agTemp;

            loadedActionFile.Add(path);
            loadedActionGroup.Add(ag);
            loadedActionFileSum.Add(1);
        }

        materials = ag.material;

        //检测后续的加载是否要求mipmap
        if(minmap)
        {
            foreach(Material material in ag.material)
            {
                if(material.mainTexture.mipmapCount <= 1)
                {
                    //Debug.LogError("后续的加载有mipmap的要求  需要将前置的加载方式mipmap设置为true");
                    break;
                }
            }
        }

        ag.addPlayer(this);
    }


    public string key;
    /// <summary>
    /// 
    /// </summary>
    /// <param name="shader"></param>
    /// <param name="independ">独立材质  是否参与缓存之类的</param>
    public void setShader(Shader shader, bool isIndepend = false)
    {
        key = getKey(ag.path ,shader);

        if (!isIndepend && dictMaterials.ContainsKey(key))
        {
            object ob = dictMaterials[key];
            materials = (Material[])ob;
        }
        else
        {
            materials = new Material[materials.Length];
            for (int i = 0; i < materials.Length; i++)
            {
                materials[i] = new Material(shader);
                materials[i].mainTexture = ag.material[i].mainTexture;
            }
            if (!isIndepend)
                dictMaterials.Add(key, materials);
        }
    }

    public void setMaterials(Material[] materials, bool isSetTexture = true)
    {
        this.materials = materials;
        if (isSetTexture)
        {
            for (int i = 0; i < materials.Length; i++)
            {
                materials[i].mainTexture = ag.material[i].mainTexture;
            }
        }
    }

    /// <summary>
    /// 补充 .bin后缀
    /// </summary>
    /// <param name="path"></param>
    /// <param name="shader"></param>
    /// <returns></returns>
    public static string getKey(string path, Shader shader)
    {
        if (!path.EndsWith(".bin"))
        {
            path += ".bin";
        }

        if (shader == null)
        {

            return path;
        }

        return path + "_" + shader.name;
    }


    public void setFilter(FilterMode filterMode)
    {
        for (int i = 0; i < materials.Length; i++)
        {
            materials[i].mainTexture.filterMode = filterMode;
            ((Texture2D)materials[i].mainTexture).Apply();
        }
    }


    ///**
    // * 重设动作播放
    // */
    //public void reset(){
    //	currentFrameID=0;
    //	currLast=0;
    //	currActionId=0;
    //	isEnd = true;
    //	isCurrEnd = true;
    //	isLastFrame = true;
    //	playCount = 0;
    //}


    //private bool isKeepLastFrame = false;
    //private bool keepLastFrame = false;

    ////正向动作播放
    //public void setAction(int action, int loopCount, bool _keepLastFrame = false)
    //{
    //    isReverseAction = false;
    //    this.isKeepLastFrame = _keepLastFrame;
    //    keepLastFrame = false;

    //    currActionId = action;
    //    playCount = loopCount;
    //    currentFrameID = 0;
    //    currLast = 0;
    //    isEnd = false;
    //    isCurrEnd = false;
    //    isLastFrame = false;

    //    needRefresh = true;
    //}

    ////反向播放
    //public void setActionReverse(int action, int loopCount, bool _keepLastFrame = false)
    //{
    //    isReverseAction = true;
    //    this.isKeepLastFrame = _keepLastFrame;
    //    keepLastFrame = false;

    //    currActionId = action;
    //    playCount = loopCount;
    //    currentFrameID = ag.actions[currActionId].frames.Length - 1;
    //    currLast = 0;
    //    isEnd = false;
    //    isCurrEnd = false;
    //    isLastFrame = false;

    //    needRefresh = true;
    //}

    //public bool isReverse()
    //{
    //    return isReverseAction;
    //}


    /**
	 * 播放当前动作
	 */
    //public void playAction()
    //{
    //    if (keepLastFrame) return;

    //    if (isReverseAction)
    //    {
    //        //倒叙播放
    //        playReverseAction();
    //    }
    //    else
    //    {
    //        //正序播放
    //        playPositiveAction();
    //    }
    //}

    //public void playLastFrame()
    //{
    //    if (isReverseAction)
    //    {
    //        //倒叙播放
    //        if (currentFrameID > 0)
    //        {
    //            currentFrameID = 0;
    //        }
    //    }
    //    else
    //    {
    //        //正序播放  
    //        if (currentFrameID < ag.actions[currActionId].frames.Length)
    //        {
    //            currentFrameID = ag.actions[currActionId].frames.Length - 1;
    //        }
    //    }
    //}

    //private bool updateCurrentFrameID = false;

    #region 正序播放

    //private void playPositiveAction()
    //{
    //    //如果是最后一帧  并且需要保持状态  就一直继续return掉
    //    if (isEnd) return;

    //    if (keepLastFrame) return;

    //    //没有动画啊
    //    if (ag.actions[currActionId].frames.Length <= 1)
    //    {
    //        isLastFrame = true;
    //        return;
    //    }
            

    //    isLastFrame = false;

    //    addFramePerTime();

    //    if (updateCurrentFrameID)
    //    {
    //        updateCurrentFrameID = false;
    //        currLast = 0;//走下一帧，时间线归零

    //        if (currentFrameID < ag.actions[currActionId].frames.Length - 1)
    //        {
    //            currentFrameID++;

    //            if (currentFrameID >= ag.actions[currActionId].frames.Length - 1)
    //            {
    //                //notifyLastFrame();
    //            }
    //            needRefresh = true;
    //        }
    //        else
    //        {//最后一帧已经走完
    //            playCount--;
    //            isCurrEnd = true;

    //            if (playCount == 0)
    //            {//标记结束帧
    //                isEnd = true;
    //                //notifyFinish();

    //                if (isKeepLastFrame)
    //                {
    //                    keepLastFrame = true;
    //                }
    //                return;
    //            }
    //            currentFrameID = 0;
    //            needRefresh = true;
    //        }
    //    }

    //    if (currLast >= ag.actions[currActionId].lastTime[currentFrameID])
    //    {
    //        updateCurrentFrameID = true;

    //        if (currentFrameID < ag.actions[currActionId].frames.Length - 1)
    //        {
    //            isLastFrame = false;
    //        }
    //        else
    //        {
    //            isLastFrame = true;
    //        }
    //    }
    //}
    #endregion


    #region 倒叙播放

    //private bool isReverseAction = false;


    //private void playReverseAction()
    //{


    //    //如果是最后一帧  并且需要保持状态  就一直继续return掉
    //    if (isEnd)
    //    {
    //        return;
    //    }

    //    //没有动画啊
    //    if (ag.actions[currActionId].frames.Length <= 1)
    //    {
    //        return;
    //    }

    //    if (currLast >= ag.actions[currActionId].lastTime[currentFrameID])
    //    {
    //        currLast = 0;//走下一帧，时间线归零
    //        if (currentFrameID > 0)
    //        {
    //            currentFrameID--;
    //            isLastFrame = false;

    //            if (currentFrameID == 0)
    //            {
    //                //notifyLastFrame();
    //            }
    //            needRefresh = true;
    //        }
    //        else
    //        {//最后一帧已经走完
    //            isLastFrame = true;
    //            playCount--;
    //            isCurrEnd = true;

    //            if (playCount == 0)
    //            {
    //                isEnd = true;
    //                //notifyFinish();
    //                if (isKeepLastFrame)
    //                {
    //                    keepLastFrame = true;
    //                }
    //                return;
    //            }

    //            needRefresh = true;
    //            currentFrameID = ag.actions[currActionId].frames.Length - 1;
    //        }
    //    }
    //    addFramePerTime();
    //}

    #endregion


    #region 时间驱动类型
    public enum TimeDrive
    {
        RealityTime,
        UpdateTime
    }

    private TimeDrive timeDrive = TimeDrive.RealityTime;
    public void setTimeDrive(TimeDrive timeDrive)
    {
        this.timeDrive = timeDrive;
    }

    private static int updateTimeMM;
    public static void setUpdateTime(int _mm)
    {
        updateTimeMM = _mm;
    }

    //private void addFramePerTime()
    //{
    //    switch(timeDrive)
    //    {
    //        case TimeDrive.RealityTime:
    //            currLast += (int)(Time.deltaTime*1000);
    //            break;
    //        case TimeDrive.UpdateTime:
    //            currLast += updateTimeMM;
    //            break;
    //    }

    //    //currLast += TimeInfo.elapsedTime_MM;
    //    currLast += 1000;
    //}

    #endregion

    public void paintActionFrame(int actionId, int frameId, float x, float y)
    {
        //检查输入的Action和Frame是否越界
        if(actionId < getActionSum() && frameId < getAction(actionId).frames.Length)
        {
            getAction(actionId).getFrame(frameId).paintFrame(this, getMeshGroup(), x, y, false);
        }
       
    }


    //public void paintActionFrame(int actionId, int frameId, float x, float y, float playerRotate, bool centerRotate, float scaleX, float scaleY)
    //{
    //    if (getAction(actionId).getFrame(frameId)==null)
    //    {
    //        //Logger.LogInfoError("is null " + " actionId:" + actionId + " frameId:" + frameId);
    //        return;
    //    }

    //    getAction(actionId).getFrame(frameId).paintFrame(this, getMeshGroup(), x, y, playerRotate, centerRotate, scaleX, scaleY);

    //}


    //public void paintFrame(int frameId, float x, float y, int playerRotate, bool centerRotate, float scaleX, float scaleY)
    //{
    //    ag.frames[frameId].paintFrame(this, getMeshGroup(), x, y, playerRotate, centerRotate, scaleX, scaleY);
    //}


    public void paint()
    {
        paint(0,0);
    }


    public void paint(float offx, float offy, float scale = 1)
    {
        CatFrame frame = ag.actions[currActionId].frames[currentFrameID];
        frame.paintFrame(this, meshGroup, offx, offy, rotateAngle, centerRotate, scale, scale);

        needRefresh = false;
    }



    public void updatePaint()
    {
        if(meshGroup != null)
            meshGroup.endDrawed();
        needRefresh = false;
    }


    /**
	 * 设置全部沿中心旋转模块
	 * @param centerRotate 物理引擎设置为true
	 */
    private bool centerRotate = false;// true情况为每帧只有一个模块时使用
    public void setCenterRotateMode(bool centerRotate)
    {
        this.centerRotate = centerRotate;
    }

    private float rotateAngle;// in degree 第一象限逆时针（同数学中的标准坐标系）
    public void setRotate(float rotateAngle)
    {
        this.rotateAngle = rotateAngle;
    }
    public float getRotateAngle()
    {
        return rotateAngle;
    }

    /**
	 * 取得当前帧
	 * @return Frame
	 */
    public CatFrame getCurrFrame()
    {
        return ag.actions[currActionId].frames[currentFrameID];
    }
    /**
	 * 取得上一帧
	 * @return Frame
	 */
    public CatFrame getLastFrame()
    {
        int lastFrameId = currentFrameID - 1;
        if (currentFrameID == 0)
        {
            lastFrameId = 0;
        }
        return ag.actions[currActionId].frames[lastFrameId];
    }

    /**
	 * 取得当前帧的面积范围
	 * @param farmeId 帧编号
	 * @return Rectangle
	 */
    public Rect getRectangle(int farmeId)
    {
        return ag.actions[currActionId].frames[farmeId].getRectangle();
    }
    /**
	 * 是否结束
	 */
    //public bool isEnded()
    //{
    //    return isEnd;
    //}
    /**
	 * 是否结束
	 */
    //public bool isCurrEndd()
    //{
    //    return isCurrEnd;
    //}

    /**
	 * 是否最后一帧
	 */
    //public bool isLastFramed()
    //{
    //    return isLastFrame;
    //}

    //是否是该帧第一次播放
    //public bool isFrameFirstTime()
    //{
    //    if (currLast <= 0) return true;
    //    return false;
    //}

    /**
	 * 取得当前动作
	 * @return Action
	 */
    public CatAction getCurrAction()
    {
        return ag.actions[currActionId];
    }


    public int getCurrActionFrameSum()
    {
        return ag.actions[currActionId].frames.Length;
    }

    public int getCurrActionFrameSum(int actionId)
    {
        return ag.actions[actionId].frames.Length;
    }


    /**
	 * 通过ID取得帧
	 * @param id 帧的ID
	 * @return FrameID
	 */
    public CatFrame getFrame(int id)
    {
        return ag.frames[id];
    }

    /**
	 * 取得动作
	 * @param id 动作ID
	 * @return ActionID
	 */
    public CatAction getAction(int id)
    {

#if UNITY_EDITOR
        if (id < 0 || id > ag.actions.Length - 1)
        {
            Debug.LogError("id====" + id);
        }
#endif

        return ag.actions[id];
    }

    /**
     * @return 动作数量
     */
    public int getActionSum()
    {
        return ag.actions.Length;
    }

    public int getFrameSum()
    {
        return ag.frames.Length;
    }


    public bool cleared;

    public bool clearedd()
    {
        return cleared;
    }
    /**
	 * 释放内存：2种模式，如果是pool模式则交给pool处理
	 */
    public void clear()
    {
        if (cleared || autoRelease)
        {
            return;
        }
        cleared = true;
        int index = loadedActionFile.IndexOf(ag.path);
        if (index == -1)
        {
            Debug.LogError("cat-engine" + "===================================error occurs when clear the Playerr[" + ag.path + "]");
            return;
        }
        int sum = ((int)loadedActionFileSum[index]);
        if (sum > 1)
        {
            loadedActionFileSum[index] = (sum - 1);
            //清空引用
        }
        else
        {
            loadedActionFile.RemoveAt(index);
            loadedActionGroup.RemoveAt(index);
            ag.clear();
            loadedActionFileSum.RemoveAt(index);
        }
        ag.removePlayer(this);
    }

    /// <summary>
    /// 未完成
    /// </summary>
    public void reload()
    {//TODO 此方法没有完成，要做只释放图片的方法

    }

    /**
	 * 释放内存：2种模式，如果是pool模式则交给pool处理
	 */
    private void _clear()
    {
        if (cleared)
        {
            return;
        }
        cleared = true;
        int index = loadedActionFile.IndexOf(ag.path);
        if (index == -1)
        {
            Debug.LogError("cat-engine" + "===================================error occurs when clear the Playerr[" + ag.path + "]");
            return;
        }
        int sum = ((int)loadedActionFileSum[index]);
        if (sum > 1)
        {
            loadedActionFileSum[index] = (sum - 1);
            //清空引用
        }
        else
        {
            loadedActionFile.RemoveAt(index);
            loadedActionGroup.RemoveAt(index);
            ag.clear();
            loadedActionFileSum.RemoveAt(index);
        }
        ag.removePlayer(this);
    }

    /**
	 * 清理全部资源
	 */
    public static void clearAll()
    {
        for (int i = 0; i < loadedActionGroup.Count; i++)
        {//TODO 测试buf情况
            loadedActionGroup[i].clear();
        }
        loadedActionFile.Clear();
        loadedActionGroup.Clear();
        loadedActionFileSum.Clear();
    }


    public void destroy()
    {
        if(meshGroup != null)
        {
            meshGroup.destroy();
        }
    }

}
