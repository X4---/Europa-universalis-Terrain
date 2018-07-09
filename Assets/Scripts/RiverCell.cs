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
    [NonSerialized] public List<Vector4> tangles;
    
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
    
    private Dictionary<Vector3, Vector3> kNoiseVector;

    public void Gen(RiverData data)
    {
        vertices = new List<Vector3>();
        colors = new List<Color>();
        uvs = new List<Vector2>();
        triangles = new List<int>();
        kBoundsPoints = new List<Vector3>();
        tangles = new List<Vector4>();

        verticesOffset = 0;
        beginCount = 0;
        endCount = 0;

        //GenFromOld(data);
        GenFromNew(data);
        //GenFromNewEx(data);

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
        kNoiseVector = new Dictionary<Vector3, Vector3>();

        for (int i =0,iMax = data.kOrigins.Count; i < iMax;++i)
        {
            var river = data.kOrigins[i];
            if ((river.type & RiverData.RiverBoundsType.EndPoint) == 0)
            {
                kCurE = river;
                kPre = null;

                while (kCurE != null && (kCurE.type & RiverData.RiverBoundsType.EndPoint) == 0)
                {
                    GenNoiseVector(kCurE, kPre);
                }
            }
        }


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
        kMesh.SetTangents(tangles);

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
        
        kCur.bHasGend = true;
        var dir = kCur.flowDir;
        if (before == null)// 这是原点
        {
            //if (kCur != null)
            {

                kCur.widthModify = kxModify;

                float centerx = kCur.pos.x + 0.5f;
                float centerz = kCur.pos.z + 0.5f;

                Vector3 center = new Vector3(centerx, 0, centerz);

                var borderleft = BorderLeft(center, kCur.flowDir);
                var borderright = BorderRight(center, kCur.flowDir);

                var downleft = DownLeft(center, kCur.flowDir);
                var downRight = DownRight(center, kCur.flowDir);

                var mid = ModifyVector(LerpPos(downleft, downRight, 0.5f));

                var ldir = RiverData.GetTargetDirsDir(kCur.flowDir, RiverData.Direction.Left);
                var rdir = RiverData.GetTargetDirsDir(kCur.flowDir, RiverData.Direction.Right);

                var l = ModifyVector(LerpPos(borderleft, borderright, 0.5f + 0.5f* kRiverPercent * kCur.widthModify));
                var r = ModifyVector(LerpPos(borderright, borderleft, 0.5f + 0.5f * kRiverPercent * kCur.widthModify));

                AddEightVertice(mid, l, r, mid, Color.white);
            }

        }
        else
        {
            //var dir = kCur.flowDir;

            if(kPre.flowDir == kCur.flowDir)
            {
                kCur.widthModify = Mathf.Clamp(kPre.widthModify + kxModify, 0f, 1.0f);


                float centerx = kCur.pos.x + 0.5f;
                float centerz = kCur.pos.z + 0.5f;

                Vector3 center = new Vector3(centerx, 0, centerz);

                var borderleft = BorderLeft(center, dir);
                var borderright = BorderRight(center, dir);

                var downl = kBoundsPoints[0];
                var downr = kBoundsPoints[1];

                var ldir = RiverData.GetTargetDirsDir(dir, RiverData.Direction.Left);
                var rdir = RiverData.GetTargetDirsDir(dir, RiverData.Direction.Right);

                var l = ModifyVector(LerpPos(borderleft, borderright, 0.5f + 0.5f * kRiverPercent * kCur.widthModify), kCur.diffModify, dir);
                var r = ModifyVector(LerpPos(borderright, borderleft, 0.5f + 0.5f * kRiverPercent * kCur.widthModify), kCur.diffModify, dir);

                AddEightVertice(downl, l, r, downr, Color.white);
            }
            else
            {
                kCur.widthModify = Mathf.Clamp(kPre.widthModify + kxModify, 0f, 1.0f);


                float centerx = kCur.pos.x + 0.5f;
                float centerz = kCur.pos.z + 0.5f;

                Vector3 center = new Vector3(centerx, 0, centerz);

                var borderleft = BorderLeft(center, dir);
                var borderright = BorderRight(center, dir);

                var downl = kBoundsPoints[0];
                var downr = kBoundsPoints[1];
                
                var diff = RiverData.ThisDirisOrisDirsDir(kCur.flowDir, kPre.flowDir);
                
                var l = ModifyVector(LerpPos(borderleft, borderright, 0.5f + 0.5f * kRiverPercent * kCur.widthModify));
                var r = ModifyVector(LerpPos(borderright, borderleft, 0.5f + 0.5f * kRiverPercent * kCur.widthModify));

                AddEightVertice(downl, l, r, downr, Color.white, diff);
            }


            
        }


        kPre = kCurE;
        kCurE = kCur.GetLink(kCur.flowDir);
        
    }

    private void GenNoiseVector(RiverData.Bounds kCur, RiverData.Bounds before)
    {
        kCur.widthModify = 0f;
        if (before != null)
        {
            int count = 0;
            var next = kCur.GetNext();
            
            while(next != null && next.flowDir == kCur.flowDir)
            {
                ++count;
                next = next.GetNext();
            }

            var temp = kCur;

            for(int i =0; i < count; ++i)
            {
                var T = (float)count;
                var onePI = Mathf.PI;
                var angle = (float)onePI * i / T + onePI;
            
                var a = Mathf.Sin(angle);

                temp.diffModify = a;
                temp = temp.GetNext();
            }

            kPre = temp;
            kCurE = temp.GetNext();
        }else
        {
            kPre = kCurE;
            kCurE = kCur.GetLink(kCur.flowDir);
        }
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

                temptarget.x += deltax;
                temptarget.z += deltaz;

                Modifyer[j] = new Vector3(temptarget.x, temptarget.y, temptarget.z);

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


                var dirone = second - one;
                var dirtwo = third - zero;

                zero = ModifyVector(zero);
                one = ModifyVector(one);
                second = ModifyVector(second);
                third = ModifyVector(third);

                if( Vector3.Dot( Vector3.Cross(dirone, dirtwo), Vector3.up) < 0)
                {
                    vertices.Add(one);
                    vertices.Add(zero);
                    vertices.Add(third);
                    vertices.Add(second);
                }
                else
                {
                    vertices.Add(zero);
                    vertices.Add(one);
                    vertices.Add(second);
                    vertices.Add(third);
                }



                

            }
            else
            {
                var zero = target + curupdir * 0.5f;
                var one = target - curupdir * 0.5f;

                var second = next + nextdir * 0.5f;
                var third = next - nextdir * 0.5f;

                zero = ModifyVector(zero);
                one = ModifyVector(one);
                second = ModifyVector(second);
                third = ModifyVector(third);

                var dirone = second - one;
                var dirtwo = third - zero;

                if (Vector3.Dot(Vector3.Cross(dirone, dirtwo), Vector3.up) < 0)
                {
                    vertices.Add(one);
                    vertices.Add(zero);
                    vertices.Add(third);
                    vertices.Add(second);
                }
                else
                {
                    vertices.Add(zero);
                    vertices.Add(one);
                    vertices.Add(second);
                    vertices.Add(third);
                }


              
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

    private void AddFourVertice(Vector3 downl, Vector3 l , Vector3 r, Vector3 downr, Color a)
    {
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

        colors.Add(a);
        colors.Add(a);
        colors.Add(a);
        colors.Add(a);

        uvs.Add(new Vector2(1, 1));
        uvs.Add(new Vector2(1, 0));
        uvs.Add(new Vector2(0, 0));
        uvs.Add(new Vector2(0, 1));

        tangles.Add(new Vector4(1,1,0,0));
        tangles.Add(new Vector4(1,0,0,0));
        tangles.Add(new Vector4(0,0,0,0));
        tangles.Add(new Vector4(0,1,0,0));


        verticesOffset += 4;
    }

    private void AddEightVertice(Vector3 downl, Vector3 l, Vector3 r, Vector3 downr, Color a, RiverData.Direction diff = RiverData.Direction.NotExist)
    {
        kBoundsPoints.Clear();
        kBoundsPoints.Add(l);
        kBoundsPoints.Add(r);

        var l1 = ModifyVector(LerpPos(downl, l, 0.66f));
        var l2 = ModifyVector(LerpPos(downl, l, 0.33f));

        var r1 = ModifyVector(LerpPos(downr, r, 0.66f));
        var r2 = ModifyVector(LerpPos(downr, r, 0.33f));

        if( diff == RiverData.Direction.Left)
        {
            var dir1 = r1 - l1;
            var dir2 = r2 - l2;

            r1 = ModifyVector(l1 + dir1 * 1.4f);
            r2 = ModifyVector(l2 + dir2 * 1.4f);

        }else if (diff == RiverData.Direction.Right)
        {
            var dir1 = l1 - r1;
            var dir2 = l2 - r2;

            l1 = ModifyVector(r1 + dir1 * 1.4f);
            l2 = ModifyVector(r2 + dir2 * 1.4f);
        }

        vertices.Add(downl);
        vertices.Add(l1);
        vertices.Add(l2);
        vertices.Add(l);
        vertices.Add(r);
        vertices.Add(r2);
        vertices.Add(r1);
        vertices.Add(downr);

        triangles.Add(verticesOffset + 0);
        triangles.Add(verticesOffset + 6);
        triangles.Add(verticesOffset + 7);
        triangles.Add(verticesOffset + 0);
        triangles.Add(verticesOffset + 1);
        triangles.Add(verticesOffset + 6);

        triangles.Add(verticesOffset + 1);
        triangles.Add(verticesOffset + 5);
        triangles.Add(verticesOffset + 6);
        triangles.Add(verticesOffset + 1);
        triangles.Add(verticesOffset + 2);
        triangles.Add(verticesOffset + 5);

        triangles.Add(verticesOffset + 2);
        triangles.Add(verticesOffset + 4);
        triangles.Add(verticesOffset + 5);
        triangles.Add(verticesOffset + 2);
        triangles.Add(verticesOffset + 3);
        triangles.Add(verticesOffset + 4);

        colors.Add(a);
        colors.Add(a);
        colors.Add(a);
        colors.Add(a);
        colors.Add(a);
        colors.Add(a);
        colors.Add(a);
        colors.Add(a);


        uvs.Add(new Vector2(1, 1));
        uvs.Add(new Vector2(1, 0.66f));
        uvs.Add(new Vector2(1, 0.33f));
        uvs.Add(new Vector2(1, 0));

        uvs.Add(new Vector2(0, 0));
        uvs.Add(new Vector2(0, 0.33f));
        uvs.Add(new Vector2(0, 0.66f));
        uvs.Add(new Vector2(0, 1));

        tangles.Add(new Vector4(1, 1, 0, 0));
        tangles.Add(new Vector4(1, 0.66f, 0, 0));
        tangles.Add(new Vector4(1, 0.33f, 0, 0));
        tangles.Add(new Vector4(1, 0, 0, 0));

        tangles.Add(new Vector4(0, 0, 0, 0));
        tangles.Add(new Vector4(0, 0.33f, 0, 0));
        tangles.Add(new Vector4(0, 0.66f, 0, 0));
        tangles.Add(new Vector4(0, 1, 0, 0));

        verticesOffset += 8;
    }

    private void AddEightVertice(Vector3 downl, Vector3 l, Vector3 r, Vector3 downr, Color a, float diffcount)
    {
        kBoundsPoints.Clear();
        kBoundsPoints.Add(l);
        kBoundsPoints.Add(r);

        var l1 = ModifyVector(LerpPos(downl, l, 0.66f));
        var l2 = ModifyVector(LerpPos(downl, l, 0.33f));

        var r1 = ModifyVector(LerpPos(downr, r, 0.66f));
        var r2 = ModifyVector(LerpPos(downr, r, 0.33f));
        
        vertices.Add(downl);
        vertices.Add(l1);
        vertices.Add(l2);
        vertices.Add(l);
        vertices.Add(r);
        vertices.Add(r2);
        vertices.Add(r1);
        vertices.Add(downr);

        triangles.Add(verticesOffset + 0);
        triangles.Add(verticesOffset + 6);
        triangles.Add(verticesOffset + 7);
        triangles.Add(verticesOffset + 0);
        triangles.Add(verticesOffset + 1);
        triangles.Add(verticesOffset + 6);

        triangles.Add(verticesOffset + 1);
        triangles.Add(verticesOffset + 5);
        triangles.Add(verticesOffset + 6);
        triangles.Add(verticesOffset + 1);
        triangles.Add(verticesOffset + 2);
        triangles.Add(verticesOffset + 5);

        triangles.Add(verticesOffset + 2);
        triangles.Add(verticesOffset + 4);
        triangles.Add(verticesOffset + 5);
        triangles.Add(verticesOffset + 2);
        triangles.Add(verticesOffset + 3);
        triangles.Add(verticesOffset + 4);

        colors.Add(a);
        colors.Add(a);
        colors.Add(a);
        colors.Add(a);
        colors.Add(a);
        colors.Add(a);
        colors.Add(a);
        colors.Add(a);


        uvs.Add(new Vector2(1, 1));
        uvs.Add(new Vector2(1, 0.66f));
        uvs.Add(new Vector2(1, 0.33f));
        uvs.Add(new Vector2(1, 0));

        uvs.Add(new Vector2(0, 0));
        uvs.Add(new Vector2(0, 0.33f));
        uvs.Add(new Vector2(0, 0.66f));
        uvs.Add(new Vector2(0, 1));

        tangles.Add(new Vector4(1, 1, 0, 0));
        tangles.Add(new Vector4(1, 0.66f, 0, 0));
        tangles.Add(new Vector4(1, 0.33f, 0, 0));
        tangles.Add(new Vector4(1, 0, 0, 0));

        tangles.Add(new Vector4(0, 0, 0, 0));
        tangles.Add(new Vector4(0, 0.33f, 0, 0));
        tangles.Add(new Vector4(0, 0.66f, 0, 0));
        tangles.Add(new Vector4(0, 1, 0, 0));

        verticesOffset += 8;
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

    private Vector3 BorderLeftEx(Vector3 center, RiverData.Direction dir)
    {
        Vector3 result = new Vector3();
        var vector = RiverData.GetDirectionDir(dir);

        var noiseModify = NoiseModify(center);
        //Vector3 noiseDir = new Vector3(noiseModify.x - center.x, 0, noiseModify.z - center.z);
        var noiseDir = (NoiseVector(center)).normalized;

        var direx = (vector + noiseDir).normalized;
        var dirleft = Vector3.Cross(direx, Vector3.up);

        result = center + (direx + dirleft) * 0.5f;

        return result;
    }
    private Vector3 BorderRightEx(Vector3 center, RiverData.Direction dir)
    {
        Vector3 result = new Vector3();
        var vector = RiverData.GetDirectionDir(dir);

        var noiseModify = NoiseModify(center);
        var noiseDir = (NoiseVector(center)).normalized;

        var direx = (vector + noiseDir).normalized;
        var dirleft = Vector3.Cross(direx, Vector3.down);

        result = center + (direx + dirleft) * 0.5f;

        return result;
    }
    private Vector3 DownLeftEx(Vector3 center, RiverData.Direction dir)
    {
        Vector3 result = new Vector3();
        var vector = RiverData.GetDirectionDir(dir);

        var noiseModify = NoiseModify(center);
        var noiseDir = (NoiseVector(center)).normalized;

        var direx = (-vector + noiseDir).normalized;
        var dirleft = Vector3.Cross(direx, Vector3.down);

        result = center + (direx + dirleft) * 0.5f;
        return result;
    }
    private Vector3 DownRightEx(Vector3 center, RiverData.Direction dir)
    {
        Vector3 result = new Vector3();
        var vector = RiverData.GetDirectionDir(dir);
        
        var noiseDir = (NoiseVector(center)).normalized;

        var direx = (-vector + noiseDir).normalized;
        var dirleft = Vector3.Cross(direx, Vector3.up);

        result = center + (direx + dirleft) * 0.5f;
        return result;
    }



    private Vector3 CenterPos(Vector3 leftdown)
    {
        Vector3 result = new Vector3();

        result.x = leftdown.x;// + 0.5f;
        result.y = leftdown.y;
        result.z = leftdown.z;// + 0.5f;

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
        Vector3 result = new Vector3(kOri.x, kOri.y, kOri.z);
        
        int x = ((int)(result.x * 11)) % kNoiseMap.width;
        int z = ((int)(result.z * 4)) % kNoiseMap.height;

        float xx = (kNoiseMap.GetPixel(x, z).r * 2 - 1) * kNoisePercent;
        float zz = (kNoiseMap.GetPixel(z, x).r * 2 - 1) * kNoisePercent;

        result.x += xx;
        result.z += zz;

        return result;
    }

    private Vector3 NoiseVector(Vector3 kOri)
    {
        float xx = (((int)(kOri.x)) % 8 - 4.0f) / 4f;
        float zz = (((int)(kOri.z)) % 8 - 4.0f) / 4f;

        var modify = Vector3.left * xx + Vector3.up * zz;
        
        return modify;

    }
    private Vector3 NoiseModify(Vector3 kOri, float diff, RiverData.Direction oriDir)
    {
        Vector3 result = new Vector3(kOri.x, kOri.y, kOri.z);

        double twoPi = Math.PI * 2f;

        double T = 4.0;

        double a = kOri.x * twoPi / (T);
        double b = kOri.z * twoPi / (T);

        diff *= kNoisePercent;
        //float diff = (float)(Math.Sin(a) + Math.Sin(b)) * kNoisePercent;

        switch(oriDir)
        {
            case RiverData.Direction.Up:
                {
                    result.x += diff;
                }
                break;
            case RiverData.Direction.Down:
                {
                    result.x += diff;
                }
                break;
            case RiverData.Direction.Right:
                {
                    result.z += diff;
                }
                break;
            case RiverData.Direction.Left:
                {
                    result.z += diff;
                }
                break;
        }
        return result;
    }

    private Vector3 HeightSample(Vector3 kOri)
    {
        Vector3 result = kOri;
        result.y = kHeightMap.GetPixel((int)kOri.x, (int)kOri.z).a * ConfigParam.BLOCKHEIGHT + 1f;
        return result;
    }
    private Vector3 ModifyVector(Vector3 kOri)
    {
        return HeightSample(NoiseModify(kOri));
    }

    private Vector3 ModifyVector(Vector3 kOri, float diff, RiverData.Direction oriDir)
    {
        return HeightSample(NoiseModify(kOri, diff, oriDir));
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
