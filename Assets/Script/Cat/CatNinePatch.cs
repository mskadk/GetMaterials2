using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class CatNinePatch
{
    public static  int TOP_LEFT = 0;
    public static  int TOP_CENTER = 1;
    public static  int TOP_RIGHT = 2;
    public static  int MIDDLE_LEFT = 3;
    public static  int MIDDLE_CENTER = 4;
    public static  int MIDDLE_RIGHT = 5;
    public static  int BOTTOM_LEFT = 6;
    public static  int BOTTOM_CENTER = 7;
    public static  int BOTTOM_RIGHT = 8;

    static private  Color tempColor = new Color();

    private Texture texture;
    private int bottomLeft = -1, bottomCenter = -1, bottomRight = -1;
    private int middleLeft = -1, middleCenter = -1, middleRight = -1;
    private int topLeft = -1, topCenter = -1, topRight = -1;
    private float leftWidth, rightWidth, middleWidth, middleHeight, topHeight, bottomHeight;
    private float[] vertices = new float[9 * 4 * 5];
    private int idx;
    private Color color = Color.white;//new Color(Color.WHITE);
    private float padLeft = -1, padRight = -1, padTop = -1, padBottom = -1;

    /** @param left Pixels from left edge.
	 * @param right Pixels from right edge.
	 * @param top Pixels from top edge.
	 * @param bottom Pixels from bottom edge. */
    public CatNinePatch(Texture texture, int left, int right, int top, int bottom)
    {
       new CatNinePatch(new TextureRegion(texture), left, right, top, bottom);
    }

    /** @param left Pixels from left edge.
	 * @param right Pixels from right edge.
	 * @param top Pixels from top edge.
	 * @param bottom Pixels from bottom edge. */
    public CatNinePatch(TextureRegion region, int left, int right, int top, int bottom)
    {
        if (region == null) {
            Debug.LogError("region is null");
            //throw new IllegalArgumentException("region cannot be null.");
        }

        this.region = region;
        initPatch(region, left, right, top, bottom);
    }
    private TextureRegion region;
    private int left = 0, right = 0, top = 0, bottom = 0;
    public bool resetPatch(int left, int right, int top, int bottom)
    {
        if (this.left != left || this.right != right || this.top != top || this.bottom != bottom)
        {
            idx = 0;
            initPatch(region, left, right, top, bottom);
            return true;
        }
        else
        {
            //相同
            return false;
        }
    }

    private void initPatch(TextureRegion region, int left, int right, int top, int bottom)
    {
        int middleWidth = region.getRegionWidth() - left - right;
        int middleHeight = region.getRegionHeight() - top - bottom;

        TextureRegion[] patches = new TextureRegion[9];
        if (top > 0)
        {
            if (left > 0) patches[0] = new TextureRegion(region, 0, 0, left, top);
            if (middleWidth > 0) patches[1] = new TextureRegion(region, left, 0, middleWidth, top);
            if (right > 0) patches[2] = new TextureRegion(region, left + middleWidth, 0, right, top);
        }
        if (middleHeight > 0)
        {
            if (left > 0) patches[3] = new TextureRegion(region, 0, top, left, middleHeight);
            if (middleWidth > 0) patches[4] = new TextureRegion(region, left, top, middleWidth, middleHeight);
            if (right > 0) patches[5] = new TextureRegion(region, left + middleWidth, top, right, middleHeight);
        }
        if (bottom > 0)
        {
            if (left > 0) patches[6] = new TextureRegion(region, 0, top + middleHeight, left, bottom);
            if (middleWidth > 0) patches[7] = new TextureRegion(region, left, top + middleHeight, middleWidth, bottom);
            if (right > 0) patches[8] = new TextureRegion(region, left + middleWidth, top + middleHeight, right, bottom);
        }

        // If split only vertical, move splits from right to center.
        if (left == 0 && middleWidth == 0)
        {
            patches[TOP_CENTER] = patches[TOP_RIGHT];
            patches[MIDDLE_CENTER] = patches[MIDDLE_RIGHT];
            patches[BOTTOM_CENTER] = patches[BOTTOM_RIGHT];
            patches[TOP_RIGHT] = null;
            patches[MIDDLE_RIGHT] = null;
            patches[BOTTOM_RIGHT] = null;
        }
        // If split only horizontal, move splits from bottom to center.
        if (top == 0 && middleHeight == 0)
        {
            patches[MIDDLE_LEFT] = patches[BOTTOM_LEFT];
            patches[MIDDLE_CENTER] = patches[BOTTOM_CENTER];
            patches[MIDDLE_RIGHT] = patches[BOTTOM_RIGHT];
            patches[BOTTOM_LEFT] = null;
            patches[BOTTOM_CENTER] = null;
            patches[BOTTOM_RIGHT] = null;
        }

        load(patches);
    }

    public CatNinePatch(Texture texture, Color color)
    {
        setCatNinePatch(new TextureRegion(texture));
        setColor(color);
    }

    public CatNinePatch(Texture texture)
    {
        setCatNinePatch(new TextureRegion(texture));
    }

    public CatNinePatch(TextureRegion region, Color color)
    {
        setCatNinePatch(region);
        setColor(color);
    }

    public void setCatNinePatch(TextureRegion region)
    {
        load(new TextureRegion[] {
			//
			null, null, null, //
			null, region, null, //
			null, null, null //
		});
    }

    public CatNinePatch(TextureRegion[] patches)
    {
        if (patches == null || patches.Length != 9)
        {
            throw new Exception("NinePatch needs nine TextureRegions");
        }
            

        load(patches);

        float leftWidth = getLeftWidth();
        if ((patches[TOP_LEFT] != null && patches[TOP_LEFT].getRegionWidth() != leftWidth)
            || (patches[MIDDLE_LEFT] != null && patches[MIDDLE_LEFT].getRegionWidth() != leftWidth)
            || (patches[BOTTOM_LEFT] != null && patches[BOTTOM_LEFT].getRegionWidth() != leftWidth))
        {
            throw new Exception("Left side patches must have the same width");
        }

        float rightWidth = getRightWidth();
        if ((patches[TOP_RIGHT] != null && patches[TOP_RIGHT].getRegionWidth() != rightWidth)
            || (patches[MIDDLE_RIGHT] != null && patches[MIDDLE_RIGHT].getRegionWidth() != rightWidth)
            || (patches[BOTTOM_RIGHT] != null && patches[BOTTOM_RIGHT].getRegionWidth() != rightWidth))
        {
            throw new Exception("Right side patches must have the same width");
        }

        float bottomHeight = getBottomHeight();
        if ((patches[BOTTOM_LEFT] != null && patches[BOTTOM_LEFT].getRegionHeight() != bottomHeight)
            || (patches[BOTTOM_CENTER] != null && patches[BOTTOM_CENTER].getRegionHeight() != bottomHeight)
            || (patches[BOTTOM_RIGHT] != null && patches[BOTTOM_RIGHT].getRegionHeight() != bottomHeight))
        {
            throw new Exception("Bottom side patches must have the same height");
        }

        float topHeight = getTopHeight();
        if ((patches[TOP_LEFT] != null && patches[TOP_LEFT].getRegionHeight() != topHeight)
            || (patches[TOP_CENTER] != null && patches[TOP_CENTER].getRegionHeight() != topHeight)
            || (patches[TOP_RIGHT] != null && patches[TOP_RIGHT].getRegionHeight() != topHeight))
        {
            throw new Exception("Top side patches must have the same height");
        }
    }

    public CatNinePatch(CatNinePatch ninePatch)
    {
        new CatNinePatch(ninePatch, ninePatch.color);
    }

    public CatNinePatch(CatNinePatch ninePatch, Color color)
    {
        texture = ninePatch.texture;

        bottomLeft = ninePatch.bottomLeft;
        bottomCenter = ninePatch.bottomCenter;
        bottomRight = ninePatch.bottomRight;
        middleLeft = ninePatch.middleLeft;
        middleCenter = ninePatch.middleCenter;
        middleRight = ninePatch.middleRight;
        topLeft = ninePatch.topLeft;
        topCenter = ninePatch.topCenter;
        topRight = ninePatch.topRight;

        leftWidth = ninePatch.leftWidth;
        rightWidth = ninePatch.rightWidth;
        middleWidth = ninePatch.middleWidth;
        middleHeight = ninePatch.middleHeight;
        topHeight = ninePatch.topHeight;
        bottomHeight = ninePatch.bottomHeight;

        vertices = new float[ninePatch.vertices.Length];

        ninePatch.vertices.CopyTo(vertices, 0);
        //System.arraycopy(ninePatch.vertices, 0, vertices, 0, ninePatch.vertices.Length);
        idx = ninePatch.idx;
        this.color=(color);
    }

    private void load(TextureRegion[] patches)
    {
        float color = 1;// Color.white.toFloatBits();
        Debug.LogError(111);
        if (patches[BOTTOM_LEFT] != null)
        {
            bottomLeft = add(patches[BOTTOM_LEFT], color);
            leftWidth = patches[BOTTOM_LEFT].getRegionWidth();
            bottomHeight = patches[BOTTOM_LEFT].getRegionHeight();
        }
        if (patches[BOTTOM_CENTER] != null)
        {
            bottomCenter = add(patches[BOTTOM_CENTER], color);
            middleWidth = Math.Max(middleWidth, patches[BOTTOM_CENTER].getRegionWidth());
            bottomHeight = Math.Max(bottomHeight, patches[BOTTOM_CENTER].getRegionHeight());
        }
        if (patches[BOTTOM_RIGHT] != null)
        {
            bottomRight = add(patches[BOTTOM_RIGHT], color);
            rightWidth = Math.Max(rightWidth, patches[BOTTOM_RIGHT].getRegionWidth());
            bottomHeight = Math.Max(bottomHeight, patches[BOTTOM_RIGHT].getRegionHeight());
        }
        if (patches[MIDDLE_LEFT] != null)
        {
            middleLeft = add(patches[MIDDLE_LEFT], color);
            leftWidth = Math.Max(leftWidth, patches[MIDDLE_LEFT].getRegionWidth());
            middleHeight = Math.Max(middleHeight, patches[MIDDLE_LEFT].getRegionHeight());
        }
        if (patches[MIDDLE_CENTER] != null)
        {
            middleCenter = add(patches[MIDDLE_CENTER], color);
            middleWidth = Math.Max(middleWidth, patches[MIDDLE_CENTER].getRegionWidth());
            middleHeight = Math.Max(middleHeight, patches[MIDDLE_CENTER].getRegionHeight());
        }
        if (patches[MIDDLE_RIGHT] != null)
        {
            middleRight = add(patches[MIDDLE_RIGHT], color);
            rightWidth = Math.Max(rightWidth, patches[MIDDLE_RIGHT].getRegionWidth());
            middleHeight = Math.Max(middleHeight, patches[MIDDLE_RIGHT].getRegionHeight());
        }
        if (patches[TOP_LEFT] != null)
        {
            topLeft = add(patches[TOP_LEFT], color);
            leftWidth = Math.Max(leftWidth, patches[TOP_LEFT].getRegionWidth());
            topHeight = Math.Max(topHeight, patches[TOP_LEFT].getRegionHeight());
        }
        if (patches[TOP_CENTER] != null)
        {
            topCenter = add(patches[TOP_CENTER], color);
            middleWidth = Math.Max(middleWidth, patches[TOP_CENTER].getRegionWidth());
            topHeight = Math.Max(topHeight, patches[TOP_CENTER].getRegionHeight());
        }
        if (patches[TOP_RIGHT] != null)
        {
            topRight = add(patches[TOP_RIGHT], color);
            rightWidth = Math.Max(rightWidth, patches[TOP_RIGHT].getRegionWidth());
            topHeight = Math.Max(topHeight, patches[TOP_RIGHT].getRegionHeight());
        }
        if (idx < vertices.Length)
        {
            float[] newVertices = new float[idx];
            //System.arraycopy(vertices, 0, newVertices, 0, idx);
            vertices.CopyTo(newVertices, 0);
            vertices = newVertices;
        }
    }

    private int add(TextureRegion region, float color)
    {
        if (texture == null)
            texture = region.getTexture();
        else if (texture != region.getTexture()) //
            throw new Exception("All regions must be from the same texture.");

         float u = region.getU();
         float v = region.getV2();
         float u2 = region.getU2();
         float v2 = region.getV();
         float[] vertices = this.vertices;

        idx += 2;
        vertices[idx++] = color;
        vertices[idx++] = u;
        vertices[idx] = v;
        idx += 3;
        vertices[idx++] = color;
        vertices[idx++] = u;
        vertices[idx] = v2;
        idx += 3;
        vertices[idx++] = color;
        vertices[idx++] = u2;
        vertices[idx] = v2;
        idx += 3;
        vertices[idx++] = color;
        vertices[idx++] = u2;
        vertices[idx++] = v;

        return idx - 4 * 5;
    }

    private void set(int idx, float x, float y, float width, float height, float color)
    {
         float fx2 = x + width;
         float fy2 = y + height;
         float[] vertices = this.vertices;
        vertices[idx++] = x;
        vertices[idx++] = y;
        vertices[idx] = color;
        idx += 3;
        vertices[idx++] = x;
        vertices[idx++] = fy2;
        vertices[idx] = color;
        idx += 3;
        vertices[idx++] = fx2;
        vertices[idx++] = fy2;
        vertices[idx] = color;
        idx += 3;
        vertices[idx++] = fx2;
        vertices[idx++] = y;
        vertices[idx] = color;
    }

    public void draw(MeshGroup batch, Material material, float x, float y, float width, float height)
    {
        //		此处更改宽高
        float centerColumnX = x + leftWidth;
        float rightColumnX = x + width - rightWidth;
        float middleRowY = y + bottomHeight;
        float topRowY = y + height - topHeight;

        float c = 1;// tempColor.mul(batch.getColor()).toFloatBits();

        if (bottomLeft != -1) set(bottomLeft, x, y, centerColumnX - x, middleRowY - y, c);
        if (bottomCenter != -1) set(bottomCenter, centerColumnX, y, rightColumnX - centerColumnX, middleRowY - y, c);
        if (bottomRight != -1) set(bottomRight, rightColumnX, y, x + width - rightColumnX, middleRowY - y, c);
        if (middleLeft != -1) set(middleLeft, x, middleRowY, centerColumnX - x, topRowY - middleRowY, c);
        if (middleCenter != -1)
            set(middleCenter, centerColumnX, middleRowY, rightColumnX - centerColumnX, topRowY - middleRowY, c);
        if (middleRight != -1) set(middleRight, rightColumnX, middleRowY, x + width - rightColumnX, topRowY - middleRowY, c);
        if (topLeft != -1) set(topLeft, x, topRowY, centerColumnX - x, y + height - topRowY, c);
        if (topCenter != -1) set(topCenter, centerColumnX, topRowY, rightColumnX - centerColumnX, y + height - topRowY, c);
        if (topRight != -1) set(topRight, rightColumnX, topRowY, x + width - rightColumnX, y + height - topRowY, c);
        //batch.draw(texture, vertices, 0, idx);
    }

    /** Multiplies the top/left/bottom/right sizes and padding by the specified amount. */
    public void scale(float scaleX, float scaleY)
    {
        leftWidth *= scaleX;
        rightWidth *= scaleX;
        topHeight *= scaleY;
        bottomHeight *= scaleY;
        padLeft *= scaleX;
        padRight *= scaleX;
        padTop *= scaleY;
        padBottom *= scaleY;
    }

    public void setColor(Color color)
    {
        this.color=(color);
    }

    public Color getColor()
    {
        return color;
    }

    public float getLeftWidth()
    {
        return leftWidth;
    }

    public void setLeftWidth(float leftWidth)
    {
        this.leftWidth = leftWidth;
    }

    public float getRightWidth()
    {
        return rightWidth;
    }

    public void setRightWidth(float rightWidth)
    {
        this.rightWidth = rightWidth;
    }

    public float getTopHeight()
    {
        return topHeight;
    }

    public void setTopHeight(float topHeight)
    {
        this.topHeight = topHeight;
    }

    public float getBottomHeight()
    {
        return bottomHeight;
    }

    public void setBottomHeight(float bottomHeight)
    {
        this.bottomHeight = bottomHeight;
    }

    public float getMiddleWidth()
    {
        return middleWidth;
    }

    public void setMiddleWidth(float middleWidth)
    {
        this.middleWidth = middleWidth;
    }

    public float getMiddleHeight()
    {
        return middleHeight;
    }

    public void setMiddleHeight(float middleHeight)
    {
        this.middleHeight = middleHeight;
    }

    public float getTotalWidth()
    {
        return leftWidth + middleWidth + rightWidth;
    }

    public float getTotalHeight()
    {
        return topHeight + middleHeight + bottomHeight;
    }

    public void setPadding(int left, int right, int top, int bottom)
    {
        this.padLeft = left;
        this.padRight = right;
        this.padTop = top;
        this.padBottom = bottom;
    }

    /** Returns the left padding if set, else returns {@link #getLeftWidth()}. */
    public float getPadLeft()
    {
        if (padLeft == -1) return getLeftWidth();
        return padLeft;
    }

    public void setPadLeft(int left)
    {
        this.padLeft = left;
    }

    /** Returns the right padding if set, else returns {@link #getRightWidth()}. */
    public float getPadRight()
    {
        if (padRight == -1) return getRightWidth();
        return padRight;
    }

    public void setPadRight(int right)
    {
        this.padRight = right;
    }

    /** Returns the top padding if set, else returns {@link #getTopHeight()}. */
    public float getPadTop()
    {
        if (padTop == -1) return getTopHeight();
        return padTop;
    }

    public void setPadTop(int top)
    {
        this.padTop = top;
    }

    /** Returns the bottom padding if set, else returns {@link #getBottomHeight()}. */
    public float getPadBottom()
    {
        if (padBottom == -1) return getBottomHeight();
        return padBottom;
    }

    public void setPadBottom(int bottom)
    {
        this.padBottom = bottom;
    }

    public Texture getTexture()
    {
        return texture;
    }
}
