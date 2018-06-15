using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class RiverGrid : MonoBehaviour {

    public RiverCell kCell;
    public Texture2D kHeightMap;
    public Texture2D kRiverMap;

    public int RiverPixCount = 0;
    public int RiverCount = 0;

    public int Exi;

    private RiverCell[] cells;

    private List<RiverData> rivers = new List<RiverData>();
    private List<RiverData> saverivesr = new List<RiverData>();

    private IEnumerator gen = null;

    private void Awake()
    {
        gen = GenRiverIE();
    }

    private void Update()
    {
        var k = Time.realtimeSinceStartup;

        if (gen !=null)
        {
            while(gen.MoveNext())
            {
                var cur = Time.realtimeSinceStartup;

                if(cur - k > 1.0 / 60)
                {
                    return ;
                }
            }
        }
        
    }

    public IEnumerator GenRiverIE()
    {
        if (kRiverMap != null)
        {
            var xsize = (int)kRiverMap.width;
            var zsize = (int)kRiverMap.height;

            bool[][] Gened = new bool[xsize][];
            ObjectPool<RiverData> pool = new ObjectPool<RiverData>();

            LC_Helper.Loop(xsize, (i) =>
            {
                Gened[i] = new bool[zsize];
                LC_Helper.Loop(zsize, (j) =>
                {
                    Gened[i][j] = false;
                });

            });
            
            for(int i =0,iMax = xsize; i <iMax; ++i)
            {
                Exi = i;
                for (int j =0, jMax = zsize; j < jMax; ++j)
                {
                    bool hasmarked = Gened[i][j];

                    if( hasmarked )
                    {
                        continue;
                    }
                    var tex = kRiverMap.GetPixel(i, j);
                    
                    if (RiverData.isRiverTex(tex))
                    {
                        Vector3 pos = new Vector3(i, 0, j);
                        var newRiver = pool.New();
                        newRiver.PointGenRiver(pos, kRiverMap, Gened);
                        rivers.Add(newRiver);
                    }

                    RiverCount = rivers.Count;

                    yield return null;
                }
            }
            
        }

        yield break;
    }
    
}
