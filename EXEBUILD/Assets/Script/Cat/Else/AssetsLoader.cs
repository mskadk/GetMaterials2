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

    public static AudioClip loadClip(string filename)
    {

        string externalURL = MyStatic.AssetsRoot + filename;

        string url = externalURL;

        if (!MyStatic.useStreamingAssets && File.Exists(externalURL))
        {

        }
        else
        {
            url = Application.streamingAssetsPath + "/" + filename;
        }


        /*if(fileInfo.Length > 1*1024*1024)
        {
            return loadAudioClipWithDownloader(url);
        }
        else*/
        {
            return loadAudioClipInRequest(url);
        }
    }

    public static AudioClip loadAudioClipWithDownloader(string url)
    {
        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            DownloadHandlerAudioClip downloadHandler = new DownloadHandlerAudioClip(url, AudioType.UNKNOWN);
            downloadHandler.streamAudio = true; //该代码并无作用 unity的bug   bug跟踪 https://forum.unity.com/threads/downloadhandleraudioclip-streamaudio-is-ignored.699908/

            www.downloadHandler = downloadHandler;
            www.SendWebRequest();

            while (!www.isDone)
            {

            }

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(www.error);
            }
            else
            {
                AudioClip audioClip = downloadHandler.audioClip;
                return audioClip;
            }
        }
        return null;
    }

    public static AudioClip loadAudioClipInRequest(string url)
    {
        UnityWebRequest request = null;

        //string externalURL = MyStatic.AssetsRoot + filename;

        //if (!MyStatic.useStreamingAssets && File.Exists(externalURL))
        //{
        //    request = UnityWebRequestMultimedia.GetAudioClip(externalURL, AudioType.UNKNOWN);
        //}
        //else
        //{
        //    string url = Application.streamingAssetsPath + "/" + filename;
        //    request = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.UNKNOWN);
        //}
#if UNITY_IOS
        if (!url.StartsWith("file://"))
        {
            url = "file://" + url;
        }
#endif
        request = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.UNKNOWN);

        if (request != null)
        {
            request.timeout = 10 * 1000;
            request.SendWebRequest();
            while (request.result == UnityWebRequest.Result.InProgress)
            {
                //Debug.LogError("error");
            }

            if (request.result == UnityWebRequest.Result.Success)
            {
                AudioClip audioClip = DownloadHandlerAudioClip.GetContent(request);

                return audioClip;
            }
        }


        return null;
    }


    //public static byte[] loadBytesInRequest(string filename)
    //{
    //    UnityWebRequest request = null;


    //    string externalURL = MyStatic.AssetsRoot + filename;

    //    if (!MyStatic.useStreamingAssets && File.Exists(externalURL))
    //    {
    //        request = UnityWebRequest.Get(externalURL);
    //    }
    //    else
    //    {
    //        string url = Application.streamingAssetsPath + "/" + filename;
    //        request = UnityWebRequest.Get(url);
    //    }

    //    if(request != null)
    //    {
    //        request.timeout = 10 * 1000;
    //        request.SendWebRequest();
    //        while (request.result == UnityWebRequest.Result.InProgress)
    //        {

    //        }

    //        if (request.result == UnityWebRequest.Result.Success)
    //        {
    //            byte[] byteArray = request.downloadHandler.data;
    //            return byteArray;
    //        }
    //    }

    //    return null;
    //}

}



