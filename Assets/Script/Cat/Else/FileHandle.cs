using UnityEngine;
using System.Collections;
using System.IO;
using System;
using System.Collections.Generic;

public class FileHandle {


    public FileInfo file;
    

    public FileHandle(string path)
    {
        file = new FileInfo(path);
    }

    public bool exist()
    {
        return file.Exists;
    }

    public void create()
    {
        if (file.Exists) return;
        if (!Directory.Exists(file.DirectoryName))
        {
            Directory.CreateDirectory(file.DirectoryName);
        }
        //file.Create();
    }

    public void delect()
    {
        if (file.Exists == false) return;
        file.Delete();
    }
    
    public void write(byte[] data)
    {
        try
        {
            FileStream fs = file.OpenWrite();
            fs.Write(data, 0, data.Length);
            fs.Flush();
            fs.Close();
        }
        catch (Exception e)
        {
            Debug.LogError(file.FullName+"\n"+e.Message+"\n"+ e.StackTrace);
        }
    }

    public byte[] getData()
    {
        byte[] data = null;
        try
        {
            FileStream fs = file.OpenRead();
            data = new byte[fs.Length];
            fs.Read(data, 0, data.Length);
            fs.Close();
        }
        catch(Exception)
        {
            Debug.LogError("读取失败="+file.FullName);
            //Debug.LogError(e.StackTrace);
        }

        if(data.Length < 1)
        {
            Debug.LogError("没有获取到数据");
        }

        return data;
    }



    public void destroy()
    {

        //file.
    }





    /// <summary>
    /// 拷贝文件夹
    /// </summary>
    /// <param name="srcPath">需要被拷贝的文件夹路径</param>
    /// <param name="tarPath">拷贝目标路径</param>
    private void CopyFolder(string srcPath, string tarPath)
    {
        if (!Directory.Exists(srcPath))
        {
            Debug.Log("CopyFolder is finish.");
            return;
        }

        if (!Directory.Exists(tarPath))
        {
            Directory.CreateDirectory(tarPath);
        }

        //获得源文件下所有文件
        List<string> files = new List<string>(Directory.GetFiles(srcPath));
        files.ForEach(f =>
        {
            string destFile = Path.Combine(tarPath, Path.GetFileName(f));
            File.Copy(f, destFile, true); //覆盖模式
        });

        //获得源文件下所有目录文件
        List<string> folders = new List<string>(Directory.GetDirectories(srcPath));
        folders.ForEach(f =>
        {
            string destDir = Path.Combine(tarPath, Path.GetFileName(f));
            CopyFolder(f, destDir); //递归实现子文件夹拷贝
        });
    }

}
