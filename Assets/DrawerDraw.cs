using Assets.Script.My;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class DrawerDraw : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Stopwatch sw = Stopwatch.StartNew();
        sw.Start();

        for (int i = 0; i < 1; i++)
        {
            SpritePainter.Paint(gameObject, "T_Materials", 0, i % 100);
        }

        sw.Stop();
        //UnityEngine.Debug.Log(sw.ElapsedMilliseconds);

    }

    // Update is called once per frame
    void Update()
    {

    }
}
