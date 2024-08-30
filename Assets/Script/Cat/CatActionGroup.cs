using Assets.Script.My;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;



/**
 * 储存ActionEditor使用的公共数据
 * 
 * @author Cat
 * @version 1.0.1
 */
public class CatActionGroup
{

    public bool smooth;//是否打开动作平滑

    public static int PLAYER_BUFF_SIZE = 16;// 默认player buff大小

    public CatPlayerr[] players = new CatPlayerr[PLAYER_BUFF_SIZE];

    public int playerBuffIndex = 0;

    public CatModule[] modules; // 模块模板

    public CatFrame[] frames; // 这里的模块都是克隆来的，所以位置不同

    public CatAction[] actions; // 这里的Frame都是引用

    public string[] imgPath;// img路径

    public bool isLinkFile;// 是否链接文件

    public string path;// 动作文件路径

    public Texture2D[] imgSource;
    public Material[] material;


    private bool isSprite = false;
    public CatActionGroup(string path, bool enableFreeRotate, bool smooth, bool longImageArray, bool usURPLit, bool _bilinear, bool minmap, bool linear)
    {
        this.smooth = smooth;
        this.path = path;
        this.enableFreeRotate = enableFreeRotate;
        this.longImageArray = longImageArray;
        //try
        //{

        JavaReader din = AssetsLoader.getAsset(MyStatic.spriteRoot + path);
        isSprite = true;
        if (din == null)
        {
            isSprite = false;
            din = AssetsLoader.getAsset(MyStatic.spriteHRoot + path);
        }

        if (din == null)
        {
            Debug.LogError("未加载到任何的player文件..检查该文件是否存在===" + path);
            return;
        }
        //LoggerFwq.LogInfoError("path:" + path);
        read(din, enableFreeRotate, usURPLit, _bilinear, minmap, linear);
        din.close();
        //}
        //catch (Exception e)
        //{
        //	Debug.Log(e.StackTrace);
        //}
    }


    public void addPlayer(CatPlayerr player)
    {
        bool added = false;
        if (playerBuffIndex == PLAYER_BUFF_SIZE)
        {// 超界
            for (int i = 0; i < players.Length; i++)
            {
                if (players[i] == null)
                {
                    added = true;
                    players[i] = player;
                    return;
                }
            }
        }
        else
        {
            added = true;
            players[playerBuffIndex] = player;
            return;
        }
        if (!added)
        {
            PLAYER_BUFF_SIZE <<= 1;
            CatPlayerr[] players = new CatPlayerr[PLAYER_BUFF_SIZE];// 模块buff。如果开启，则imgSource永远置空
            Array.Copy(this.players, 0, players, 0, this.players.Length);
            this.players[PLAYER_BUFF_SIZE >> 1] = player;
        }
        // buff index增加
        playerBuffIndex++;
    }

