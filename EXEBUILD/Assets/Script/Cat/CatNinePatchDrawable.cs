using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class CatNinePatchDrawable
{
    private CatNinePatch patch;

    /**
	 * Creates an unitialized NinePatchDrawable. The ninepatch must be set
	 * before use.
	 */
    public CatNinePatchDrawable()
    {
    }

    public CatNinePatchDrawable(CatNinePatch patch)
    {
        setPatch(patch);
        //patch.scale(1/Global.SPRITE_RES_SCALE, 1/Global.SPRITE_RES_SCALE);
    }

    public CatNinePatchDrawable(CatNinePatchDrawable drawable)
    {
        //super(drawable);
        setPatch(drawable.patch);
    }

    public void draw(MeshGroup g, Material material,float x, float y, float width, float height)
    {
        //		Matrix4 transform=new Matrix4();
        //		transform.setToScaling(2, 2, 1);
        //		transform.setTranslation(Global.halfHUDW*(1-2), Global.halfHUDH*(1-2), 0);
        //		g.setTransformMatrix(transform);

        //x += g.transy;
        //y += g.transy;
        //y = FairyStatic.scrHeight - y - height;

        patch.draw(g, material,x, y, width, height);
        //		transform=new Matrix4();
        //		g.setTransformMatrix(transform);
    }

    public void setPatch(CatNinePatch patch)
    {
        this.patch = patch;
        //setMinWidth(patch.getTotalWidth());
        //setMinHeight(patch.getTotalHeight());
        //setTopHeight(patch.getPadTop());
        //setRightWidth(patch.getPadRight());
        //setBottomHeight(patch.getPadBottom());
        //setLeftWidth(patch.getPadLeft());
    }

    public CatNinePatch getPatch()
    {
        return patch;
    }

    public void resetPatch(int cornerLR, int cornerLR2, int cornerTB, int cornerTB2)
    {
        bool changed = patch.resetPatch(cornerLR, cornerLR2, cornerTB, cornerTB2);
        //		if(changed){
        //			patch.scale(Global.RES_SCALE, Global.RES_SCALE);
        //		}
    }
}
