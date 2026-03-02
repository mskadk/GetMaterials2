using System;
using UnityEngine;

/// <summary>
/// 动作 - 包含一组帧的序列
/// </summary>
public class CatAction
{
    public int id;
    public CatFrame[] frames;

    public void read(int id, JavaReader din, CatActionGroup data)
    {
        try
        {
            this.id = id;
            din.readByte(); // type，跳过

            int size = din.readShort();
            frames = new CatFrame[size];

            for (int i = 0; i < size; i++)
            {
                frames[i] = data.frames[din.readShort()];
                din.readShort(); // lastTime，跳过
            }
        }
        catch (Exception e)
        {
            Debug.Log(e.StackTrace);
        }
    }

    public void clear()
    {
        if (frames != null)
        {
            for (int i = 0; i < frames.Length; i++)
                frames[i] = null;
            frames = null;
        }
    }
}