    public void removePlayer(CatPlayerr player)
    {
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] != null && players[i].Equals(player))
            {
                players[i] = null;
                return;
            }
        }
    }


    private static List<string> textureKeys = new List<string>();
    private static List<Texture2D> textureBuffers = new List<Texture2D>();
    private static List<int> textureSums = new List<int>();


    public bool enableFreeRotate;//自由旋转属性
    public bool enableAvatar;//模块分层
    public bool enableActionOffset;//模块位移
    public bool longImageArray;//图片最高数量使用int
    public static readonly string DEF_ENABLE_FREE_ROTATE = "enableFreeRotate";
    public static readonly string DEF_ENABLE_AVATAR = "enableAvatar";
    public static readonly string DEF_ENABLE_ACTION_OFFSET = "enableActionOffset";
    public static readonly string DEF_ENABLE_LONG_IMAGE_ARRAY = "long-image-array";
    /**
	 * 动作文件文件读取 （暂不支持link模式动作文件的读取）
	 * 
	 * @param din
	 * @param enableFreeRotate 
	 */
    private void read(JavaReader din, bool enableFreeRotate, bool usURPLit, bool _bilinear, bool minmap, bool linear)
    {

        //shader = Shader.Find("Unlit/Transparent Colored");

        //try
        //{
        int size = 0;

        longImageArray = true;
        size = din.readInt();
        if (size > 1000 || size < 0)
        {
            din.br.BaseStream.Position -= 4;
            size = din.readByte();
            if (size < 0)
            {
                size += 256;
            }
            longImageArray = false;
        }

        imgPath = new string[size];
        imgSource = new Texture2D[size];
        material = new Material[size];

        for (int i = 0; i < size; i++)
        {
            imgPath[i] = din.readUTF() + CatSys.imageSuffix;
            for (int j = 0; j < i; j++)
            {
                if (imgPath[i].Equals(imgPath[j]))
                {
                    Debug.Log("cat-engine" + "Sprite Image Duplicate " + imgPath[i]);
                }
            }

            if (imgPath[i].EndsWith(".png"))
            {
                imgPath[i] = imgPath[i].Substring(0, imgPath[i].Length - 4);
            }

            string path = imgPath[i];

            // 载入texture
            Texture2D mTexture = null;
            if (isSprite)
            {
                path = "sprite/" + path;
                mTexture = getTextureFromBuffer(path, _bilinear, minmap, linear);
            }
            else
            {
                path = "spriteH/" + path;
                mTexture = getTextureFromBuffer(path, _bilinear, minmap, linear);
            }

            imgSource[i] = mTexture;

            if (usURPLit)

            {
                material[i] = new Material(ResourceStatic.share_URP_2D_Lit());
            }

            else
            {
                material[i] = new Material(ResourceStatic.share_Stand());
            }

            //bug
            material[i].mainTexture = imgSource[i];

        }
        int sum = din.readShort();
        modules = new CatModule[sum];
        for (int i = 0; i < sum; i++)
        {
            modules[i] = new CatModule();
            modules[i].read(i, din, longImageArray);

            int mindex = modules[i].imgIndex;
            TextureRegion region = new TextureRegion(imgSource[mindex], modules[i].x, modules[i].y, modules[i].width, modules[i].height);
            region.setMatrial(material[mindex]);
            modules[i].setTextureRegion(region);
            modules[i].textureRegion.setMatrialIndex(mindex);

            modules[i].image = new TextureRegion(imgSource[mindex]);
            modules[i].image.setMatrialIndex(mindex);
        }
        sum = din.readShort();
        frames = new CatFrame[sum];
        for (int i = 0; i < sum; i++)
        {
            frames[i] = new CatFrame(this);
            frames[i].read(i, din, this);
        }
        sum = din.readShort();
        actions = new CatAction[sum];
        for (int i = 0; i < sum; i++)
        {
            actions[i] = new CatAction();
            actions[i].read(i, din, this);
        }
        //}
        //catch (Exception e)
        //{
        //	Debug.LogError(e.StackTrace);
        //	Debug.LogError(e.Message);
        //}
    }

    private bool cleared;
    /**
	 * 清除掉此动作的缓冲资源
	 */
    public void clear()
    {
        if (cleared)
        {
            return;
        }
        cleared = true;
        List<string> ptextureKeys = new List<string>();
        List<Texture2D> ptextureBuffers = new List<Texture2D>();
        List<int> ptextureSums = new List<int>();
        //清除texture和textureRegion
        for (int i = 0; i < imgPath.Length; i++)
        {
            for (int j = 0; j < textureKeys.Count; j++)
            {
                if (textureKeys[j].Equals(imgPath[i]))
                {
                    if (textureSums.Count > j)
                    {
                        int sum = textureSums[j];
                        if (sum == 1)
                        {
                            ptextureKeys.Add(textureKeys[j]);
                            ptextureBuffers.Add(textureBuffers[j]);
                            ptextureSums.Add(j);

                            textureSums[j] = 0;//设置为0（以后不作响应）

                            //imgSource[i].recycle();//TODO android

                        }
                        else if (sum > 1)
                        {
                            textureSums[j] = sum - 1;
                        }
                    }
                    else
                    {
                        Debug.LogError("cat-engine" + "===========================================================Clear Image Error " + path);
                    }

                    break;
                }
            }
        }

        for (int i = 0; i < ptextureKeys.Count; i++)
        {
            textureKeys.Remove(ptextureKeys[i]);
        }

        for (int i = 0; i < ptextureBuffers.Count; i++)
        {
            textureBuffers.Remove(ptextureBuffers[i]);
        }


        for (int i = 0; i < textureSums.Count; i++)
        {
            if (textureSums[i] == 0)
            {
                textureSums.RemoveAt(i);
                i--;
            }
        }
        //清理零散资源
        for (int i = 0; i < imgSource.Length; i++)
        {
            imgSource[i] = null;
        }
        imgSource = null;
        for (int i = 0; i < modules.Length; i++)
        {
            modules[i].clear();
            modules[i] = null;
        }
        modules = null;
        for (int i = 0; i < frames.Length; i++)
        {
            frames[i].clear();
            frames[i] = null;
        }
        modules = null;
        for (int i = 0; i < actions.Length; i++)
        {
            actions[i].clear();
            actions[i] = null;
        }
        modules = null;
        for (int i = 0; i < imgPath.Length; i++)
        {
            imgPath[i] = null;
        }
        imgPath = null;
        path = null;
    }

    /**
	 * 强制清空内存
	 */
    public static void clearAll()
    {
        textureKeys.Clear();
        for (int i = 0; i < textureBuffers.Count; i++)
        {
            //textureBuffers.get(i).recycle();
        }
        textureBuffers.Clear();
        textureSums.Clear();
    }

    private int loadedIndex;//-1表示新
    public Texture2D getTextureFromBuffer(string key, bool _Bilinear, bool minmap, bool linear)
    {
        int targetIndex = -1;

        for (int i = 0; i < textureKeys.Count; i++)
        {
            if (textureKeys[i].Equals(key))
            {
                targetIndex = i;
                break;
            }
        }
        if (targetIndex != -1)
        {
            textureSums[targetIndex] = textureSums[targetIndex] + 1;
            loadedIndex = targetIndex;
            return textureBuffers[targetIndex];
        }
        else
        {
            Texture2D img = null;
            try
            {
                img = TextureManager.loadImagePng(key, minmap, linear);

                //此处linear概念 暂时这么用 后续有需要再修改
                img.wrapMode = TextureWrapMode.Clamp;
                if (_Bilinear)
                    img.filterMode = FilterMode.Trilinear;
                else
                    img.filterMode = FilterMode.Point;
            }
            catch (IOException e)
            {
                Debug.Log(e.StackTrace);
            }
            textureKeys.Add(key);
            textureBuffers.Add(img);
            textureSums.Add((1));
            loadedIndex = -1;
            return img;
        }

    }

    public void setImgFilterMode(FilterMode mode)
    {
        for (int i = 0; i < imgSource.Length; i++)
        {
            if (imgSource[i] != null)
            {
                imgSource[i].filterMode = mode;
            }
        }
    }


    /**
	 * @see java.lang.Object#toString()
	 */
    public string toString()
    {
        return "frames=" + (frames == null ? "null" : ("" + frames.Length)) + " action=" + (actions == null ? "null" : ("" + actions.Length));
    }

}
