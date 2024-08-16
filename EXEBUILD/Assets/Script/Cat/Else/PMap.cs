using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;


public class PMap
{

    public string[] imgName;


    public int tileXSum, tileYSum;//

    public int tileLayer;//ceng

    public int viewWidth, viewHeight;//


    //图块大小，宽高一致
    public static int tileWH = 16;
    
	
	//地图数组
	public MapTile[][][] mapArray;



    public void loadBaseMap(JavaReader din)
    {
        imgName = new String[din.readByte()];
        for (int i = 0; i < imgName.Length; i++)
        {
            imgName[i] = din.readUTF();
        }
        string mapPlace = din.readUTF();

        int tileW = din.readInt();
        int tileH = din.readInt();
        PMap.tileWH = tileW;
        PMap.tileWH = tileH;


        this.tileXSum = din.readInt();
        this.tileYSum = din.readInt();
        this.tileLayer = din.readByte();//层

        this.mapArray = new MapTile[tileLayer][][];
        //初始化地图序列

        for (int layer = 0; layer < tileLayer; layer++)
        {
            mapArray[layer] = new MapTile[tileXSum][];

            for (int i = 0; i < tileXSum; i++)
            {
                //具体行列
                mapArray[layer][i] = new MapTile[tileYSum];
                for (int j = 0; j < tileYSum; j++)
                {
                    int no = din.readInt();//负数表示animated
                    if (no < 0)
                    {
                        //TODO uncompleted 暂时不支持负数表示动态地块的状态
                    }
                    else
                    {
                        mapArray[layer][i][j] = new MapTile(no);
                    }
                }
            }
        }
    }


    public static PMap loadMap(string mapFile)
    {
        JavaReader dis = AssetsLoader.getAsset(mapFile);
        if (dis == null)
        {
            Debug.LogError("找不到文件 " + mapFile);
            return null;
        }

        PMap map = new PMap();
        map.loadBaseMap(dis);
        return map;
    }

}