using System;
using System.Collections.Generic;

using UnityEngine;



/**
 * 帧
 * @author Cat
 * @version 1.0.1
 */
public class CatFrame
{

    public int id;
    public CatModule[] modules;
    public float[] locx;
    public float[] locy;
    public byte[] rotate;

    public int rotateAngle;//自由旋转可用
    public int rotateRefX, rotateRefY;//旋转中心
    public float transparency = 1.0f;//透明度 默认是1
    public float frameScaleX = 1.0f, frameScaleY = 1.0f;//缩放值
    public int frameScaleRefX, frameScaleRefY;//缩放中心

    //模块对应的值
    public float[] moduleFreeRotate;//旋转角度
    public float[] moduleFreeRotateRefX;//旋转中心
    public float[] moduleFreeRotateRefY;
    public float[] moduleTransparency;//透明度
    public float[] moduleScaleX;//缩放比
    public float[] moduleScaleY;
    public float[] modulePosX;//修正位置（打开后使用float存储位置）
    public float[] modulePosY;


    //分为几种类型的碰撞  0=不通过 1=伤害 2=xx之类
    public CatCollisionArea[] collides;

    public int buffOffx, buffOffy;

    public CatFrame()
    {
    }

    public CatActionGroup ag;

    public CatFrame(CatActionGroup ag)
    {
        this.ag = ag;
    }

    private CatActionGroup datas;

    public void read(int id, JavaReader din, CatActionGroup datas)
    {
        this.id = id;
        this.datas = datas;
        try
        {
            if (datas.enableFreeRotate)
            {
                rotateAngle = din.readShort();
                rotateRefX = din.readShort();
                rotateRefY = din.readShort();
                transparency = din.readFloat();//透明度
                frameScaleX = din.readFloat();
                frameScaleY = din.readFloat();//缩放值
                frameScaleRefX = din.readShort();
                frameScaleRefY = din.readShort();//缩放中心
            }

            int sum = din.readShort();
            locx = new float[sum];
            locy = new float[sum];
            rotate = new byte[sum];

            moduleFreeRotate = new float[sum];//旋转角度
            moduleFreeRotateRefX = new float[sum];//旋转中心
            moduleFreeRotateRefY = new float[sum];
            moduleTransparency = new float[sum];//透明度
            moduleScaleX = new float[sum];//缩放比
            moduleScaleY = new float[sum];
            modulePosX = new float[sum];//缩放中心
            modulePosY = new float[sum];

            modules = new CatModule[sum];
            for (int i = 0; i < sum; i++)
            {
                int mid = din.readShort();
                CatModule m = datas.modules[mid];
                if (datas.enableFreeRotate)
                {
                    if (ag.smooth)
                    {
                        locx[i] = din.readFloat();
                        locy[i] = din.readFloat();
                        moduleFreeRotate[i] = din.readFloat();//旋转角度
                        moduleFreeRotateRefX[i] = din.readFloat();//旋转中心
                        moduleFreeRotateRefY[i] = din.readFloat();
                        moduleTransparency[i] = din.readFloat();//透明度
                        moduleScaleX[i] = din.readFloat();//缩放比
                        moduleScaleY[i] = din.readFloat();
                        modulePosX[i] = din.readFloat();//缩放中心
                        modulePosY[i] = din.readFloat();
                        //						locx[i]=modulePosX[i];
                        //						locy[i]=modulePosY[i];
                    }
                    else
                    {
                        locx[i] = din.readShort();
                        locy[i] = din.readShort();
                        moduleFreeRotate[i] = din.readShort();//旋转角度
                        moduleFreeRotateRefX[i] = din.readShort();//旋转中心
                        moduleFreeRotateRefY[i] = din.readShort();
                        moduleTransparency[i] = din.readFloat();//透明度
                        moduleScaleX[i] = din.readFloat();//缩放比
                        moduleScaleY[i] = din.readFloat();
                        modulePosX[i] = din.readShort();//缩放中心
                        modulePosY[i] = din.readShort();
                        //						locx[i]=modulePosX[i];
                        //						locy[i]=modulePosY[i];
                    }
                }
                else
                {
                    locx[i] = din.readShort();
                    locy[i] = din.readShort();
                    rotate[i] = din.readByte();
                    moduleTransparency[i] = 1;//透明度
                    moduleScaleX[i] = 1;//缩放比
                    moduleScaleY[i] = 1;
                }

                modules[i] = m;
            }
            int colliSum = din.readByte();

            collides = new CatCollisionArea[colliSum];
            for (int i = 0; i < colliSum; i++)
            {
                CatCollisionArea colli = new CatCollisionArea();
                colli.type = din.readShort();
                colli.x = din.readShort();
                colli.y = din.readShort();
                colli.width = din.readShort();
                colli.height = din.readShort();

                //转换为unity 坐标
                colli.y = -colli.y - colli.height;

                collides[i] = colli;
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e.StackTrace+" "+ datas.path);
            Debug.LogError(e.Message);
        }
    }

