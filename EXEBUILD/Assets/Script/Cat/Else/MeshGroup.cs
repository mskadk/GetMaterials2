using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;


/// <summary>
/// render +　mesh
/// </summary>
public class MeshGroup
{

    ///////////////////////////END-原生API//////////////////////////////////////

    public static int HCENTER = 1;
    public static int VCENTER = 2;
    public static int LEFT = 4;
    public static int RIGHT = 8;
    public static int TOP = 16;
    public static int BOTTOM = 32;
    public static int BASELINE = 64;
    //addctive
    public static int CENTER = HCENTER | VCENTER;
    public static int TOP_HCENTER = TOP | HCENTER;
    public static int BOTTOM_HCENTER = BOTTOM | HCENTER;

    public static int LEFT_TOP = LEFT | TOP;
    public static int LEFT_VCENTER = LEFT | VCENTER;
    public static int LEFT_BOTTOM = LEFT | BOTTOM;

    public static int RIGHT_TOP = RIGHT | TOP;
    public static int RIGHT_VCENTER = RIGHT | VCENTER;
    public static int RIGHT_BOTTOM = RIGHT | BOTTOM;


    protected List<Material> materialList = new List<Material>();
    protected List<int[]> meshSub = new List<int[]>();
    
    protected MeshBody meshBody;
    protected Material lastMaterial;

    protected int lastIds = 0;

    public bool isDrawElements = false;

    public MeshGroup()
    {
        meshBody = new MeshBody();
        
    }
    

    private void resetDrawing()
    {
        lastIds = 0;
        meshBody.Ids = 0;
        lastMaterial = null;

        materialList.Clear();
        meshSub.Clear();

        meshBody.resetDrawBody();
    }


    public void drawImage(Material material, TextureRegion region, float dx, float dy, float scaleX, float scaleY, float rotate, float alpha)
    {
        drawImage(material, region, dx, dy, scaleX, scaleY, rotate, alpha, 0);
    }

    public void drawImage(Material material, TextureRegion region, float dx, float dy,float scaleX, float scaleY,float rotate, float alpha, int anchor = 0)
    {

        float dwWidth = region.getRegionWidth() * scaleX;
        float dwHeight = region.getRegionHeight() * scaleY;

        drawModule(material, region, dx, dy, dwWidth, dwHeight, rotate, alpha, anchor);
    }

    public virtual void drawModule(Material material, TextureRegion region, float dx, float dy, float dwWidth, float dwHeight, float rotate, float alpha, int anchor)
    {
        if(!isDrawElements)
        {
            resetDrawing();
            isDrawElements = true;
        }

        meshBody.addDrawBody();
        //Texture2D texture = region.getTexture();

        if (material != lastMaterial && lastMaterial != null)
        {
            flashMaterial(lastMaterial);
        }

        //对其方式
        if (anchor == 0)
        {
            anchor = BOTTOM | LEFT;
        }
        // vertical
        if ((anchor & TOP) != 0)
        {
            dy += dwHeight;
        }
        else if ((anchor & VCENTER) != 0)
        {
            dy -= (dwHeight/2);
        }
        // horizontal
        if ((anchor & RIGHT) != 0)
        {
            dx -= dwWidth;// region.getRegionWidth();
        }
        else if ((anchor & HCENTER) != 0)
        {
            dx -= (dwWidth/2);
        }
        //---

        meshBody.addVertices(dx, dy, 0, 0, dwWidth, dwHeight, 1, 1, rotate);
        meshBody.addTriangles();
        meshBody.addNormals();
        meshBody.addColor32(alpha);

        float u = region.getU();
        float v = region.getV();
        float u2 = region.getU2();
        float v2 = region.getV2();
        meshBody.addUvs(u, v, u2, v2);

        
        lastMaterial = material;
    }


    public virtual void drawModule(Material material, TextureRegion region, float x, float y, float originX, float originY, float width, float height, float scaleX, float scaleY, float rotation, float alpha)
    {
        if (!isDrawElements)
        {
            resetDrawing();
            isDrawElements = true;
        }

        meshBody.addDrawBody();

        if (material != lastMaterial && lastMaterial != null)
        {
            flashMaterial(lastMaterial);
        }

        meshBody.addVertices(x, y, originX, originY, width, height, scaleX, scaleY, rotation);
        meshBody.addTriangles();
        meshBody.addNormals();
        meshBody.addColor32(alpha);

        float u = region.getU();
        float v = region.getV();
        float u2 = region.getU2();
        float v2 = region.getV2();
        meshBody.addUvs(u, v, u2, v2);
        //meshBody.drawIndex++;
        lastMaterial = material;
    }



    public virtual void drawModule(Material material, TextureRegion region, float x, float y, float originX, float originY, float width, float height, float scaleX, float scaleY, float rotation, Color32[] colors)
    {
        if (!isDrawElements)
        {
            resetDrawing();
            isDrawElements = true;
        }

        meshBody.addDrawBody();

        if (material != lastMaterial && lastMaterial != null)
        {
            flashMaterial(lastMaterial);
        }

        meshBody.addVertices(x, y, originX, originY, width, height, scaleX, scaleY, rotation);
        meshBody.addTriangles();
        meshBody.addNormals();
        meshBody.addColor32(colors);

        float u = region.getU();
        float v = region.getV();
        float u2 = region.getU2();
        float v2 = region.getV2();
        meshBody.addUvs(u, v, u2, v2);
        //meshBody.drawIndex++;
        lastMaterial = material;
    }


    public virtual void endDrawed()
    {
        if (isDrawElements == false) return;

        flashMaterial(lastMaterial);
        meshBody.endDrawBody();

        //恢复颜色
        meshBody.setColor(1, 1, 1, 1);

        isDrawElements = false;
    }


    public virtual void flashMaterial(Material material)
    {
        int[] subTriangles = new int[meshBody.Ids - lastIds];
        for (int i = lastIds; i < meshBody.Ids; i++)
        {
            subTriangles[i - lastIds] = meshBody.triangles[i];
        }

        meshSub.Add(subTriangles);
        materialList.Add(material);

        lastIds = meshBody.Ids;
    }
    
    public MeshBody getMeshBody()
    {
        return meshBody;
    }

}