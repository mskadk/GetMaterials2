
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

/// <summary>
/// 网格数据结构
/// </summary>
public class MeshBody
{



    private int drawIndex = 0;//绘制图片的个数

    private int bodyIndex;//绘制了第index个顶点
    private int bodyCount;//body体的个数

    public List<int> triangles = new List<int>();
    public List<Vector3> vertices = new List<Vector3>();
    public List<Vector3> normals = new List<Vector3>();
    public List<Vector2> uvs = new List<Vector2>();
    public List<Color> colors = new List<Color>();
    
    public int Ids = 0;

    public MeshBody()
    {
        //resetDrawBody();
    }


    public int getDrawCount()
    {
        return drawIndex;
    }

    #region 长度的自动维护
    private bool isDrawed = false;
    
    public void resetDrawBody()
    {
        Ids = 0;
        isDrawed = false;
        drawIndex = 0;
        bodyIndex = 0;
        bodyCount = 1;

        triangles.Clear();
        vertices.Clear();
        normals.Clear();
        uvs.Clear();
        colors.Clear();

    }

    public void addDrawBody()
    {
        if (!isDrawed)
        {
            resetDrawBody();
            isDrawed = true;
            drawIndex++;
            return;
        }else
        {
            drawIndex++;
            bodyIndex++;
        }
    }
    
    //设置信息
    public void endDrawBody()
    {
        isDrawed = false;
    }

    #endregion



    #region 顶端 法线  颜色
/*
    //顶点ver
    public void addVertices(float x, float y, float width, float height, float rotation)
    {
        int baseIndex = bodyIndex * 4;

        float originX = width / 2;
        float originY = height / 2;

        float worldOriginX = x + originX;
        float worldOriginY = y + originY;

        float p1x = -originX;
        float p1y = -originY;
        float p2x = -originX;
        float p2y = originY;
        float p3x = originX;
        float p3y = originY;
        float p4x = originX;
        float p4y = -originY;

        float x1;
        float y1;
        float x2;
        float y2;
        float x3;
        float y3;
        float x4;
        float y4;

        if (rotation != 0)
        {
            float radians = rotation * Mathf.Deg2Rad;
            float cos = (float)Mathf.Cos(radians);
            float sin = (float)Mathf.Sin(radians);

            x1 = cos * p1x - sin * p1y;
            y1 = sin * p1x + cos * p1y;

            x2 = cos * p2x - sin * p2y;
            y2 = sin * p2x + cos * p2y;

            x3 = cos * p3x - sin * p3y;
            y3 = sin * p3x + cos * p3y;

            x4 = x1 + (x3 - x2);
            y4 = y3 - (y2 - y1);
        }
        else
        {
            x1 = p1x;
            y1 = p1y;

            x2 = p2x;
            y2 = p2y;

            x3 = p3x;
            y3 = p3y;

            x4 = p4x;
            y4 = p4y;
        }

        x1 += worldOriginX;
        y1 += worldOriginY;
        x2 += worldOriginX;
        y2 += worldOriginY;
        x3 += worldOriginX;
        y3 += worldOriginY;
        x4 += worldOriginX;
        y4 += worldOriginY;


        vertices[baseIndex] = new Vector3(x1, y1, 0);
        vertices[baseIndex + 1] = new Vector3(x2, y2, 0);
        vertices[baseIndex + 2] = new Vector3(x3, y3, 0);
        vertices[baseIndex + 3] = new Vector3(x4, y4, 0);

    }
    */

