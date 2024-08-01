using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using UnityEngine;



//
public delegate void PostEvent();

/*
//延时执行的帧数
public delegate void PostEventDelay(int delayFrame);*/


public class FairyRunnable : MonoBehaviour
{

    //顺序执行的
    private static List<PostEvent> orderEvents = new List<PostEvent>();

    //立即执行的
    private static List<PostEvent> immEvents = new List<PostEvent>();

    //定时执行的
    private static List<DelayData> delayEvents = new List<DelayData>();

    private int delay = 0;
    private const int runDelay = 5;

    public static void postRunnable(PostEvent even)
    {
        orderEvents.Add(even);
    }
    public static void postRunnableIm(PostEvent even)
    {
        immEvents.Add(even);
    }

    public static void postRunnableDelay(PostEvent even, int timeMM)
    {
        delayEvents.Add(new DelayData(even, timeMM));
    }

    void LateUpdate()
    {
        //执行自己配置的单步runnable，每一帧执行一次
        if (orderEvents.Count > 0 && delay == 0)
        {
            PostEvent runnable = orderEvents[0];
            orderEvents.RemoveAt(0);

            if (runnable != null)
            {
                runnable();
                delay = runDelay;
            }
            runnable = null;
        }

        delay = delay > 0 ? delay - 1 : 0;


        //执行自己配置的单步runnable，每一帧执行一次
        if (immEvents.Count > 0)
        {
            for(int i = 0; i < immEvents.Count; i++)
            {
                PostEvent runnable = immEvents[i];
                if (runnable != null)
                {
                    try
                    {
                        runnable();
                    }
                    catch(Exception e)
                    {
                        Debug.LogError(e.StackTrace);
                    }
                }
            }

            immEvents.Clear();
        }



        if (delayEvents.Count > 0)
        {
            for (int i = delayEvents.Count - 1; i >= 0; i--)
            {
                DelayData data = delayEvents[i];

                if (data.isFinish())
                {
                    data.excete();
                    delayEvents.RemoveAt(i);
                }
            }
        }



    }


    public class DelayData
    {

        private PostEvent even;

        private long createTime;
        private int timeMM;

        public DelayData(PostEvent even, int timeMM)
        {
            this.even = even;
            this.timeMM = timeMM;

            createTime = TimeTool.currentTimeMillis();
        }

        public bool isFinish()
        {
            if (TimeTool.currentTimeMillis() - createTime > timeMM)
            {
                return true;
            }
            return false;
        }

        public void excete()
        {
            if (even != null)
            {
                even();
            }
        }

    }

}



