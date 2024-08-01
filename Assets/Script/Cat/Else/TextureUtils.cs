using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class TextureUtils
{


    /// <summary>
    /// 加载图片
    /// </summary>
    public static Texture2D loadImage(byte[] datas, TextureFormat format, bool mipmap, bool _linear)
    {

        Texture2D tex = new Texture2D(1, 1, format, mipmap, _linear);
        tex.LoadImage(datas);

        //apply之后  才有mipmap
        tex.Apply(true);

        return tex;
    }


    /// <summary>
    /// 加载图片
    /// </summary>
    public static Texture2D loadImageJPG(byte[] datas, TextureFormat format, bool mipmap, bool _linear)
    {
        Texture2D tex = new Texture2D(1, 1, format, mipmap, _linear);
        tex.LoadImage(datas);

        //apply之后  才有mipmap
        tex.Apply(true);
        return tex;
    }


    public static Texture2DArray CreateTexture2DArray(Texture2D[] texList, bool minmip, bool linear)
    {

        Texture2DArray tex2DArr = null;
        if (texList.Length > 0)
        {
            Texture2D tex = texList[0];
            tex2DArr = new Texture2DArray(tex.width, tex.height, texList.Length, tex.format, minmip, linear);

            FairyRunnable.postRunnableIm(() => {

                for (int i = 0; i < texList.Length; i++)
                {
                    Texture2D txt = texList[i];
                    //Debug.LogError("tex==" + i + "  "+texList[i]);
                    Graphics.CopyTexture(txt, 0, 0, tex2DArr, i, 0);
                }
                tex2DArr.Apply();

            });

           /* for (int i = 0; i < texList.Length; i++)
            {
                Texture2D txt = texList[i];
                //Debug.LogError("tex==" + i + "  "+texList[i]);
                Graphics.CopyTexture(txt, 0, 0, tex2DArr, i, 0);
            }
            tex2DArr.Apply();*/
        }
        return tex2DArr;
    }
/*

    public static int CreateTexture2DArray(Texture2DArray tex2D, Texture2D tex, bool minmip, bool linear)
    {
        tex2D.updateCount
        int index = 
        Graphics.CopyTexture(txt, 0, 0, tex2D, i, 0);
        tex2D.Apply();
    }*/

    public static void setPixelMode(Texture tex)
    {
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
    }


    public static void setPixelMode(Material[] mats, FilterMode filterMode)
    {
        for(int i = 0; i< mats.Length; i++)
        {
            mats[i].mainTexture.filterMode = filterMode;
        }
    }


    public static void setPixelMode(Texture2DArray tex)
    {
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
    }


    public static void renderTexCopyTex2d(Texture2D tex, RenderTexture rt)
    {
        if (tex == null || rt == null)
            return;
        RenderTexture.active = rt;
        tex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
        tex.Apply();
    }


  /*  public static void GammaToLinear(Texture2D tex)
    {
        RenderTexture rt = new RenderTexture(tex.width, tex.height, 0, RenderTextureFormat.ARGB32);
        rt.enableRandomWrite = true;
        rt.Create();

        ComputeShader m_GrayComputeShader = Resources.Load<ComputeShader>("Shader/ZFairy_Game/GammaCorrectiom");
        int kernal = m_GrayComputeShader.FindKernel("CSMain");
        m_GrayComputeShader.SetTexture(kernal, "inputTex", tex);
        m_GrayComputeShader.SetTexture(kernal, "outputTex", rt);
        //m_GrayComputeShader.SetFloats("gammaLut", gammaLut);

        m_GrayComputeShader.Dispatch(kernal, tex.width / 8, tex.height / 8, 1);


        FairyRunnable.postRunnableIm(() => {
            renderTexCopyTex2d(tex, rt);
            //PlatformTools.SaveTextureToPNG(tex, "D://22.png");
        });
    }*/

}