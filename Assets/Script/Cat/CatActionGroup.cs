using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Assets.Script.My;

/// <summary>
/// 动作组 - 负责从 .bin 文件加载所有数据（图集、模块、帧、动作）
/// </summary>
public class CatActionGroup
{
    public bool smooth;
    public bool enableFreeRotate;
    public bool longImageArray;
    public string path;

    public CatModule[] modules;
    public CatFrame[] frames;
    public CatAction[] actions;

    public string[] imgPath;
    public Texture2D[] imgSource;

    // ===== 静态缓存 =====
    private static List<string> loadedPaths = new List<string>();
    private static List<CatActionGroup> loadedGroups = new List<CatActionGroup>();
    private static List<int> loadedRefCounts = new List<int>();

    /// <summary>
    /// 加载或从缓存获取 CatActionGroup
    /// </summary>
    public static CatActionGroup Load(string spriteName)
    {
        string path = spriteName.EndsWith(".bin") ? spriteName : spriteName + ".bin";

        int index = loadedPaths.IndexOf(path);
        if (index != -1)
        {
            loadedRefCounts[index]++;
            return loadedGroups[index];
        }

        CatActionGroup group = new CatActionGroup(path);
        if (group.actions == null)
        {
            Debug.LogError($"CatActionGroup 加载失败: {path}");
            return null;
        }

        loadedPaths.Add(path);
        loadedGroups.Add(group);
        loadedRefCounts.Add(1);
        return group;
    }

    /// <summary>
    /// 释放引用
    /// </summary>
    public static void Release(string spriteName)
    {
        string path = spriteName.EndsWith(".bin") ? spriteName : spriteName + ".bin";
        int index = loadedPaths.IndexOf(path);
        if (index == -1) return;

        loadedRefCounts[index]--;
        if (loadedRefCounts[index] <= 0)
        {
            loadedGroups[index].Clear();
            loadedPaths.RemoveAt(index);
            loadedGroups.RemoveAt(index);
            loadedRefCounts.RemoveAt(index);
        }
    }

    /// <summary>
    /// 清空所有缓存
    /// </summary>
    public static void ClearAll()
    {
        for (int i = 0; i < loadedGroups.Count; i++)
            loadedGroups[i].Clear();
        loadedPaths.Clear();
        loadedGroups.Clear();
        loadedRefCounts.Clear();
    }

    // ===== 构造函数：读取 .bin 文件 =====
    private bool isSprite;

    private CatActionGroup(string path)
    {
        this.path = path;
        this.enableFreeRotate = true;
        this.smooth = false;
        this.longImageArray = true;

        JavaReader din = AssetsLoader.getAsset(MyStatic.spriteRoot + path);
        isSprite = true;

        if (din == null)
        {
            isSprite = false;
            din = AssetsLoader.getAsset(MyStatic.spriteHRoot + path);
        }

        if (din == null)
        {
            Debug.LogError("未加载到 player 文件: " + path);
            return;
        }

        ReadData(din);
        din.close();
    }

    private void ReadData(JavaReader din)
    {
        // ===== 读取图片路径 =====
        int size = din.readInt();
        if (size > 1000 || size < 0)
        {
            din.br.BaseStream.Position -= 4;
            size = din.readByte();
            if (size < 0) size += 256;
            longImageArray = false;
        }

        imgPath = new string[size];
        imgSource = new Texture2D[size];

        for (int i = 0; i < size; i++)
        {
            imgPath[i] = din.readUTF() + CatSys.imageSuffix;

            if (imgPath[i].EndsWith(".png"))
                imgPath[i] = imgPath[i].Substring(0, imgPath[i].Length - 4);

            string texPath = (isSprite ? "sprite/" : "spriteH/") + imgPath[i];
            imgSource[i] = TextureManager.loadImagePng(texPath, false, false);

            if (imgSource[i] != null)
            {
                imgSource[i].wrapMode = TextureWrapMode.Clamp;
                imgSource[i].filterMode = FilterMode.Point;

                imgSource[i] = RemoveBlackBackground(imgSource[i], 0.1f);
            }
        }

        // ===== 读取模块 =====
        int sum = din.readShort();
        modules = new CatModule[sum];
        for (int i = 0; i < sum; i++)
        {
            modules[i] = new CatModule();
            modules[i].read(i, din, longImageArray);
        }

        // ===== 读取帧 =====
        sum = din.readShort();
        frames = new CatFrame[sum];
        for (int i = 0; i < sum; i++)
        {
            frames[i] = new CatFrame(this);
            frames[i].read(i, din, this);
        }

        // ===== 读取动作 =====
        sum = din.readShort();
        actions = new CatAction[sum];
        for (int i = 0; i < sum; i++)
        {
            actions[i] = new CatAction();
            actions[i].read(i, din, this);
        }
    }

    // ===== 清理 =====
    public void Clear()
    {
        if (imgSource != null)
        {
            for (int i = 0; i < imgSource.Length; i++)
                imgSource[i] = null;
            imgSource = null;
        }

        if (modules != null)
        {
            for (int i = 0; i < modules.Length; i++)
                modules[i] = null;
            modules = null;
        }

        if (frames != null)
        {
            for (int i = 0; i < frames.Length; i++)
            {
                frames[i]?.clear();
                frames[i] = null;
            }
            frames = null;
        }

        if (actions != null)
        {
            for (int i = 0; i < actions.Length; i++)
            {
                actions[i]?.clear();
                actions[i] = null;
            }
            actions = null;
        }

        imgPath = null;
        path = null;
    }

    private static Texture2D RemoveBlackBackground(Texture2D source, float threshold = 0.1f)
    {
        Texture2D newTex = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
        Color[] pixels = source.GetPixels();

        for (int i = 0; i < pixels.Length; i++)
        {
            Color p = pixels[i];
            if (p.r <= threshold && p.g <= threshold && p.b <= threshold)
            {
                pixels[i] = Color.clear;
            }
        }

        newTex.SetPixels(pixels);
        newTex.Apply();
        newTex.wrapMode = source.wrapMode;
        newTex.filterMode = source.filterMode;
        return newTex;
    }

}
