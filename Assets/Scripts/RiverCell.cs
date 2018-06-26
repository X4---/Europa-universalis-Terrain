using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class RiverCell : MonoBehaviour {

    public Mesh kMesh;
    public Texture2D kHeightMap;
    public Texture2D kRiverMap;
    public Texture2D kNoiseMap;

    public float kNoisePercent = 0.1f;

    [NonSerialized] public List<Vector3> vertices;
    [NonSerialized] public List<Color> colors;
    [NonSerialized] public List<Vector2> uvs;
    [NonSerialized] public List<int> triangles;
    
    private int verticesOffset = 0;
    private void Awake()
    {
        kMesh = GetComponent<MeshFilter>().mesh = new Mesh();
    }

    private List<Vector3> kBoundsPoints;
    private RiverData.Bounds kCurE, kPre;

    public void Gen(RiverData data)
    {
        vertices = new List<Vector3>();
        colors = new List<Color>();
        uvs = new List<Vector2>();
        triangles = new List<int>();
        kBoundsPoints = new List<Vector3>();

        verticesOffset = 0;

        for (int i =0,iMax = data.kOrigins.Count; i<iMax; ++i)
        {
           
            var river = data.kOrigins[i];
            if((river.type & RiverData.RiverBoundsType.BeginPoint) !=0)
            {
                kCurE = river;
                kPre = null;

                while(kCurE != null)
                {
                    GenFromOriginBounds(kCurE, kPre);
                }
            }
        }
    }

    public void Apply()
    {
        kMesh.SetVertices(vertices);
        kMesh.SetColors(colors);
        kMesh.SetUVs(0, uvs);
        kMesh.SetTriangles(triangles, 0);

        Debug.Log("vertices cout " + vertices.Count);
    }

    private void GenFromOriginBounds(RiverData.Bounds kCur, RiverData.Bounds before)
    {
        if(kCur == null)
        {

            Debug.Log("CC");
        }

        if (before == null)// 这是原点
        {
            //if (kCur != null)
            {
                float centerx = kCur.pos.x + 0.5f;
                float centerz = kCur.pos.z + 0.5f;

                Vector3 center = new Vector3(centerx, 0, centerz);

                var borderleft = BorderLeft(center, kCur.flowDir);
                var borderright = BorderRight(center, kCur.flowDir);

                var downleft = DownLeft(center, kCur.flowDir);
                var downRight = DownRight(center, kCur.flowDir);

                var mid = ModifyVector(LerpPos(downleft, downRight, 0.5f));

                var l = ModifyVector(LerpPos(borderleft, borderright, 1 - kNoisePercent));
                var r = ModifyVector(LerpPos(borderright, borderleft, 1 - kNoisePercent));

                kBoundsPoints.Clear();
                kBoundsPoints.Add(l);
                kBoundsPoints.Add(r);

                vertices.Add(mid);
                vertices.Add(l);
                vertices.Add(r);
                vertices.Add(mid);

                triangles.Add(verticesOffset + 0);
                triangles.Add(verticesOffset + 1);
                triangles.Add(verticesOffset + 2);
                triangles.Add(verticesOffset + 2);
                triangles.Add(verticesOffset + 3);
                triangles.Add(verticesOffset + 0);

                verticesOffset += 4;



            }

        }
        else
        {
            var dir = kCur.flowDir;
            var nextb = kCur.GetLink(kCur.flowDir);
            if(nextb == null)
            {
                dir = before.flowDir;
            }

            float centerx = kCur.pos.x + 0.5f;
            float centerz = kCur.pos.z + 0.5f;

            Vector3 center = new Vector3(centerx, 0, centerz);

            var borderleft = BorderLeft(center, kCur.flowDir);
            var borderright = BorderRight(center, kCur.flowDir);

            var downl = kBoundsPoints[0];
            var downr = kBoundsPoints[1];

            var l = ModifyVector(LerpPos(borderleft, borderright, 1 - kNoisePercent));
            var r = ModifyVector(LerpPos(borderright, borderleft, 1 - kNoisePercent));

            kBoundsPoints.Clear();
            kBoundsPoints.Add(l);
            kBoundsPoints.Add(r);

            vertices.Add(downl);
            vertices.Add(l);
            vertices.Add(r);
            vertices.Add(downr);

            triangles.Add(verticesOffset + 0);
            triangles.Add(verticesOffset + 1);
            triangles.Add(verticesOffset + 2);
            triangles.Add(verticesOffset + 2);
            triangles.Add(verticesOffset + 3);
            triangles.Add(verticesOffset + 0);

            verticesOffset += 4;
        }


        kPre = kCurE;
        kCurE = kCur.GetLink(kCur.flowDir);
        
    }


    private Vector3 BorderLeft(Vector3 center, RiverData.Direction dir)
    {
        Vector3 result = new Vector3();
        switch(dir)
        {
            case RiverData.Direction.Up:
                result.x = center.x - 0.5f;
                result.z = center.z + 0.5f;
                break;
            case RiverData.Direction.Right:
                result.x = center.x + 0.5f;
                result.z = center.z + 0.5f;
                break;
            case RiverData.Direction.Down:
                result.x = center.x + 0.5f;
                result.z = center.z - 0.5f;
                break;
            case RiverData.Direction.Left:
                result.x = center.x - 0.5f;
                result.z = center.z - 0.5f;
                break;
        }

        return result;
    }
    private Vector3 BorderRight(Vector3 center, RiverData.Direction dir)
    {
        Vector3 result = new Vector3();
        switch (dir)
        {
            case RiverData.Direction.Up:
                result.x = center.x + 0.5f;
                result.z = center.z + 0.5f;
                break;
            case RiverData.Direction.Right:
                result.x = center.x + 0.5f;
                result.z = center.z - 0.5f;
                break;
            case RiverData.Direction.Down:
                result.x = center.x - 0.5f;
                result.z = center.z - 0.5f;
                break;
            case RiverData.Direction.Left:
                result.x = center.x - 0.5f;
                result.z = center.z + 0.5f;
                break;
        }

        return result;
    }
    private Vector3 DownLeft(Vector3 center, RiverData.Direction dir)
    {
        Vector3 result = new Vector3();
        switch (dir)
        {
            case RiverData.Direction.Up:
                result.x = center.x - 0.5f;
                result.z = center.z - 0.5f;
                break;
            case RiverData.Direction.Right:
                result.x = center.x - 0.5f;
                result.z = center.z + 0.5f;
                break;
            case RiverData.Direction.Down:
                result.x = center.x + 0.5f;
                result.z = center.z + 0.5f;
                break;
            case RiverData.Direction.Left:
                result.x = center.x + 0.5f;
                result.z = center.z - 0.5f;
                break;
        }

        return result;
    }
    private Vector3 DownRight(Vector3 center, RiverData.Direction dir)
    {
        Vector3 result = new Vector3();
        switch (dir)
        {
            case RiverData.Direction.Up:
                result.x = center.x + 0.5f;
                result.z = center.z - 0.5f;
                break;
            case RiverData.Direction.Right:
                result.x = center.x - 0.5f;
                result.z = center.z - 0.5f;
                break;
            case RiverData.Direction.Down:
                result.x = center.x - 0.5f;
                result.z = center.z + 0.5f;
                break;
            case RiverData.Direction.Left:
                result.x = center.x + 0.5f;
                result.z = center.z + 0.5f;
                break;
        }

        return result;
    }

    private Vector3 LerpPos(Vector3 Begin, Vector3 End, float range)
    {
        Vector3 result = new Vector3();
        range = Mathf.Clamp(range, 0f, 1f);
        result = Begin * range + End * (1 - range);
        return result;
    }

    private Vector3 NoiseModify(Vector3 kOri)
    {
        Vector3 result = kOri;
        int x = (int)result.x % kNoiseMap.width;
        int z = (int)result.z & kNoiseMap.height;

        float xx = kNoiseMap.GetPixel(x, z).r * kNoisePercent;
        float zz = kNoiseMap.GetPixel(x, z).r * kNoisePercent;
        result.x += xx;
        result.z += zz;

        return result;
    }
    private Vector3 HeightSample(Vector3 kOri)
    {
        Vector3 result = kOri;
        result.y = kHeightMap.GetPixel((int)kOri.x, (int)kOri.z).a * ConfigParam.BLOCKHEIGHT + 1;
        return result;
    }
    private Vector3 ModifyVector(Vector3 kOri)
    {
        return HeightSample(NoiseModify(kOri));
    }


    private void GenBounds(List<RiverData.Bounds> cached, Dictionary<RiverData.Bounds, int> gened, ref int verticesoffset)
    {
        var tar = cached[cached.Count - 1];
        cached.RemoveAt(cached.Count - 1);

        if( !gened.ContainsKey(tar) )
        {
            gened.Add(tar, 1);

            if(tar.left != null)
            {
                cached.Add(tar.left);
            }
            if(tar.right !=null)
            {
                cached.Add(tar.right);
            }
            if(tar.up != null)
            {
                cached.Add(tar.up);
            }
            if(tar.down != null)
            {
                cached.Add(tar.down);
            }
            var x = (int)tar.pos.x;
            var z = (int)tar.pos.z;

            var y = kHeightMap.GetPixel(x, z).a * ConfigParam.BLOCKHEIGHT +1;
            var ld = new Vector3(x, y, z);
            vertices.Add(ld);

            z += 1;
            y = kHeightMap.GetPixel(x, z).a * ConfigParam.BLOCKHEIGHT + 1;
            var lu = new Vector3(x, y, z);
            vertices.Add(lu);

            x += 1;
            y = kHeightMap.GetPixel(x, z).a * ConfigParam.BLOCKHEIGHT +1;
            var ru = new Vector3(x, y, z);
            vertices.Add(ru);

            //x -= 1;
            z -= 1;
            y = kHeightMap.GetPixel(x, z).a * ConfigParam.BLOCKHEIGHT + 1;
            var rd = new Vector3(x, y, z);
            vertices.Add(rd);
            
            triangles.Add(0 + verticesoffset);
            triangles.Add(1 + verticesoffset);
            triangles.Add(2 + verticesoffset);
            triangles.Add(2 + verticesoffset);
            triangles.Add(3 + verticesoffset);
            triangles.Add(0 + verticesoffset);

            Color a = Color.black;

            if((tar.type & (RiverData.RiverBoundsType.BeginPoint |
                RiverData.RiverBoundsType.EndPoint)) != 0)
            {
                a = tar.cor *  ( 1.0f -  Mathf.Clamp( tar.widthModify , 1f, 50.0f) / 50f) ;
                
            }else if( (tar.type & RiverData.RiverBoundsType.Main) != 0)
            {
                a = Color.white * (1.0f - Mathf.Clamp(tar.widthModify, 1f, 50.0f) / 50f);

            }else
            {
                if( (tar.type & RiverData.RiverBoundsType.In) != 0)
                {
                    a = Color.blue * (1.0f - Mathf.Clamp(tar.widthModify, 1f, 50.0f) / 50f);
                }
                else
                {
                    a = Color.yellow * (1.0f - Mathf.Clamp(tar.widthModify, 1f, 50.0f) / 50f);
                }

                

            }

            colors.Add(a);
            colors.Add(a);
            colors.Add(a);
            colors.Add(a);

            verticesoffset += 4;
        }
    }

    

}
