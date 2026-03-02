using System;
using UnityEngine;

/// <summary>
/// 帧 - 记录一帧中包含哪些模块以及它们的位置/变换信息
/// </summary>
public class CatFrame
{
    public int id;
    public CatModule[] modules;
    public float[] locx;
    public float[] locy;

    // 帧级别变换
    public float frameScaleX = 1.0f;
    public float frameScaleY = 1.0f;
    public float transparency = 1.0f;

    // 模块级别变换
    public float[] moduleScaleX;
    public float[] moduleScaleY;
    public float[] moduleTransparency;

    private CatActionGroup ag;

    public CatFrame() { }

    public CatFrame(CatActionGroup ag)
    {
        this.ag = ag;
    }

    public void read(int id, JavaReader din, CatActionGroup datas)
    {
        this.id = id;
        this.ag = datas;

        try
        {
            if (datas.enableFreeRotate)
            {
                din.readShort();  // rotateAngle
                din.readShort();  // rotateRefX
                din.readShort();  // rotateRefY
                transparency = din.readFloat();
                frameScaleX = din.readFloat();
                frameScaleY = din.readFloat();
                din.readShort();  // frameScaleRefX
                din.readShort();  // frameScaleRefY
            }

            int sum = din.readShort();
            locx = new float[sum];
            locy = new float[sum];
            modules = new CatModule[sum];
            moduleScaleX = new float[sum];
            moduleScaleY = new float[sum];
            moduleTransparency = new float[sum];

            for (int i = 0; i < sum; i++)
            {
                int mid = din.readShort();
                modules[i] = datas.modules[mid];

                if (datas.enableFreeRotate)
                {
                    if (ag.smooth)
                    {
                        locx[i] = din.readFloat();
                        locy[i] = din.readFloat();
                        din.readFloat(); // moduleFreeRotate
                        din.readFloat(); // moduleFreeRotateRefX
                        din.readFloat(); // moduleFreeRotateRefY
                        moduleTransparency[i] = din.readFloat();
                        moduleScaleX[i] = din.readFloat();
                        moduleScaleY[i] = din.readFloat();
                        din.readFloat(); // modulePosX
                        din.readFloat(); // modulePosY
                    }
                    else
                    {
                        locx[i] = din.readShort();
                        locy[i] = din.readShort();
                        din.readShort(); // moduleFreeRotate
                        din.readShort(); // moduleFreeRotateRefX
                        din.readShort(); // moduleFreeRotateRefY
                        moduleTransparency[i] = din.readFloat();
                        moduleScaleX[i] = din.readFloat();
                        moduleScaleY[i] = din.readFloat();
                        din.readShort(); // modulePosX
                        din.readShort(); // modulePosY
                    }
                }
                else
                {
                    locx[i] = din.readShort();
                    locy[i] = din.readShort();
                    din.readByte(); // rotate
                    moduleTransparency[i] = 1f;
                    moduleScaleX[i] = 1f;
                    moduleScaleY[i] = 1f;
                }
            }

            // 跳过碰撞区域数据
            int colliSum = din.readByte();
            for (int i = 0; i < colliSum; i++)
            {
                din.readShort(); // type
                din.readShort(); // x
                din.readShort(); // y
                din.readShort(); // width
                din.readShort(); // height
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e.StackTrace + " " + datas.path);
            Debug.LogError(e.Message);
        }
    }

    public void clear()
    {
        modules = null;
        locx = null;
        locy = null;
        moduleScaleX = null;
        moduleScaleY = null;
        moduleTransparency = null;
    }
}
