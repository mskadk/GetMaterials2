using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using Assets.Script.My;

public class AssetsLoader
{
    public static JavaReader getAsset(string filePath)
    {
        return getFileDataInputStream(MyStatic.workSpacePath + filePath);
    }

    public static JavaReader getFileDataInputStream(string path)
    {
        if (!File.Exists(path))
        {
            Debug.Log("getFileDataInputStream: file not exist:" + path);
            return null;
        }

        byte[] data = File.ReadAllBytes(path);
        JavaReader jr = new JavaReader();
        jr.setBinaryReader(new BinaryReader(new MemoryStream(data)));
        return jr;
    }

    public static JavaReader getStreamingAssetsData(string filePath)
    {
        string url = Path.Combine(Application.streamingAssetsPath, filePath);
        UnityWebRequest request = UnityWebRequest.Get(url);
        request.timeout = 10 * 1000;
        request.SendWebRequest();
        while (request.result == UnityWebRequest.Result.InProgress) { }

        byte[] byteArray = null;
        if (request != null && request.result == UnityWebRequest.Result.Success)
        {
            byteArray = request.downloadHandler.data;
        }

        if (byteArray == null || byteArray.Length < 1)
        {
            Debug.LogWarning("未读取到文件===============" + url);
            return null;
        }

        JavaReader jr = new JavaReader();
        jr.setBinaryReader(new BinaryReader(new MemoryStream(byteArray)));
        return jr;
    }
}