    public void paintFrame(CatPlayerr player, MeshGroup g)
    {
        paintFrame(player, g, 0, 0, false);
    }

    public void paintFrame(CatPlayerr player, MeshGroup g, float drawX, float drawY, bool centerRotate)
    {
        paintFrame(player, g, drawX, drawY, 0, centerRotate, 1, 1);
    }

    public void paintFrame(CatPlayerr player, MeshGroup g, float drawX, float drawY, bool centerRotate, float scale)
    {
        paintFrame(player, g, drawX, drawY, 0, centerRotate, scale, scale);
    }


    public static bool debugRenderCalls = false;

    public void paintFrame(CatPlayerr player, MeshGroup g, float offx, float offy, float playerRotate, bool centerRotate, float scaleX, float scaleY)
    {

        //g.beginDrawing(modules.Length);

        for (int i = 0; i < modules.Length; i++)
        {
            CatModule m = modules[i];
            if (!m.visible || moduleTransparency[i] <= 0)
            {
                //不可见则跳出
                continue;
            }

            int materialIndex = m.textureRegion.materialIndex;
            float frameScaleX = this.frameScaleX * scaleX;
            float frameScaleY = this.frameScaleY * scaleY;

            if (centerRotate)
            {
                //以帧中心为中心联合旋转
                float moduleCenterX = locx[i] + m.halfW;
                float moduleCenterY = locy[i] + m.halfH;
                double len = Math.Sqrt(moduleCenterX * moduleCenterX + moduleCenterY * moduleCenterY);
                float alpha = Mathf.Atan2(moduleCenterY, moduleCenterX);
                float beta = alpha - (playerRotate + rotateAngle) * Mathf.Deg2Rad;
                float newX = (float)(len * Mathf.Cos(beta) * frameScaleX) - m.halfW;
                float newY = (float)(len * Mathf.Sin(beta) * frameScaleY) - m.halfH;

                float rotate = (playerRotate + (rotateAngle + (360 - moduleFreeRotate[i])))%360;

                g.drawModule(player.materials[materialIndex], m.textureRegion, offx + newX, offy - newY - m.height, m.halfW, m.halfH, m.width, m.height, moduleScaleX[i] * frameScaleX, moduleScaleY[i] * frameScaleY, rotate, moduleTransparency[i]);
            }
            else
            {
                float rotate = (playerRotate + (rotateAngle + (360 - moduleFreeRotate[i]))) % 360;

                //以模块中心为中心旋转                
                float dx = offx + locx[i];
                float dy = offy - locy[i] - m.height;
                
                g.drawModule(player.materials[materialIndex], m.textureRegion, dx, dy, m.halfW, m.halfH, m.width, m.height, moduleScaleX[i] * frameScaleX, moduleScaleY[i] * frameScaleY, rotate, moduleTransparency[i]);
            }
        }

        //g.endDrawed();
    }

    public void paintNinePatch(CatPlayerr catplayer,MeshGroup g,  float x, float y, float width, float height)
    {
        paintNinePatchP(catplayer,g, x, y, width, height, 0.33333f);
    }

