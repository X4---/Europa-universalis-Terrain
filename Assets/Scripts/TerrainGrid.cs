using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainGrid : MonoBehaviour {

    public TerrainCell kcell;
    // Use this for initialization

    private TerrainCell[] cells;

    private void Awake()
    {
        var objtar = kcell.gameObject;
        var transformcache = this.transform;
        cells = new TerrainCell[ConfigParam.BLOCKWIDTHCOUNT * ConfigParam.BLOCKHEIGHTCOUNT];

        LC_Helper.DoubleLoop(ConfigParam.BLOCKWIDTHCOUNT, ConfigParam.BLOCKHEIGHTCOUNT, ( i, j) =>
        {
            var ins = GameObject.Instantiate(objtar, transformcache);
            var cell = ins.GetComponent<TerrainCell>();

            cells[i * ConfigParam.BLOCKHEIGHTCOUNT + j] = cell;

            cell.Gen(i, j);
            cell.Apply();
        });
    }
}
