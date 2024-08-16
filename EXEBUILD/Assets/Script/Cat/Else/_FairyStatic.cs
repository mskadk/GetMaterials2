//using UnityEngine;


//public class FairyStatic
//{

//    //public const string url_内网 = "http://fwt.mengdongshiji.com:9565/server_list";
//    //public const string url_玩家测试 = "http://fwct3l1.mengdongshiji.com:9565/server_list";
//    //public const string url_审核 = "http://shenhe.mengdongshiji.com:9565/server_list";

//    public const string url_内网 = "http://fwt.mengdongshiji.com:9565/server_list";
//    //public const string url_内网 = "http://demo0l1.mengdongshiji.com:9565/server_list";
//    public const string url_玩家测试 = "http://demo0l1.mengdongshiji.com:9565/server_list";
//    public const string url_审核 = "http://demo0l1.mengdongshiji.com:9565/server_list";




//#if _审核版
//    public static string firstUrl = url_审核;
//#elif INTERNAL_TEST
//    //内网服
//    public static string firstUrl = url_内网;
//#else
//    //测试服
//    //public static string firstUrl = url_玩家测试;
//    public static string firstUrl = url_内网;
//#endif


//    public static int FrameRate = 60;

//    //使用ab资源
//    public static bool useAddressable = true;
//    //使用本地资源
//    public static bool useStreamingAssets = false;


//    public const int LogicFrameRate = 60;



//    //是否GM模式
//    public static bool GM_Enable = false;

//    //是否玩家测试封测  关闭注册
//    public static bool ALPHA_Enable = true;


//    //是否查找bug 模式
//    public static bool Debug_Enable = false;
//    //显示
//    public static bool DebugGraph_Enable = false;


//    //是否是开发者 机器
//    public static bool DevelopPC_Enable = false;

//    public static bool isFairyPc = false;









//    //public const int scrWidth = 2400;
//    //public const int scrHeight = 1080;

//    public const int scrWidth = 1800;
//    public const int scrHeight = 810;


//    public const int HUDWidth = (scrWidth >> 1);
//    public const int HUDHeight = (scrHeight >> 1);


//    public static string loadName = "";


//    //主存档目录  使用的沙盒目录
//    public static string ArchiveRoot;
//    //主资源目录
//    public static string AssetsRoot = "";


//    public static string prefabRoot = "Prefab/";
//    public static string particleRoot = "Particle/";
//    public static string shaderRoot = "Shader/";
//    public static string materialsRoot = "Materials/";



//    public static string abRoot = "assetBundle/";
//    public static string bgRoot = "bg/";
//    public static string imgRoot = "img/";
//    public static string defRoot = "def/";
//    public static string live2dRoot = "live2d/";
//    public static string luaRoot = "lua/";
//    public static string spriteRoot = "sprite/";
//    public static string spriteHRoot = "spriteH/";
//    public static string spineRoot = "spine/";
//    public static string mapRoot = "map/";
//    public static string audioRoot = "audio/";
//    public static string dbroot = "db/";
//    public static string video = "video/";

//    public static string starRoot = "starjson/";
//    public static string spineShipRoot = "airshipspine/";

//    public static void initRoot()
//    {
//        ArchiveRoot = Application.persistentDataPath + "/";
//        //AssetsRoot = VersionConfig.getAssetsRoot();


//#if UNITY_EDITOR || INTERNAL_TEST

//        ALPHA_Enable = false;
//        Debug_Enable = true;
//        GM_Enable = true;
//        DevelopPC_Enable = true;

//        Debug.Log("=====" + SystemInfo.deviceName.ToUpper());
//        if (SystemInfo.deviceName.ToUpper().Equals("NUC"))
//            //晓帆pc
//        {
//           AssetsRoot = "D:/work/manager/Assets WorkSpace/FreeWorld/";
//        }



//#endif

//        Debug.Log("use resource path=" + AssetsRoot);
//    }


//}