    //cornerPercent角占比（默认应该是三分之一）
    public void paintNinePatchP(CatPlayerr catplayer, MeshGroup g, float x, float y, float width, float height, float cornerPercent)
    {
        width = (int)width;
        height = (int)height;
        // 默认选项
        int rotateAngle = 0;
        bool centerRotate = false;

        for (int i = 0; i < modules.Length; i++)
        {
            CatModule m = modules[i];
            if (!m.visible || moduleTransparency[i] <= 0)
            {
                //不可见则跳出
                continue;
            }
        
            int materialIndex=   m.textureRegion.materialIndex;
            Material material =    catplayer.materials[materialIndex];

            float scaleX = width / m.width;
            float scaleY = height / m.height;

            float frameScaleX = this.frameScaleX * scaleX;
            float frameScaleY = this.frameScaleY * scaleY;

            if (datas.enableFreeRotate)
            {
                //Android自由旋转模式
                float a = g.getMeshBody().a ;
                if (moduleTransparency[i] != 1)
                {
                    MeshBody mb = g.getMeshBody();
                    g.getMeshBody().setColor(mb.r, mb.g, mb.b, a * moduleTransparency[i]);
                }
                if (centerRotate)
                {
                    //以帧中心为中心联合旋转
                    float moduleCenterX = locx[i] + m.halfW;
                    float moduleCenterY = locy[i] + m.halfH;
                    double len = Math.Sqrt(moduleCenterX * moduleCenterX + moduleCenterY * moduleCenterY) * frameScaleX;

                    //float alpha = MathUtils.atan2(moduleCenterY, moduleCenterX);
                    //float beta = alpha - (rotateAngle) * MathUtils.degreesToRadians;
                    //float newX = (float)(len * MathUtils.cos(beta)) - m.halfW;
                    //float newY = (float)(len * MathUtils.sin(beta)) - m.halfH;

                    
                    float alpha = Mathf.Atan2(moduleCenterY, moduleCenterX);
                    float beta = alpha - (rotateAngle) * Mathf.Deg2Rad;
                    float newX = (float)(len * Mathf.Cos(beta)) - m.halfW;
                    float newY = (float)(len * Mathf.Sin(beta)) - m.halfH;


                    float drawW = m.width * moduleScaleX[i] * frameScaleX;
                    float drawH = m.height * moduleScaleY[i] * frameScaleY;
                    float drawOffx = (m.width - (m.width * moduleScaleX[i] * frameScaleX)) * 0.5f;
                    float drawOffy = (m.height - (m.height * moduleScaleY[i] * frameScaleY)) * 0.5f;
                    int w = (int)(m.width * cornerPercent);
                    int h = (int)(m.height * cornerPercent);

                    if (m.ninePatch == null)
                    {
                        CatNinePatch np = new CatNinePatch(m.textureRegion, w, w, h, h);
                        CatNinePatchDrawable nine = new CatNinePatchDrawable(np);
                        m.ninePatch = nine;
                    }
                    else
                    {
                        try
                        {
                            m.ninePatch.getPatch().resetPatch(w, w, h, h);
                        }
                        catch (Exception)
                        {
                            //							e.printStackTrace();
                            CatNinePatch np = new CatNinePatch(m.textureRegion, w, w, h, h);
                            CatNinePatchDrawable nine = new CatNinePatchDrawable(np);
                            m.ninePatch = nine;
                        }
                    }
                    m.ninePatch.draw(g, material, x + newX + drawOffx, y + newY + drawOffy, drawW, drawH);

                }
                else
                {
                    //以模块中心为中心旋转
                    float drawW = m.width * moduleScaleX[i] * frameScaleX;
                    float drawH = m.height * moduleScaleY[i] * frameScaleY;
                    float drawOffx = (m.width - (m.width * moduleScaleX[i] * frameScaleX)) * 0.5f;
                    float drawOffy = (m.height - (m.height * moduleScaleY[i] * frameScaleY)) * 0.5f;

                    int w = (int)(m.width * cornerPercent);
                    int h = (int)(m.height * cornerPercent);
                    if (m.ninePatch == null)
                    {
                        CatNinePatch np = new CatNinePatch(m.textureRegion, w, w, h, h);
                        CatNinePatchDrawable nine = new CatNinePatchDrawable(np);
                        m.ninePatch = nine;
                    }
                    else
                    {
                        try
                        {
                            m.ninePatch.getPatch().resetPatch(w, w, h, h);
                        }
                        catch (Exception)
                        {
                            //							e.printStackTrace();
                            CatNinePatch np = new CatNinePatch(m.textureRegion, w, w, h, h);
                            CatNinePatchDrawable nine = new CatNinePatchDrawable(np);
                            m.ninePatch = nine;
                        }
                    }
                    m.ninePatch.draw(g, material,x + locx[i] + drawOffx, y + locy[i] + drawOffy, drawW, drawH);

                }
                if (moduleTransparency[i] != 1)
                {
                    g.getMeshBody().setColor(1, 1, 1, a);
                }
            }
            else
            {
                int w = (int)(m.width * cornerPercent);
                int h = (int)(m.height * cornerPercent);
                if (m.ninePatch == null)
                {
                    CatNinePatch np = new CatNinePatch(m.textureRegion, w, w, h, h);
                    CatNinePatchDrawable nine = new CatNinePatchDrawable(np);
                    m.ninePatch = nine;
                }
                else
                {
                    try
                    {
                        m.ninePatch.getPatch().resetPatch(w, w, h, h);
                    }
                    catch (Exception)
                    {
                        //						e.printStackTrace();
                        CatNinePatch np = new CatNinePatch(m.textureRegion, w, w, h, h);
                        CatNinePatchDrawable nine = new CatNinePatchDrawable(np);
                        m.ninePatch = nine;
                    }
                }
                m.ninePatch.draw(g, material, x + locx[i], y + locy[i], m.width, m.height);
            }
        }
    }



