using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Math_Helper{
    
    public Math_Helper(int i)
    {
        maxIndex = i;
        Gen();
    }

    public long[][] data;
    private int maxIndex;

    private void Gen()
    {
        data = new long[maxIndex][];
        for(int i =0; i < maxIndex; ++i)
        {
            data[i] = new long[maxIndex];
            for(int j =0; j < maxIndex; ++j)
            {
                data[i][j] = 1;
            }
        }

        for(int n = 1; n < maxIndex; ++n)
        {
            for(int j = 1; j <= n; ++j)
            {
                data[n][j] = data[n][j - 1] * (n - j + 1);
            }
        }
    }

    public long Cni(int n, int i)
    {
        if( n >= maxIndex || n <0 || i > n)
        {
            return 0;
        }

        return Pni(n, i) / Pni(i, i);
    }

    public long Pni(int n, int i)
    {
        if( n >= maxIndex || n < 0 || i > n)
        {
            return 0;
        }

        return data[n][i];
    }
}
