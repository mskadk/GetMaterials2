using Assets.Script.My;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        GameObject g = SpriteManager.Paint(gameObject, "t_materials", 0, 0);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
