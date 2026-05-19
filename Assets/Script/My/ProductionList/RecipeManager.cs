using Assets.Script.My;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RecipeManager : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        //SpriteManager.Paint(this.gameObject, "T_Materials", 0, 0);
        SpriteManager.PaintUI(GameObject.Find("Image_001"),"T_Materials",0,0);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
