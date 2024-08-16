using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomTool
{


    #region  random

    private static System.Random rd = new System.Random();


    public static void setRandomSeed()
    {
        int seed = unchecked((int)DateTime.Now.Ticks);
        rd = new System.Random(seed);
    }


    /**
	 * 取得随机bool值
	 */
    public static bool getRandomBool()
    {
        return (rd.Next() % 2 == 0) ? true : false;
    }

    public static int getRandom()
    {
        return rd.Next();
    }

    public static int getRandom(int range)
    {
        return rd.Next(range);
    }

    public static int getRandomInt(int min, int max)
    {
        return rd.Next(min, max);
    }
    public static float getRandomFloat(float min, float max)
    {
        float range = max - min;

        return (float)(min + rd.NextDouble() * range);
    }

    public static float getRandomFloat()
    {
        return (float)rd.NextDouble();
    }


    public static bool getRandomProbability(float procent)
    {
        float cc = getRandomFloat();
        if (cc < procent) return true;
        return false;
    }


    #endregion



}
