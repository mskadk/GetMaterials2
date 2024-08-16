using UnityEngine;


//#if ADDRESSABLES_ENABLED
using UnityEngine.AddressableAssets;
//#endif

public class ResourceLoader
{

    public static string addressResRoot = "Assets/Resources/";


    public static Shader LoadShader(string path, string shaderName)
    {

#if UNITY_EDITOR
        return UnityEditor.AssetDatabase.LoadAssetAtPath<Shader>(addressResRoot + path);
#endif

        //#if ADDRESSABLES_ENABLED
        return Load<Shader>(path);
        //#else
        //        return Shader.Find(shaderName);    
        //#endif

    }

    public static T Load<T>(string path) where T : UnityEngine.Object
    {
        string url = addressResRoot + path;
        return Resources.Load<T>(url);


//#if UNITY_EDITOR
//        string url = addressResRoot + path;
//        return UnityEditor.AssetDatabase.LoadAssetAtPath<T>(url);
//#endif


        //        //#if ADDRESSABLES_ENABLED
        //        string key = "Assets/AddressResources/" + path;
        //        key = key.Replace("\\", "/");
        //        Debug.Log("key=" + key);
        //        var op = Addressables.LoadAssetAsync<T>(key);


        //        T go = op.WaitForCompletion();

        //        return go;
        //        //#else
        //        //        return null;
        //        //#endif


    }



}