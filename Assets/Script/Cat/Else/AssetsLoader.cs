using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using UnityEngine.Networking;


using UnityEngine;
using Assets.Script.My;


public class AssetsLoader
{
    public static JavaReader getAsset(string filePath)
    {
        JavaReader jr = null;

        jr = getFileDataInputStream(MyStatic.workSpacePath + filePath);

        return jr;
    }

    public static JavaReader getStreamingAssetsData(string filePath)
    {
        string url = Path.Combine(Application.streamingAssetsPath, filePath); //Application.streamingAssetsPath + "/" + filePath;
        UnityWebRequest request = UnityWebRequest.Get(url);
        request.timeout = 10 * 1000;
        request.SendWebRequest();
        while (request.result == UnityWebRequest.Result.InProgress)
        {
        }
        byte[] byteArray = null;
        if (request != null && request.result == UnityWebRequest.Result.Success)
        {
            byteArray = request.downloadHandler.data;
        }

        if (byteArray == null || (byteArray != null && byteArray.Length < 1))
        {
            Debug.LogWarning("未读取到文件===============" + url);
            return null;
        }

        JavaReader jr = new JavaReader();
        jr.setBinaryReader(new BinaryReader(new MemoryStream(byteArray)));
        return jr;
    }


    public static JavaReader getFileDataInputStream(string path)
    {
        //FileHandle file = new FileHandle(path);
        FileHandle file = new FileHandle(path);
        if (file.exist() == false)
        {
            Debug.Log("getFileDataInputStream: file not exist:" + path);
            return null;
        }

        MemoryStream ms = new MemoryStream(file.getData());

        JavaReader jr = new JavaReader();
        jr.setBinaryReader(new BinaryReader(ms));
        return jr;
    }

}



