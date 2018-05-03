using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class LC_Helper
{
    public static bool DoubleLoop(int firstloopcount, int secondloopcount, Action<int, int> cellcall)
    {
        for (int i = 0, iMax = firstloopcount; i < iMax; ++i)
        {
            for (int j = 0, jMax = secondloopcount; j < jMax; ++j)
            {
                cellcall(i, j);
            }
        }

        return false;
    }

    public static bool DoubleLoop(int firstloopcount, int secondloopcount, Action cellcall)
    {
        for (int i = 0, iMax = firstloopcount; i < iMax; ++i)
        {
            for (int j = 0, jMax = secondloopcount; j < jMax; ++j)
            {
                cellcall();
            }
        }

        return false;
    }
    

}
