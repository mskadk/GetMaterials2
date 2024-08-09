using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LineAnchor : MonoBehaviour
{
    LineRenderer lr;
    bool grab = false;
    // Start is called before the first frame update
    void Start()
    {
        lr = transform.parent.gameObject.GetComponent<LineRenderer>();
        transform.Find("text").GetComponent<TextMesh>().text = name;
    }

    // Update is called once per frame
    void Update()
    {
        if (!grab)
        {
            int i = int.Parse(name);
            Vector3 pos = lr.GetPosition(i) + Vector3.back;
            transform.position = pos;
            
        }
    }
}
