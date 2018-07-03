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

    public float kNoisePercent = 1f;
    public float kRiverPercent = 0.8f;

    public float kxModify = 0.2f;
    public float kzModify = 0.2f;

    public int BoundsCount = 0;

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
    private RiverData.Direction kPreDir = RiverData.Direction.NotExist;

    private int beginCount;
    private int endCount;
    private bool bshowLog = false;

    public void Gen(RiverData data)
    {
        vertices = new List<Vector3>();
        colors = new List<Color>();
        uvs = new List<Vector2>();
        triangles = new List<int>();
        kBoundsPoints = new List<Vector3>();

        verticesOffset = 0;
        beginCount = 0;
        endCount = 0;

        //GenFromOld(data);
        //GenFromNew(data);
        GenFromNewEx(data);

        if (beginCount != endCount)
        {
            bshowLog = true;
            Debug.Log("Begin : " + beginCount + " EndCount : " + endCount);

            Dictionary<Color, int> cs = new Dictionary<Color, int>();

            data.SearchRiver((bounds) =>
            {
                if((bounds.type & RiverData.RiverBoundsType.BeginPoint) !=0
                || (bounds.type & RiverData.RiverBoundsType.EndPoint) != 0)
                {
                    var cor = bounds.cor;

                    int count = 0;
                    if (cs.TryGetValue(cor, out count))
                    {
                        cs[cor] = count + 1;
                    }
                    else
                    {
                        cs.Add(cor, 1);
                    }
                }
            });

            foreach(var coc in cs)
            {
                Debug.Log("color : " + coc.Key + " has count : " + coc.Value);
            }
        }

        
    }

    private void GenFromOld(RiverData data)
    {
        List<RiverData.Bounds> cached = new List<RiverData.Bounds>();
        Dictionary<RiverData.Bounds, int> gened = new Dictionary<RiverData.Bounds, int>();

        cached.Add(data.kOrigin);

        while (cached.Count > 0)
        {
            GenBounds(cached, gened, ref verticesOffset);
        }
    }

    private void GenFromNew(RiverData data)
    {
        for (int i = 0, iMax = data.kOrigins.Count; i < iMax; ++i)
        {

            var river = data.kOrigins[i];
            if ((river.type & RiverData.RiverBoundsType.EndPoint) == 0)
            {
                kCurE = river;
                kPre = null;

                while (kCurE != null && (kCurE.type & RiverData.RiverBoundsType.EndPoint) == 0)
                {
                    GenFromOriginBounds(kCurE, kPre);
                }
            }
        }
    }

    private void GenFromNewEx(RiverData data)
    {
        List<Vector3> centerpoints = new List<Vector3>();

        BoundsCount = 0;

        for (int i =0,iMax = data.kOrigins.Count; i < iMax; ++i)
        {
            var river = data.kOrigins[i];

            if( (river.type & RiverData.RiverBoundsType.BeginPoint) !=0)
            {
                centerpoints.Clear();

                GenCenter(river, centerpoints);
                BoundsCount += centerpoints.Count;
                GenFromCenter(centerpoints);


            }
            

        }





    }

    public void Apply()
    {
        kMesh.SetVertices(vertices);
        kMesh.SetColors(colors);
        kMesh.SetUVs(0, uvs);
        kMesh.SetTriangles(triangles, 0);

        if(bshowLog)
        {
            Debug.Log("vertices cout " + vertices.Count + " " + this.name);
        }
    }

    private void GenFromOriginBounds(RiverData.Bounds kCur, RiverData.Bounds before)
    {
        if(kCur == null)
        {
            Debug.Log("CC");
        }

        Color a = Color.black;

        if (kCur.bHasGend)
        {
            kPre = kCurE;
            kCurE = null;
            Debug.Log("HasGened " + kCur.pos + " before is " + before);

            a = Color.red;
            var index = (int)kCur.widthModify;

            colors[index] = a;
            colors[index + 1] = a;
            colors[index + 2] = a;
            colors[index + 3] = a;


            return;
        }

        kCur.bHasGend = true;

       
        var dir = kCur.flowDir;

        if( kCur.flowDir == RiverData.Direction.NotExist)
        {

            Debug.Log("Not exist " + kCur.pos);
            kCurE = null;

            return;
        }else
        {
            kPreDir = dir;
        }

        colors.Add(a);
        colors.Add(a);
        colors.Add(a);
        colors.Add(a);

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

                var l = ModifyVector(LerpPos(borderleft, borderright, kRiverPercent));
                var r = ModifyVector(LerpPos(borderright, borderleft, kRiverPercent));

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

                kCur.widthModify = verticesOffset;

                verticesOffset += 4;

                



            }

        }
        else
        {
            //var dir = kCur.flowDir;

            float centerx = kCur.pos.x + 0.5f;
            float centerz = kCur.pos.z + 0.5f;

            Vector3 center = new Vector3(centerx, 0, centerz);

            var borderleft = BorderLeft(center, dir);
            var borderright = BorderRight(center, dir);

            var downl = kBoundsPoints[0];
            var downr = kBoundsPoints[1];

            var l = ModifyVector(LerpPos(borderleft, borderright, kRiverPercent));
            var r = ModifyVector(LerpPos(borderright, borderleft, kRiverPercent));

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

            kCur.widthModify = verticesOffset;


            verticesOffset += 4;
        }


        kPre = kCurE;
        kCurE = kCur.GetLink(kCur.flowDir);
        
    }

    private void GenCenter(RiverData.Bounds kCur , List<Vector3> kCenterPoints)
    {
        while(kCur != null && (kCur.type & RiverData.RiverBoundsType.EndPoint) ==0)
        {
            var center = CenterPos(kCur.pos);
            kCenterPoints.Add(center);

            kCur = kCur.GetLink(kCur.flowDir);

        }

    }

    private void GenFromCenter(List<Vector3> kCenterPoints)
    {
        List<Vector3> Modifyer = new List<Vector3>();

        LC_Helper.Loop(kCenterPoints.Count, (i)=>{

            var tar = kCenterPoints[i];

            Modifyer.Add(new Vector3(tar.x, tar.y, tar.z));

        });

        for(int i = 1,iMax = kCenterPoints.Count; i < iMax; ++i)
        {
            var target = kCenterPoints[i];

            var preindex = i - 1;

            var pre = kCenterPoints[i - 1];

            var deltax = target.x - pre.x;
            var deltaz = target.z - pre.z;

            for (int j = preindex; j >= 0; --j)
            {
                deltax *= kxModify;
                deltaz *= kzModify;

                var temptarget = Modifyer[j];

                //temptarget.x += deltax;
                //temptarget.z += deltaz;

                //Modifyer[j] = new Vector3(temptarget.x, temptarget.y, temptarget.z);

            }
        }


        for (int i = 0, iMax = Modifyer.Count-1; i < iMax; ++i)
        {
            var target = Modifyer[i];
            var next = Modifyer[i + 1];

            var curupdir = NorDir(i, Modifyer);
            var nextdir = NorDir(i + 1, Modifyer);

            if( curupdir.magnitude < 0.99 || nextdir.magnitude < 0.99)
            {
                Debug.Log("Error Dir");
            }
            

            if(Vector3.Dot(curupdir, nextdir) < 0)
            {
                var zero = target + curupdir * 0.5f;
                var one = target - curupdir * 0.5f;

                var second = next - nextdir * 0.5f;
                var third = next + nextdir * 0.5f;

                vertices.Add(zero);
                vertices.Add(one);
                vertices.Add(second);
                vertices.Add(third);

            }
            else
            {
                var zero = target + curupdir * 0.5f;
                var one = target - curupdir * 0.5f;

                var second = next + nextdir * 0.5f;
                var third = next - nextdir * 0.5f;

                vertices.Add(zero);
                vertices.Add(one);
                vertices.Add(second);
                vertices.Add(third);
            }

            triangles.Add(verticesOffset + 0);
            triangles.Add(verticesOffset + 2);
            triangles.Add(verticesOffset + 3);
            triangles.Add(verticesOffset + 3);
            triangles.Add(verticesOffset + 1);
            triangles.Add(verticesOffset + 0);

            verticesOffset += 4;


        };

      

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

    private Vector3 CenterPos(Vector3 leftdown)
    {
        Vector3 result = new Vector3();

        result.x = leftdown.x + 0.5f;
        result.y = leftdown.y;
        result.z = leftdown.z + 0.5f;

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
        int x = ((int)(result.x * 11)) % kNoiseMap.width;
        int z = ((int)(result.z * 4)) % kNoiseMap.height;

        float xx = (kNoiseMap.GetPixel(x, z).r * 2 - 1) * kNoisePercent;
        float zz = (kNoiseMap.GetPixel(x, z).r * 2 - 1) * kNoisePercent;
        result.x += xx;
        result.z += zz;

        return result;
    }
    private Vector3 HeightSample(Vector3 kOri)
    {
        Vector3 result = kOri;
        result.y = kHeightMap.GetPixel((int)kOri.x, (int)kOri.z).a * ConfigParam.BLOCKHEIGHT + 0.01f;
        return result;
    }
    private Vector3 ModifyVector(Vector3 kOri)
    {
        return HeightSample(NoiseModify(kOri));
    }

    private Vector3 NorDir(int index, List<Vector3> lists)
    {
        Vector3 result = Vector3.up;
        if(index == 0)
        {
            var cur = lists[index];
            var next = lists[index + 1];
            
            if(cur.x < next.x)
            {
                result = Vector3.forward;

            }else if( cur.x > next.x)
            {
                result = Vector3.back;

            }else if (cur.z < next.z)
            {
                result = Vector3.left;
            }else if( cur.z > next.z)
            {
                result = Vector3.right;
            }

        }else if ( index == lists.Count -1)
        {
            var cur = lists[index-1];
            var next = lists[index];

            if (cur.x < next.x)
            {
                result = Vector3.forward;

            }
            else if (cur.x > next.x)
            {
                result = Vector3.back;

            }
            else if (cur.z < next.z)
            {
                result = Vector3.left;
            }
            else if (cur.z > next.z)
            {
                result = Vector3.right;
            }


        }
        else
        {
            var cur = lists[index];
            var before = lists[index - 1];
            var next = lists[index + 1];

            var mid = LerpPos(before, next, 0.5f);

            result = (mid - cur).normalized;

            if( result.magnitude < 0.99f)
            {
                if (cur.x < next.x)
                {
                    result = Vector3.forward;

                }
                else if (cur.x > next.x)
                {
                    result = Vector3.back;

                }
                else if (cur.z < next.z)
                {
                    result = Vector3.left;
                }
                else if (cur.z > next.z)
                {
                    result = Vector3.right;
                }
            }

        }


        return result;
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

            if((tar.type & RiverData.RiverBoundsType.BeginPoint)!=0)
            {
                ++beginCount;
            }
            if( (tar.type & RiverData.RiverBoundsType.EndPoint) !=0)
            {
                ++endCount;
            }

            //if((tar.type & (RiverData.RiverBoundsType.BeginPoint |
            //  RiverData.RiverBoundsType.EndPoint)) != 0)
            //if ((tar.type & RiverData.RiverBoundsType.BeginPoint) !=0)
            //{
            //    a = tar.cor *  ( 1.0f -  Mathf.Clamp( tar.widthModify , 1f, 50.0f) / 50f)  
            //    a = Color.white;

            //}else if((tar.type & RiverData.RiverBoundsType.EndPoint) != 0)
            //{
            //    a = Color.black;
            //}else
            //{
            //    a = Color.blue;
            //}

            if((tar.type & RiverData.RiverBoundsType.BeginPoint) != 0)
            {
                a = Color.white;
            }else if( (tar.type & RiverData.RiverBoundsType.EndPoint) != 0)
            {
                a = Color.red;
            }else
            {
                a = Color.black;
            }

            //else if( (tar.type & RiverData.RiverBoundsType.Main) != 0)
            //{
            //    a = Color.white * (1.0f - Mathf.Clamp(tar.widthModify, 1f, 50.0f) / 50f);

            //}else
            //{
            //    if( (tar.type & RiverData.RiverBoundsType.In) != 0)
            //    {
            //        a = Color.blue * (1.0f - Mathf.Clamp(tar.widthModify, 1f, 50.0f) / 50f);
            //    }
            //    else
            //    {
            //        a = Color.yellow * (1.0f - Mathf.Clamp(tar.widthModify, 1f, 50.0f) / 50f);
            //    }

                

            //}

            colors.Add(a);
            colors.Add(a);
            colors.Add(a);
            colors.Add(a);

            verticesoffset += 4;
        }
    }

    

}