    /**
	 * 取得本帧范围
	 * @return 本帧范围
	 */
    private Rect rectangle = Rect.zero;
    public Rect getRectangle()
    {

        if (rectangle != Rect.zero)
        {
            return rectangle;
        }

        int minx = int.MaxValue;
        int miny = int.MaxValue;
        int maxx = int.MinValue;
        int maxy = int.MinValue;

        for (int i = 0; i < modules.Length; i++)
        {
            CatModule m = modules[i];

            float y = -(locy[i] + m.height);

            //byte r = rotate[i];
            minx = (int)(Math.Min(locx[i], minx) * moduleScaleX[i]);
            miny = (int)(Math.Min(y, miny) * moduleScaleY[i]);
            maxx = (int)(Math.Max(locx[i] + m.width, maxx) * moduleScaleX[i]);
            maxy = (int)(Math.Max(y + m.height, maxy) * moduleScaleY[i]);
        }

        rectangle.xMin = Mathf.Min(minx, maxx);
        rectangle.xMax = Mathf.Max(minx, maxx);
        rectangle.yMin = Mathf.Min(miny, maxy);
        rectangle.yMax = Mathf.Max(miny, maxy);

        return rectangle;
    }

  

    /**
	 * 取得碰撞区
	 * @return CollisionArea
	 */
    public CatCollisionArea getCollisionArea(int id)
    {
        if (id > collides.Length -1) return null;
        return collides[id];
    }

    /**
	 * 取得旋转后碰撞区的中心（可以直接*比例而获得缩放后的中心）
	 * @return CollisionArea
	 */
    public Vector2 getCollisionAreaCenter(int id, float angle)
    {
        CatCollisionArea c = collides[id].clone();

        float moduleCenterX = c.centerX();
        float moduleCenterY = c.centerY();
        double len = Math.Sqrt(moduleCenterX * moduleCenterX + moduleCenterY * moduleCenterY);
        double alpha = Mathf.Atan2(moduleCenterY, moduleCenterX);//弧度
        double beta = alpha - (0 + angle) * Mathf.Deg2Rad;//弧度
        float newX = (float)(len * Math.Cos(beta));
        float newY = (float)(len * Math.Sin(beta));

        Vector2 tempCenter = new Vector2();
        tempCenter.Set(newX, newY);
        return tempCenter;
    }

    /**
	 * 取得碰撞区
	 * @return CollisionArea
	 */
    public CatCollisionArea getRandomCollisionArea()
    {
        return collides[RandomTool.getRandom(collides.Length)];
    }
    /**
	 * 取得碰撞区
	 * @return CollisionArea[]
	 */
    public CatCollisionArea[] getCollisionAreas()
    {
        return collides;
    }

    /**
	 * 取得固定范围碰撞区，从start到end（包含start和end）
	 * @return CollisionArea[]
	 */
    public CatCollisionArea[] getCollisionAreas(int start, int end)
    {
        CatCollisionArea[] area = new CatCollisionArea[end - start + 1];
        for (int i = start; i < start + area.Length; i++)
        {
            area[i - start] = collides[i];
        }
        return area;
    }

    /**
	 * 取得固定类型碰撞区
	 * @return CollisionArea[]
	 */
    public CatCollisionArea[] getCollisionAreas(int type)
    {
        int sum = 0;
        for (int i = 0; i < collides.Length; i++)
        {
            if (collides[i].type == type)
            {
                sum++;
            }
        }
        CatCollisionArea[] area = new CatCollisionArea[sum];
        sum = 0;
        for (int i = 0; i < collides.Length; i++)
        {
            if (collides[i].type == type)
            {
                area[sum] = collides[i];
                sum++;
            }
        }
        return area;
    }

