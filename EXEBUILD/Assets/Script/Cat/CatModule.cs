using System;
using System.Collections.Generic;

using UnityEngine;




/**
 * 模块
 * @author Cat
 * @version 1.0.1
 */
public class CatModule{



	public int type;//默认为0

	public int id;

//	图片所属区域，绘图时也表示宽高,绘制线条时表示第二组属性
	public int x;
	public int y;
	public int width;
	public int height;
	public float halfW;
	public float halfH;
	public int imgIndex;
	
	public byte level;
	
	public TextureRegion textureRegion;
	
	public string path;



	public void read(int id, JavaReader din){
		read(id, din, false);
	}

	public void read(int id, JavaReader din, bool longImageArray){



		this.id=id;//id按位置传进来
		type=din.readByte();

		if(CatSys.ENABLE_AVATAR){
			level=din.readByte();
		}

		x=din.readShort();
		y=din.readShort();
		width=din.readShort();
		height=din.readShort();
		if(longImageArray){
			imgIndex=din.readInt();
		}else{
			imgIndex=din.readByte();
			if(imgIndex<0){
				imgIndex+=256;
			}
		}

		halfW=width/2f;
		halfH=height/2f;
	}

	public void setTextureRegion(TextureRegion textureRegion) {
		this.textureRegion=textureRegion;
		
	}



	public bool visible=true;
	public void setVisible(bool visible) {
		this.visible=visible;
	}


	public TextureRegion image;

	public void clear() {
		image=null;
		textureRegion=null;
		path=null;
    }

    public static Dictionary<String, CatNinePatchDrawable> ninePatches = new Dictionary<String, CatNinePatchDrawable>();

    public CatNinePatchDrawable ninePatch;

    public void setNinePatch(String name, CatNinePatchDrawable ninePatch)
    {
        ninePatches.Add(name, ninePatch);
        this.ninePatch = ninePatch;
    }

    public static CatNinePatchDrawable getNinePatch(String name)
    {
        CatNinePatchDrawable temp = null;
        ninePatches.TryGetValue(name, out temp);
        return temp;
    }
}
