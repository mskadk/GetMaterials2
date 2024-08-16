using System;
using System.Collections.Generic;

using UnityEngine;


/**
 * 动作
 * @author Cat
 * @version 1.0.1
 */
public class CatAction {
	
	public int id;//id
	
	public CatFrame[] frames;//本动作包含的帧

	public short[] lastTime;//每个帧持续的时间

	public void read(int id, JavaReader din, CatActionGroup data){
		try {
			this.id=id;
			int type=din.readByte();//TODO 暂时不给与拼合动作的支持
			int size=din.readShort();
			frames=new CatFrame[size];
			lastTime=new short[size];
			for (int i = 0; i < size; i++) {
				frames[i]=data.frames[din.readShort()];
				lastTime[i]=din.readShort();
			}
		} catch (Exception e) {
            Debug.Log(e.StackTrace);
		}
	}



	public CatFrame getFrame(int frameIndex) {
		if(frameIndex > -1 && frameIndex < frames.Length)
        {
			return frames[frameIndex];
		}
		Debug.Log("ArrayIndexOutOfBound："+frameIndex+"/"+frames.Length );
		return null;
	}

	public CatFrame getLastFrame() {
		return frames[frames.Length - 1];
	}

	public CatFrame getFrameId(int i) {
		return frames[i];
	}

	public void clear() {
		for (int i = 0; i < frames.Length; i++) {
			frames[i]=null;
		}
		frames=null;
		lastTime=null;
	}
}
