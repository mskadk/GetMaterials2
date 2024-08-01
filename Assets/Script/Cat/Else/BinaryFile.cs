using UnityEngine;
using System.Collections;

using System;
using System.IO;

public class BinaryFile {

    
    private FileStream fs = null;
    private BinaryWriter bw = null;
    private BinaryReader br = null;

    public BinaryWriter writeBinaryFile(string path)
    {
        try
        {
            fs = new FileStream(path, FileMode.Create, FileAccess.Write);
            bw = new BinaryWriter(fs);
            return bw;
        }
        catch (Exception e)
        {
            Debug.Log("写入文件失败  原因为：" + e.ToString());
            return null;
        }
    }


    public BinaryReader readBinaryFile(string path)
    {
        try
        {
            fs = new FileStream(path, FileMode.Open, FileAccess.Read);
            br = new BinaryReader(fs);
            return br;
        }
        catch (Exception e)
        {
            Debug.Log("读取文件失败  原因为："+path+"\n"+e.ToString());
            return null;
        }
    }

    internal BinaryReader readBinaryFile(object p)
    {
        throw new NotImplementedException();
    }

    public void closeIoLink()
    {
        if (bw != null)
        {
            bw.Close();
        }

        if (br != null)
        {
            br.Close();
        }
    }


    //保存数组到文件中
    public static void saveByte2File(string databasePath, string databaseName, byte[] data)
    {
        // 存储本地头像
        string path = databasePath + databaseName;
        if (!File.Exists(databasePath))
        {
            Directory.CreateDirectory(databasePath);
        }
        JavaReader outdos = new JavaReader();
        outdos.setBinaryWriter(new BinaryFile().writeBinaryFile(path));
        outdos.write(data);
        outdos.flush();
        outdos.close();
    }


}
