
using UnityEngine;


public class TextureNode
{



    public Texture2D texture;


    public bool mipmap;

    public int refCount;

    public TextureNode(bool mipmap)
    {
        this.mipmap = mipmap;
    }

    public void addRef()
    {
        refCount++;
    }

    public bool releaseRef()
    {
        refCount--;

        return refCount <= 0;
    }

}