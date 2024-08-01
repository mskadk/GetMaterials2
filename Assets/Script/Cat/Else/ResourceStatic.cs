using Assets.Script.My;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;


public class ResourceStatic
{

    public static Color32 grayColor32 = new Color32(170, 170, 170, 255);
    public static Color32 color0 = new Color32(115, 67, 44, 255);


    private static Font font;
    public static void init()
    {
        //getMaps();

        font = ResourceLoader.Load<Font>("Font/SourceHanSansCN-Light.otf");
        //font = Resources.Load<Font>("Font/SourceHanSansCN-Light");
        TextureUtils.setPixelMode(font.material.mainTexture);
    }


    private static Shader shader_Stand;
    private static Shader shader_StandDynamic;
    private static Shader shader_URP_2D_LIT;
    private static Shader shader_Emission;


    public static Texture2D shareTexture2D()
    {
        return Texture2D.whiteTexture;
    }



    public static Shader share_Stand()
    {
        if (shader_Stand == null)
        {
            shader_Stand = ResourceLoader.LoadShader(MyStatic.shaderRoot + "ZFairy_Common/FairyShader_Base.shader", "ZFairy/Base");
            //shader_Stand = Shader.Find("ZFairy/Base");
            /*
#if Enable_URP_2D
            shader_Stand = Shader.Find("Universal Render Pipeline/2D/Spine/Sprite");
#else

            shader_Stand = Shader.Find("ZFairy/Base");
#endif
*/
        }
        return shader_Stand;
    }



    public static Shader share_StandDynamic()
    {
        if (shader_StandDynamic == null)
        {
            shader_StandDynamic = ResourceLoader.LoadShader(MyStatic.shaderRoot + "ZFairy_Common/FairyShader_BaseDynamic.shader", "ZFairy/BaseDynamic");
            //shader_Stand = Shader.Find("ZFairy/Base");
            /*
#if Enable_URP_2D
            shader_Stand = Shader.Find("Universal Render Pipeline/2D/Spine/Sprite");
#else

            shader_Stand = Shader.Find("ZFairy/Base");
#endif
*/
        }
        return shader_StandDynamic;
    }



    public static Shader getShaderEmission()
    {
        if (shader_Emission == null)
        {
            shader_Emission = ResourceLoader.LoadShader(MyStatic.shaderRoot + "ZFairy_Game/EmissionLight.shader", "EmissionLight");
        }

        return shader_Emission;
    }

    public static Shader share_URP_2D_Lit()
    {
        if (shader_URP_2D_LIT == null)
        {

#if Enable_URP_2DLight
            shader_URP_2D_LIT = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
#else
            //shader_URP_2D_LIT = Shader.Find("ZFairy/Base");
            shader_URP_2D_LIT = ResourceLoader.LoadShader(MyStatic.shaderRoot + "ZFairy_Common/FairyShader_Base.shader", "ZFairy/Base");
#endif

        }
        return shader_URP_2D_LIT;
    }

    public static Font shareFont()
    {
        return font;
    }


    private static Material material_White;
    public static Material getMaterial()
    {
        if(material_White == null)
        {
            material_White = new Material(share_Stand());
            material_White.mainTexture = ResourceLoader.Load<Texture2D>("NGUI/half_null.png");
            //material_White.mainTexture = Texture2D.whiteTexture;
        }
        return material_White;
    }
    
    private static Material material_Emission;
    public static Material getEmissionMaterial()
    {
        if(material_Emission == null)
        {
            material_Emission = new Material(getShaderEmission());
            material_Emission.mainTexture = ResourceLoader.Load<Texture2D>("Prefab/Carrier/carrierLine.png");
            //material_White.mainTexture = Texture2D.whiteTexture;
        }
        return material_Emission;
    }


    //private static List<PMap> pMaps;

    //public static List<PMap> getMaps()
    //{
    //    if(pMaps == null)
    //    {
    //        pMaps = new List<PMap>();
    //    }

    //    for(int i = 1; i <= 6; i++)
    //    {
    //        PMap map = PMap.loadMap(FairyStatic.mapRoot + "Ore00" + i+".map");
    //        if(map != null)
    //        {
    //            pMaps.Add(map);
    //        }
    //    }
    //    return pMaps;
    //}

    //public static MapTile[][] getRandomOre()
    //{
    //    MapTile[][] tiles = pMaps[RandomTool.getRandom(pMaps.Count)].mapArray[0];
    //    return tiles;
    //}





}