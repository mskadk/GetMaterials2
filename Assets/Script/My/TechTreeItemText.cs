using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TechTreeItemText : MonoBehaviour
{
    public Text t_times;
    public Text t_id;
    public Text t_name;
    public Text t_desc;

    void Awake()
    {
        t_times = transform.Find("times").GetComponent<Text>();
        t_id = transform.Find("id").GetComponent<Text>();
        t_name = transform.Find("name").GetComponent<Text>();
        t_desc = transform.Find("desc").GetComponent<Text>();
    }
}
