using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProvinceData{

    public int ID;
    public Color kMapColor;
    public Color kShowColor;

    public ProvinceData(int id, Color mapColor)
    {
        ID = id;
        kMapColor = mapColor;
        kShowColor = mapColor;
        kShowColor.a = 0;
    }
}
