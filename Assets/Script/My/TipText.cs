using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TipText : MonoBehaviour
{
    Text t;
    void Awake()
    {
        t = GetComponent<Text>();
    }
    private void OnEnable()
    {
        // 订阅事件
        EventCenter.Instance.OnLogMessage += Log;
        EventCenter.Instance.OnLogWarning += LogWarning;
        EventCenter.Instance.OnLogError += LogError;
    }
    private void OnDisable()
    {
        // 取消订阅
        if (EventCenter.Instance != null)
        {
            EventCenter.Instance.OnLogMessage -= Log;
            EventCenter.Instance.OnLogWarning -= LogWarning;
            EventCenter.Instance.OnLogError -= LogError;
        }
    }

    public void Log(string str)
    {
        t.color = Color.gray;
        log(str);
    }

    public void LogWarning(string str)
    {
        t.color = Color.yellow;
        log(str);
    }

    public void LogError(string str)
    {
        t.color = Color.red;
        log(str);
    }

    private void log(string str)
    {
        t.text = str;
        StopAllCoroutines();
        StartCoroutine(holdAndFade());
    }

    IEnumerator holdAndFade()
    {
        Color c = t.color;
        yield return new WaitForSeconds(2);
        while (t.color.a > .5f)
        {
            t.color = new(t.color.r, t.color.g, t.color.b, t.color.a - .05f);
            yield return new WaitForSeconds(.05f);
        }
    }

}
