using System;

/// <summary>
/// 模块 - 记录图集中一块区域的位置和尺寸
/// </summary>
public class CatModule
{
    public int id;
    public int type;

    // 图片所属区域
    public int x;
    public int y;
    public int width;
    public int height;
    public float halfW;
    public float halfH;
    public int imgIndex;

    public void read(int id, JavaReader din, bool longImageArray)
    {
        this.id = id;
        type = din.readByte();

        if (CatSys.ENABLE_AVATAR)
        {
            din.readByte(); // level，跳过
        }

        x = din.readShort();
        y = din.readShort();
        width = din.readShort();
        height = din.readShort();

        if (longImageArray)
        {
            imgIndex = din.readInt();
        }
        else
        {
            imgIndex = din.readByte();
            if (imgIndex < 0)
            {
                imgIndex += 256;
            }
        }

        halfW = width / 2f;
        halfH = height / 2f;
    }
}
