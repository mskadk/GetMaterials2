using System;
using System.Collections.Generic;

using UnityEngine;


/**
 * 碰撞区域
 * @author Cat
 * @version 1.0.1
 */
public class CatCollisionArea{
	
	public int type;
	public const int COLLISION=0;//碰撞
	public const int HURT=1;//伤害
	public const int HURT_VERTIGO=2;//眩晕
	public const int HURT_FLY_UP=3;//飞起
	public const int HURT_FLY_AWAY=4;//飞出

    public float x;
    public float y;
    public float width;
    public float height;


	
	public CatCollisionArea() {
	}
	public CatCollisionArea(float x, float y, float width, float height) {
		this.x=x;
		this.y=y;
		this.width=width;
		this.height=height;
	}


	public string toString() {
		return "["+x+","+y+","+width+","+height+"]";
	}

	
	public CatCollisionArea clone() {
        CatCollisionArea c =new CatCollisionArea();
		c.x=x;
		c.y=y;
		c.width=width;
		c.height=height;
		c.type=type;
		return c;
	}

	public CatCollisionArea reform(int offx, int offy){
        CatCollisionArea newArea =clone();
		newArea.x+=offx;
		newArea.y+=offy;
		return newArea;
	}
	public CatCollisionArea bigger(int width, int height) {
        CatCollisionArea c =new CatCollisionArea();
		c.x=x-width/2;
		c.y=y-height/2;
		c.width=this.width+width;
		c.height=this.height+height;
		c.type=type;
		return c;
	}

    public float centerX()
    {
       return (float)(x + width / 2);
    }

    public float centerY()
    {
        return (float)(y + height / 2);
    }
}
