using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;


public class TimeTool
{



    public const int Day2MM = 86400000;
    public const int Day2Second = 86400;

    public const int Hour2MM = 3600000;
    public const int Hour2Second = 3600;

    public const int Minute2MM = 60000;
    public const int Minute2Second = 60;

    //public const int Second2MM = 1000;


    public static readonly DateTime Jan1st1970 = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    public static long currentTimeMillis()
    {
        return (long)((DateTime.UtcNow - Jan1st1970).TotalMilliseconds);
    }



    //毫秒转化为天
    public static int mmToDay(long mm)
    {
        return (int)(mm / Day2MM);
    }

    public static int mmToHour(long mm)
    {
        return (int)(mm / Hour2MM);
    }

    public static int mmToMinute(long mm)
    {
        return (int)(mm / Minute2MM);
    }

    public static int mmToSecond(long mm)
    {
        return (int)(mm / 1000);
    }

    public static string mmToString_DHM(long mm)
    {
        string str = "";

        int day = mmToDay(mm);
        int hour = mmToHour(mm % Day2MM);
        int minute = mmToMinute(mm % Hour2MM);
        //int second = mmToSecond(mm % Minute2MM);

        if (day > 0)
        {
            str += day + "Lan.day" + " ";
        }

        if(hour < 10)
        {
            str += "0";
        }
        str += hour + ":";

        if (minute < 10)
        {
            str += "0" + minute;
        }
        else
        {
            str += minute;
        }
        return str;
    }

    public static string mmToString_DHMS(long mm)
    {
        string str = "";

        int day = mmToDay(mm);
        int hour = mmToHour(mm % Day2MM);
        int minute = mmToMinute(mm % Hour2MM);
        int second = mmToSecond(mm % Minute2MM);

        if(day > 0)
        {
            str += day + "天 ";
        }

        if(hour > 0)
        {
            str += hour + ":";
        }

        if(minute < 10)
        {
            str += "0" + minute + ":";
        }
        else
        {
            str += minute + ":";
        }

        if(second < 10)
        {
            str += "0" + second;
        }
        else
        {
            str += "" + second;
        }

        return  str;
    }


    public static string mmToString_DHMS_Limit(long mm)//大于一天只显示天 
    {
        string str = "";

        int day = mmToDay(mm);
        int hour = mmToHour(mm % Day2MM);
        int minute = mmToMinute(mm % Hour2MM);
        int second = mmToSecond(mm % Minute2MM);

        if (day > 0)
        {
            str += day + "天 ";
        }
        else
        {
            if (hour > 0)
            {
                str += hour + ":";
            }

            if (minute < 10)
            {
                str += "0" + minute + ":";
            }
            else
            {
                str += minute + ":";
            }

            if (second < 10)
            {
                str += "0" + second;
            }
            else
            {
                str += "" + second;
            }
        }
        return str;
    }


    #region time



    public static string mmToTimeHHMMSS(long mm, bool cut24hr)
    {
        if (mm < 0)
        {
            return "0:00:00";
        }
        long h = mm / 3600000;
        if (cut24hr)
            h %= 24;
        long m = (60 + mm / 60000) % 60;
        long s = (60 + mm / 1000) % 60;
        String buffer = "";
        buffer += h;
        buffer += ":";
        buffer += (m < 10 ? ("0" + m) : ("" + m));
        buffer += (":");
        buffer += (s < 10 ? ("0" + s) : ("" + s));
        return buffer;
    }

    public static string mmToTimeHHMM(long mm)
    {
        if (mm < 0)
        {
            return "00:00";
        }
        long h = mm / 3600000;
        h %= 24;
        long m = (60 + mm / 60000) % 60;
        String buffer = "";
        buffer += (h);
        buffer += (":");
        buffer += (m < 10 ? ("0" + m) : ("" + m));
        return buffer;
    }

    public static string mmToTimeMMSS(int mm)
    {
        if (mm < 0)
        {
            return "00:00";
        }
        int m = (60 + mm / 60000) % 60;
        int s = (60 + mm / 1000) % 60;
        string buffer = "";
        buffer += (m < 10 ? ("0" + m) : ("" + m));
        buffer += (":");
        buffer += (s < 10 ? ("0" + s) : ("" + s));
        return buffer;
    }
    //另一种展示方式
    public static string mmToTimeHHMMSS_HMS(long mm)
    {
        if (mm < 0)
        {
            return "0h00m00s";
        }
        long h = mm / 3600000;
        h %= 24;
        long m = (60 + mm / 60000) % 60;
        long s = (60 + mm / 1000) % 60;
        string buffer = "";
        if (h > 0)
        {
            buffer += (h);
            buffer += ("h");
        }
        if (m > 0)
        {
            buffer += (m < 10 ? ("0" + m) : ("" + m));
            buffer += ("m");
        }
        else if (h > 0)
        {
            buffer += ("00m");
        }
        buffer += (s < 10 ? ("0" + s) : ("" + s));
        buffer += ("s");
        return buffer;
    }

    //另一种展示方式
    public static string ddd = "d", hhh = "h", mmm = "m", sss = "s";//时间分割符号
    public static string mmToTimeHHMMSS_HMS(long mm, bool cut24hr)
    {
        if (mm < 0)
        {
            return "0" + hhh + "00" + mmm + "00" + sss;
        }
        long h = mm / 3600000;
        if (cut24hr)
            h %= 24;
        long m = (60 + mm / 60000) % 60;
        long s = (60 + mm / 1000) % 60;
        string buffer = "";
        if (h > 0)
        {
            buffer += h;
            buffer += hhh;
        }
        if (m > 0)
        {
            buffer += (m < 10 ? ("0" + m) : ("" + m));
            buffer += (mmm);
        }
        else if (h > 0)
        {
            buffer += ("00" + mmm);
        }
        buffer += (s < 10 ? ("0" + s) : ("" + s));
        buffer += (sss);
        return buffer;
    }

    public static string mmToTimeHHMM_HM(long mm)
    {
        if (mm < 0)
        {
            return "0h00m";
        }
        long h = mm / 3600000;
        h %= 24;
        long m = (60 + mm / 60000) % 60;
        string buffer = "";
        if (h > 0)
        {
            buffer += (h);
            buffer += ("h");
        }
        buffer += (m < 10 ? ("0" + m) : ("" + m));
        buffer += ("m");
        return buffer;
    }

    public static string mmToTimeMMSS_MS(int mm)
    {
        if (mm < 0)
        {
            return "0m00s";
        }
        int m = (60 + mm / 60000) % 60;
        int s = (60 + mm / 1000) % 60;
        string buffer = "";
        if (m > 0)
        {
            buffer += (m < 10 ? ("0" + m) : ("" + m));
            buffer += ("m");
        }
        buffer += (s < 10 ? ("0" + s) : ("" + s));
        buffer += ("s");
        return buffer.ToString();
    }

    [Obsolete]
    public static DateTime mmToDateTime(long mm)
    {
        DateTime dtStart = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1));
        int curSecond = (int)(mm / 1000);
        return dtStart.AddSeconds(curSecond);
    }
    #endregion


}