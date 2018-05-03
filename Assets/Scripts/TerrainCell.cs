using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class TerrainCell : MonoBehaviour {

    public static int kPerCellBlockCount = 1;

    public Mesh kMesh;
    public Texture2D kHeightMap;
    public Texture2D kTerrainMap;

    [NonSerialized] public List<Vector3> vertices;
    [NonSerialized] public List<Color> colors;
    [NonSerialized] public List<Vector2> uvs;
    [NonSerialized] public List<int> triangles;

    private void Awake()
    {
        kMesh = GetComponent<MeshFilter>().mesh = new Mesh();

        var lodgroup = GetComponent<LODGroup>();
        if(lodgroup != null)
        {
            lodgroup.enabled = false;
        }
    }

    public void Apply()
    {
        kMesh.SetVertices(vertices);
        kMesh.SetColors(colors);
        kMesh.SetUVs(0,uvs);
        kMesh.SetTriangles(triangles, 0);
        kMesh.RecalculateNormals();

        var lodgroup = GetComponent<LODGroup>();
        if (lodgroup != null)
        {
            lodgroup.enabled = true;
            lodgroup.RecalculateBounds();
        }
    }

    public void Gen(int BlockCountx, int BlockCountz)
    {
        vertices = new List<Vector3>();
        colors = new List<Color>();
        uvs = new List<Vector2>();
        triangles = new List<int>();

        var xBase = ConfigParam.PERBLOCKNUMBERSIZE * BlockCountx;
        var zBase = ConfigParam.PERBLOCKNUMBERSIZE * BlockCountz;

        var heightx = ConfigParam.PERBLOCKCOUNT * BlockCountx;
        var heightz = ConfigParam.PERBLOCKCOUNT * BlockCountz;

        this.transform.localPosition = new Vector3(xBase, 0, zBase);

        //vertice;
        //ConfigParam.PERBLOCKCOUNT cell //ConfigParam.PERBLOCKCOUNT + 1 vertices;
        LC_Helper.DoubleLoop(ConfigParam.PERBLOCKCOUNT + 1, ConfigParam.PERBLOCKCOUNT + 1, (i, j) => {

            float x = i * ConfigParam.PERBLOCKWORLDSIZE; // + xBase;
            float z = j * ConfigParam.PERBLOCKWORLDSIZE; //+ zBase;

            int xx = (int)(heightx + ConfigParam.PERBLOCKUVSIZE * i);
            int zz = (int)(heightz + ConfigParam.PERBLOCKUVSIZE * j);

            float y = GetHeight(xx, zz) * ConfigParam.BLOCKHEIGHT;

            var temp = new Vector3(x, y, z);

            //kHeightMap.GetPixel(indexi, indexi);

            vertices.Add(temp);
            colors.Add(Color.white);
            uvs.Add(GetUV(xx, zz));

        });

        //triangles;

        LC_Helper.DoubleLoop(ConfigParam.PERBLOCKCOUNT, ConfigParam.PERBLOCKCOUNT, (i, j) => {

            //PerCount = ConfigParam.PERBLOCKCOUNT + 1;
            //LeftTop = i * PerCount + j
            //RightTop = i * PerCount + j + i
            //LeftDown = ( i + 1 ) * PerCount + j;
            //RightDown = ( i + 1 ) * PerCount + j + 1;

            int PerCount = ConfigParam.PERBLOCKCOUNT + 1;
            var LeftTop = i * PerCount + j;
            var RightTop = LeftTop + 1;
            var LeftDown = (i + 1) * PerCount + j;
            var RightDown = LeftDown + 1;

            triangles.Add(LeftTop);
            triangles.Add(RightDown);
            triangles.Add(LeftDown);

            triangles.Add(LeftTop);
            triangles.Add(RightTop);
            triangles.Add(RightDown);

        });

    }

    public void GenLod(int BlockCountx, int BlockCountz, int LodLevel)
    {
        vertices = new List<Vector3>();
        colors = new List<Color>();
        uvs = new List<Vector2>();
        triangles = new List<int>();

        var xBase = ConfigParam.PERBLOCKNUMBERSIZE * BlockCountx;
        var zBase = ConfigParam.PERBLOCKNUMBERSIZE * BlockCountz;

        var heightx = ConfigParam.PERBLOCKSIZE * BlockCountx;
        var heightz = ConfigParam.PERBLOCKSIZE * BlockCountz;

        //vertice;
        //ConfigParam.PERBLOCKCOUNT cell //ConfigParam.PERBLOCKCOUNT + 1 vertices;
        LC_Helper.DoubleLoop(ConfigParam.PERBLOCKCOUNT + 1, ConfigParam.PERBLOCKCOUNT + 1, (i, j) =>
        {

//             float x = i * ConfigParam.BLOCKWIDTH + xBase;
//             float z = j * ConfigParam.BLOCKWIDTH + zBase;
// 
//             int xx = heightx + ConfigParam.PERBLOCKUVSIZE * i;
//             int zz = heightz + ConfigParam.PERBLOCKUVSIZE * j;
// 
//             float y = GetHeight(xx, zz) * ConfigParam.BLOCKHEIGHT;
// 
//             var temp = new Vector3(x, y, z);
// 
//             //kHeightMap.GetPixel(indexi, indexi);
// 
//             vertices.Add(temp);
//             colors.Add(Color.white);
//             uvs.Add(GetUV(xx, zz));

        });

        //triangles;

        LC_Helper.DoubleLoop(ConfigParam.PERBLOCKCOUNT, ConfigParam.PERBLOCKCOUNT, (i, j) =>
        {

            //PerCount = ConfigParam.PERBLOCKCOUNT + 1;
            //LeftTop = i * PerCount + j
            //RightTop = i * PerCount + j + i
            //LeftDown = ( i + 1 ) * PerCount + j;
            //RightDown = ( i + 1 ) * PerCount + j + 1;

            int PerCount = ConfigParam.PERBLOCKCOUNT + 1;
            var LeftTop = i * PerCount + j;
            var RightTop = LeftTop + 1;
            var LeftDown = (i + 1) * PerCount + j;
            var RightDown = LeftDown + 1;

            triangles.Add(LeftTop);
            triangles.Add(RightDown);
            triangles.Add(LeftDown);

            triangles.Add(LeftTop);
            triangles.Add(RightTop);
            triangles.Add(RightDown);

        });

    }

    private float GetHeight(int x, int z)
    {
        x %= kHeightMap.width;
        z %= kHeightMap.height;

        return kHeightMap.GetPixel(x, z).a;

    }

    private Color GetColor(int x, int z)
    {
        x %= kTerrainMap.width;
        z %= kTerrainMap.height;

        return kTerrainMap.GetPixel(x, z);
    }

    private Vector2 GetUV(int x, int z)
    {
        x %= kTerrainMap.width;
        z %= kTerrainMap.height;

        return new Vector2((float)x / kTerrainMap.width, (float)z / kTerrainMap.height);
    }
}
