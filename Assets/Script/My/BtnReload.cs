using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BtnReload : MonoBehaviour
{
    //9EF1A0
    Button btn;
    bool c1 = false;
    Color c;
    private void Start()
    {
        btn = GetComponent<Button>();
        c = GetComponent<Image>().color;
    }
    public void Click1()
    {
        StartCoroutine(Click1I());
    }
    public IEnumerator Click1I() {
        if (!c1)
        {
            transform.GetComponentInChildren<Text>().text = "»∑∂®£ø";
            GetComponent<Image>().color = Color.green;
            c1 = true;
            yield return new WaitForSeconds(1);
            transform.GetComponentInChildren<Text>().text = "÷ÿ‘ÿ";
            GetComponent<Image>().color = c;
            c1 = false;
        }
        else
        {
            GameObject.Find("MainManager").GetComponent<MainManager>().ReloadSheets();
        }
    }
}
