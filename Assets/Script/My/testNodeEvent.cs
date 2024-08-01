using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class testNodeEvent : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void OnMouseUpAsButton()
    {
        Debug.Log($"发现石油！我叫{transform.name}，我在{transform.position}！");
    }

}