    /// <summary>
    /// 
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="originX">中心</param>
    /// <param name="originY"></param>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <param name="scaleX"></param>
    /// <param name="scaleY"></param>
    /// <param name="rotation"></param>
    public void addVertices(float x, float y, float originX, float originY, float width, float height,float scaleX, float scaleY, float rotation)
    {
        int baseIndex = bodyIndex * 4;

        // bottom left and top right corner points relative to origin
        float worldOriginX = x + originX;
        float worldOriginY = y + originY;
        float fx = -originX;
        float fy = -originY;
        float fx2 = width - originX;
        float fy2 = height - originY;

        // scale
        if (scaleX != 1 || scaleY != 1)
        {
            fx *= scaleX;
            fy *= scaleY;
            fx2 *= scaleX;
            fy2 *= scaleY;
        }

        // construct corner points, start from top left and go counter clockwise
        float p1x = fx;
        float p1y = fy;
        float p2x = fx;
        float p2y = fy2;
        float p3x = fx2;
        float p3y = fy2;
        float p4x = fx2;
        float p4y = fy;

        float x1;
        float y1;
        float x2;
        float y2;
        float x3;
        float y3;
        float x4;
        float y4;

        // rotate
        if (rotation != 0)
        {
            float radians = rotation * Mathf.Deg2Rad;
            float cos = (float)Mathf.Cos(radians);
            float sin = (float)Mathf.Sin(radians);

            x1 = cos * p1x - sin * p1y;
            y1 = sin * p1x + cos * p1y;

            x2 = cos * p2x - sin * p2y;
            y2 = sin * p2x + cos * p2y;

            x3 = cos * p3x - sin * p3y;
            y3 = sin * p3x + cos * p3y;

            x4 = x1 + (x3 - x2);
            y4 = y3 - (y2 - y1);
        }
        else
        {
            x1 = p1x;
            y1 = p1y;

            x2 = p2x;
            y2 = p2y;

            x3 = p3x;
            y3 = p3y;

            x4 = p4x;
            y4 = p4y;
        }

        x1 += worldOriginX;
        y1 += worldOriginY;
        x2 += worldOriginX;
        y2 += worldOriginY;
        x3 += worldOriginX;
        y3 += worldOriginY;
        x4 += worldOriginX;
        y4 += worldOriginY;
        
        //vertices[baseIndex + 1] = new Vector3(x1, y1, 0);
        //vertices[baseIndex + 2] = new Vector3(x2, y2, 0);
        //vertices[baseIndex + 3] = new Vector3(x3, y3, 0);
        //vertices[baseIndex + 0] = new Vector3(x4, y4, 0);


        vertices.Add(new Vector3(x1, y1, 0));
        vertices.Add(new Vector3(x2, y2, 0));
        vertices.Add(new Vector3(x3, y3, 0));
        vertices.Add(new Vector3(x4, y4, 0));
    }



    //UV的计算
    public void addUvs(float u, float v, float u2, float v2)
    {
        int baseIndex = bodyIndex * 4;

        uvs.Add(new Vector2(u, 1 - v2));
        uvs.Add(new Vector2(u, 1 - v));
        uvs.Add(new Vector2(u2, 1 - v));
        uvs.Add(new Vector2(u2, 1-v2));
    }


    //计算法线
    public void addNormals()
    {
        //int baseIndex = bodyIndex * 4;
        normals.Add(-Vector3.forward);
        normals.Add(-Vector3.forward);
        normals.Add(-Vector3.forward);
        normals.Add(-Vector3.forward);
    }


    //顶点三角形
    public void addTriangles()
    {
        int basePoint = bodyIndex * 4;

        triangles.Add(basePoint);
        triangles.Add(basePoint + 1);
        triangles.Add(basePoint + 2);
        triangles.Add(basePoint);
        triangles.Add(basePoint + 2);
        triangles.Add(basePoint + 3);
        Ids += 6;
 
    }



    public void addColor32(Color32[] cols)
    {
        int baseIndex = bodyIndex * 4;

        colors.Add(cols[0]);
        colors.Add(cols[1]);
        colors.Add(cols[2]);
        colors.Add(cols[3]);
    }

    public void addColor32(float alpha)
    {
        int baseIndex = bodyIndex * 4;

        Color tempColor = new Color();
        tempColor.a = (alpha * a);
        tempColor.r = (r);
        tempColor.g = (g);
        tempColor.b = (b);

        colors.Add(tempColor);
        colors.Add(tempColor);
        colors.Add(tempColor);
        colors.Add(tempColor);
    }

    #region 颜色值
    public float r = 1;
    public float g = 1;
    public float b = 1;
    public float a = 1;


    public void setColor(Color col)
    {
        setColor(col.r, col.g, col.b, col.a);
    }

    public void setColor(float r, float g, float b, float a)
    {
        this.r = r;
        this.g = g;
        this.b = b;
        this.a = a;
    }

    public void setAlpha(float a)
    {
        this.a = a;
    }
    #endregion


    #endregion

}

