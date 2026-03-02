using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;


using UnityEngine;



public class TextureManager
{
    
    private static Dictionary<string, TextureNode> texDictionary = new Dictionary<string, TextureNode>();


    //----------------------------图片类-----------------------

    public static Texture2D loadImagePng(string path, bool mipmap = false, bool linear = false)
    {
        path = path.EndsWith(".png") ? path : path + ".png";

        if (texDictionary.ContainsKey(path))
        {
            TextureNode node = texDictionary[path];
            node.addRef();

            //加载成mipmap
            if (!node.mipmap && mipmap)
            {
                Debug.LogError("出现后开启mipmap的情况 请将该贴图的前置加载mipmap设置为true");
            }

            return node.texture;
        }

        JavaReader jr = AssetsLoader.getAsset(path);

        if (jr != null)
        {
            //Debug.LogWarning("加载png图片=======================" + path);  
            Texture2D tex = TextureUtils.loadImage(jr.toByteArrayBR(), TextureFormat.RGBA32, mipmap, linear);
            tex.name = path;
     
            TextureNode node = new TextureNode(mipmap);
            node.texture = tex;
            node.addRef();

            texDictionary.Add(path, node);
            return tex;
        }

        Debug.LogWarning("没有找到该文件==="+path);
        return null;
    }


    public static Texture2D loadImageJpg(string path, bool mipmap = false, bool linear = false)
    {
        path = path.EndsWith(".jpg") ? path : path + ".jpg";
        if (texDictionary.ContainsKey(path))
        {
            TextureNode node = texDictionary[path];
            node.addRef();

            //加载成mipmap
            if (!node.mipmap && mipmap)
            {
                Debug.LogError("出现后开启mipmap的情况");
            }

            return node.texture;
        }

        JavaReader jr = AssetsLoader.getAsset( path);
        //JavaReader jr = PlatformTools.getStreamingAssetsData(path);
        if (jr != null)
        {
            Debug.LogWarning("加载jpg图片=======================" + path);
            Texture2D tex = TextureUtils.loadImageJPG(jr.toByteArrayBR(), TextureFormat.RGB24, mipmap,linear);
            tex.name = path;

            TextureNode node = new TextureNode(mipmap);
            node.texture = tex;
            node.addRef();

            texDictionary.Add(path, node);
            return tex;
        }

        Debug.LogWarning("没有找到该文件==="+ path);
        return null;
    }

    //默认是png
    public static Texture2D loadImage(string path, bool mipmap = false, bool linear = false)
    {
        bool isjpg = path.EndsWith(".jpg");
        if (isjpg)
        {
            return loadImageJpg(path, mipmap, linear);
        }
        else
        {
            return loadImagePng(path, mipmap, linear);
          
        }
    }

    public static void releaseTexture(Texture tex)
    {
        if (tex == null) return;

        foreach (KeyValuePair<string, TextureNode> kvp in texDictionary)
        {
            if (kvp.Value.Equals(tex))
            {
                bool isDestroy = kvp.Value.releaseRef();
                if (isDestroy)
                {
                    Debug.LogWarning("释放图片=======================" + kvp.Key);
                    texDictionary.Remove(kvp.Key);
                    MonoBehaviour.Destroy(tex);
                }
                return;
            }
        }
    }


    public static void releaseTexture(string key)
    {
        if (key == null || key.Length < 1) return;
        if (texDictionary.ContainsKey(key))
        {
            Debug.LogWarning("释放图片=======================" + key);
            TextureNode node = texDictionary[key];

            bool isDestroy = node.releaseRef();
            if(isDestroy)
            {
                texDictionary.Remove(key);
                MonoBehaviour.Destroy(node.texture);
            }
        }
    }




}