    /**
	 * 重定位碰撞区域，注意此方法为新生成{@link CollisionArea}类的对象
	 * @param area
	 * @param offx
	 * @param offy
	 * @return CollisionArea[]
	 */
    public static CatCollisionArea[] reformCollisionAreas(CatCollisionArea[] area,
            int offx, int offy)
    {
        CatCollisionArea[] newArea = new CatCollisionArea[area.Length];
        for (int i = 0; i < area.Length; i++)
        {
            newArea[i] = area[i].clone();
            newArea[i].x += offx;
            newArea[i].y += offy;
        }
        return newArea;
    }

    /**
	 * 重定位碰撞区域，注意此方法为新生成{@link CollisionArea}类的对象
	 * @param area
	 * @param offx
	 * @param offy
	 * @return CollisionArea
	 */
    public static CatCollisionArea reformCollisionArea(CatCollisionArea area,
            int offx, int offy)
    {
        CatCollisionArea newArea = area.clone();
        newArea.x += offx;
        newArea.y += offy;
        return newArea;
    }

    /**
	 * 重定位碰撞区域，注意此方法为新生成{@link CollisionArea}类的对象
	 * @param offx
	 * @param offy
	 * @return CollisionArea[]
	 */
    public CatCollisionArea[] getReformedCollisionAreas(int offx, int offy)
    {
        CatCollisionArea[] area = getCollisionAreas();
        CatCollisionArea[] newArea = new CatCollisionArea[area.Length];
        for (int i = 0; i < area.Length; i++)
        {
            newArea[i] = area[i].clone();
            newArea[i].x += offx;
            newArea[i].y += offy;
        }
        return newArea;
    }

    /**
	 * 重定位碰撞区域，注意此方法为新生成{@link CollisionArea}类的对象
	 */
    public CatCollisionArea[] getReformedCollisionAreas()
    {
        CatCollisionArea[] area = getCollisionAreas();
        CatCollisionArea[] newArea = new CatCollisionArea[area.Length];
        /*
                for (int i = 0; i < area.Length; i++) {
                    newArea[i]=area[i].clone();
                    newArea[i].x+=Global.halfHUDW;
                    newArea[i].y+=Global.halfHUDH;
                }*/
        return newArea;
    }

    /**
	 * 重定位碰撞区域，注意此方法为新生成{@link CollisionArea}类的对象
	 * @param staffContentArea 
	 */
    public CatCollisionArea[] getReformedCollisionAreas(CatCollisionArea staffContentArea)
    {
        CatCollisionArea[] area = getCollisionAreas();
        CatCollisionArea[] newArea = new CatCollisionArea[area.Length];
        for (int i = 0; i < area.Length; i++)
        {
            newArea[i] = area[i].clone();
            newArea[i].x += staffContentArea.centerX();
            newArea[i].y += staffContentArea.centerY();
        }
        return newArea;
    }

    /**
	 * 重定位碰撞区域，注意此方法为新生成{@link CollisionArea}类的对象
	 * @param start
	 * @param end
	 * @param offx
	 * @param offy
	 * @return CollisionArea[]
	 */
    public CatCollisionArea[] getReformedCollisionAreas(int start, int end, int offx, int offy)
    {
        CatCollisionArea[] area = getCollisionAreas(start, end);
        CatCollisionArea[] newArea = new CatCollisionArea[area.Length];
        for (int i = 0; i < area.Length; i++)
        {
            newArea[i] = area[i].clone();
            newArea[i].x += offx;
            newArea[i].y += offy;
        }
        return newArea;
    }

    /**
	 * 重定位碰撞区域，注意此方法为新生成{@link CollisionArea}类的对象
	 * @param id
	 * @param offx
	 * @param offy
	 * @return CollisionArea
	 */
    public CatCollisionArea getReformedCollisionArea(int id, int offx, int offy)
    {
        CatCollisionArea area = getCollisionArea(id);
        CatCollisionArea newArea = area.clone();
        newArea.x += offx;
        newArea.y += offy;
        return newArea;
    }

    public void clear()
    {
        modules = null;

        locx = null;
        locy = null;
        rotate = null;
        moduleFreeRotate = null;
        moduleFreeRotateRefX = null;
        moduleFreeRotateRefY = null;
        moduleTransparency = null;
        moduleScaleX = null;
        moduleScaleY = null;
        modulePosX = null;
        modulePosY = null;
        for (int i = 0; i < collides.Length; i++)
        {
            collides[i] = null;
        }
        collides = null;

    }
    //TODO 尚未实现
    public static CatFrame create(CatModule m, Vector2 pt)
    {
        CatFrame frame = new CatFrame();
        return frame;
    }

}
