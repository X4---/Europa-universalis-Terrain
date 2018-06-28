using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class ObjectPool<T> where T : class, new()
{
    private Stack<T> m_objectStack = new Stack<T>();
    public T New()
    {
        return (m_objectStack.Count == 0) ? new T() : m_objectStack.Pop();
    }

    public void Store(T t)
    {
        m_objectStack.Push(t);
    }
}

public class RiverData{

    public delegate void RiverSearchDelegate(Bounds kbounds);

    public delegate void RiverSearchTDelegate(Bounds kbounds, Bounds before);

    public static Dictionary<Color, int> kCor = new Dictionary<Color, int>();

    public static void SearMap(Color tarcor)
    {
        int count = 0;
        if (kCor.TryGetValue(tarcor, out count))
        {
            kCor[tarcor] = count + 1;

        }
        else
        {
            kCor.Add(tarcor, 1);
        }
    }

    public static bool isRiverTex(Color tarcor)
    {
        if (tarcor == Color.white)
        {
            return false;
        }
        else if (tarcor.r > 0.47 && tarcor.r < 0.5 && tarcor.g > 0.47 && tarcor.g < 0.5)
        {
            return false;
        }

        
        return true;
    }

    public static bool isBeginorEndColor(Color tarcor)
    {
        if(tarcor == Color.red) //红分支
        {
            return true;
        }else if( tarcor == Color.green) //绿 七点
        {
            return true;
        }else if( tarcor.r > 0.95 && tarcor.g > 0.95 && tarcor.b <0.01) //黄 
        {
            return true;
        }

        return false;
    }

    public static bool isBeginColor(Color tarcor)
    {
        if (tarcor.r > 0.95 && tarcor.g > 0.95 && tarcor.b < 0.01) //黄 
        {
            return true;
        }
        else if (tarcor == Color.green) //绿 七点
        {
            return true;
        }
        return false;
    }

    public static bool isEndColor(Color tarcor)
    {
        if (tarcor == Color.red) //red
        {
            return true;
        }
        return false;
    }

    public static bool isOriColor(Color tarcor)
    {
        if(tarcor == Color.green)
        {
            return true;
        }

        return false;
    }

    public static bool isOutColor(Color tarcor)
    {
        if (tarcor.r > 0.95 && tarcor.g > 0.95 && tarcor.b < 0.01) //黄 
        {
            return true;
        }

        return false;
    }

    public static Dictionary<Vector3, Bounds> kGenRecord = new Dictionary<Vector3, Bounds>();

    public static Bounds GetPosBounds(Vector3 tar, Color cor)
    {
        Bounds result = null;
        if (!kGenRecord.TryGetValue(tar,out result))
        {
            result = new Bounds(tar, cor);
        }
        
        return result;
    }
    public enum RiverBoundsType :int
    {
        Uninit = 0,
        Marked = 1,
        
        In = 1<<1,
        Out = 1<<2,

        Main = 1<<3,
        Branch = 1<<4,

        BeginPoint = 1<<5,
        EndPoint = 1<<6

    }

    public class Bounds
    {
        public Vector3 pos;
        public Color cor;
        public Bounds left;
        public Bounds right;
        public Bounds up;
        public Bounds down;
        public RiverBoundsType type;

        public Direction flowDir = Direction.NotExist;
        public float widthModify;

        public bool bHasGend = false;


        public Bounds(Vector3 a, Color col)
        {
            pos = a;
            cor = col;
            bHasGend = false;
            widthModify = 0.0f;
        }

        public void ExpandDir(Direction Dir, Texture2D cor, bool[][] gened)
        {
            var x = (int)pos.x;
            var z = (int)pos.z;

            switch (Dir)
            {
                case Direction.Down:
                    z -= 1;
                    break;
                case Direction.Up:
                    z += 1;
                    break;
                case Direction.Left:
                    x -= 1;
                    break;
                case Direction.Right:
                    x += 1;
                    break;
            }

            if (x >= 0 && x < cor.width && z >= 0 && z < cor.height)
            {
                var tex = cor.GetPixel(x, z);
                if (gened[x][z] == false && isRiverTex(tex))
                {
                    gened[x][z] = true;

                    var pos = new Vector3(x, 0, z);
                    var newbounds = GetPosBounds(pos, tex);
                    LinkBounds(Dir, newbounds).Expand(cor, gened);
                }
            }


        }

        public Bounds LinkBounds(Direction Dir, Bounds newlink)
        {
            switch (Dir)
            {
                case Direction.Up:
                    up = newlink;
                    newlink.down = this;
                    break;
                case Direction.Right:
                    right = newlink;
                    newlink.left = this;
                    break;
                case Direction.Down:
                    down = newlink;
                    newlink.up = this;
                    break;
                case Direction.Left:
                    left = newlink;
                    newlink.right = this;
                    break;
            }

            return newlink;
        }

        public void Link(Direction Dir)
        {
            var x = (int)pos.x;
            var z = (int)pos.z;

            switch (Dir)
            {
                case Direction.Down:
                    z -= 1;
                    break;
                case Direction.Up:
                    z += 1;
                    break;
                case Direction.Left:
                    x -= 1;
                    break;
                case Direction.Right:
                    x += 1;
                    break;
            }

            Vector3 newpos = new Vector3(x, 0, z);
            Bounds find = null;

            if( kGenRecord.TryGetValue(newpos, out find))
            {
                LinkBounds(Dir, find);
            }
        }

        public void Expand(Texture2D cor, bool[][] gened)
        {
            for (Direction dir = Direction.Begin; dir <= Direction.End; ++dir)
            {
                ExpandDir(dir, cor, gened);
            }
        }

        public Bounds GetLink(Direction dir)
        {
            Bounds result = null;
            switch (dir)
            {
                case Direction.Up:
                    result = up;
                    break;
                case Direction.Right:
                    result = right;
                    break;
                case Direction.Down:
                    result = down;
                    break;
                case Direction.Left:
                    result = left;
                    break;
            }
            return result;
        }

        public bool IsBeginOrEnd()
        {
            var col = cor;
            int linkcount = 0;
            for (Direction dir = Direction.Begin; dir <= Direction.End; ++dir)
            {
                var link = GetLink(dir);
                if (link != null)
                {
                    ++linkcount;
                }
            }

            //是边界的条件 是边界颜色 或者 linkcount <=1
            if (isBeginorEndColor(col) || linkcount <= 1)
            {
                return true;
            }

            return false;

        }

        public float GenWidthModify()
        {
            if(widthModify > 0f)
            {
                return widthModify;
            }

            if ( (type & RiverBoundsType.Main) != 0)
            {
                widthModify = 1.0f;
                //是主干
                for (Direction dir = Direction.Begin; dir <= Direction.End; ++dir)
                {
                    var l = GetLink(dir);
                    if(l != null && l.flowDir == ExDir(dir))
                    {
                        if((l.type & RiverBoundsType.Main) != 0)
                        {
                            var result = l.GenWidthModify() + 1.0f;
                            widthModify = result;
                            return result;

                        }
                    }

                }
            }else
            {
                widthModify = 1.0f;
                for (Direction dir = Direction.Begin; dir <= Direction.End; ++dir)
                {
                    var l = GetLink(dir);
                    if (l != null && l.flowDir == ExDir(dir))
                    {
                        if ((l.type & RiverBoundsType.Branch) != 0)
                        {
                            var result = Math.Max( l.GenWidthModify(), widthModify) + 1.0f;
                            widthModify = result;
                            

                        }
                    }

                }


            }

            return widthModify;
        }

        public int LinkCount()
        {
            int linkCount = 0;

            for(Direction dir = Direction.Begin; dir <= Direction.End; ++dir)
            {
                var next = GetLink(dir);
                if(next !=null)
                {
                    ++linkCount;
                }
            }

            return linkCount;
        }
        
    }


    public enum Direction : int
    {
        NotExist = -1,
        Up = 0,
        Right = 1,
        Down = 2,
        Left = 3,
        Begin = Up,
        End = Left,
    }
    
    public Bounds kOrigin;

    public List<Bounds> kOrigins = new List<Bounds>();

    public static Direction ExDir(Direction dir)
    {
        //Direction res = Direction.Right;

        switch(dir)
        {
            case Direction.Up:
                return Direction.Down;
                break;
            case Direction.Right:
                return Direction.Left;
                break;
            case Direction.Down:
                return Direction.Up;
                break;
            case Direction.Left:
                return Direction.Right;
                break;
        }

        return Direction.NotExist;

    }
    
    public void PointGenRiver(Vector3 point, Texture2D colortex, bool[][] gened, int count)
    {
        var cor = colortex.GetPixel((int)point.x, (int)point.z);

        Bounds origin = GetPosBounds(point, cor);
        gened[(int)(point.x)][(int)(point.z)] = true;
        origin.Expand(colortex, gened);

        kOrigin = origin;
        GenType( count);
    }


    private Dictionary<Bounds, int> kRecord = new Dictionary<Bounds, int>();
    public void GenType(int index)
    {
        bool bhasori = false;
        kRecord.Clear();

        SearchRiver((bounds) =>
        {
            for(Direction dir = Direction.Begin; dir <= Direction.End; ++dir)
            {
                bounds.Link(dir);
            }
        });

        //1stPass Find IsOriginOrEnd 
        SearchRiver((bounds) =>
        {
            bounds.type |= RiverBoundsType.Marked;
            bounds.flowDir = Direction.NotExist;

            if (bounds.IsBeginOrEnd())
            {
                kOrigins.Add(bounds);
                bounds.type |= RiverBoundsType.BeginPoint;
                bounds.type &= ~RiverBoundsType.EndPoint;

                if (isOriColor(bounds.cor))
                {
                    bounds.type |= RiverBoundsType.BeginPoint;
                    bounds.type &= ~RiverBoundsType.EndPoint;

                    bhasori = true;
                    //将原点修正到这个点
                    kOrigin = bounds;
                }
            }
        });

        
        if(!bhasori)
        {
            Dictionary<Color, int> temp = new Dictionary<Color, int>();

            Debug.Log("No Ori Found "+ index);
            SearchRiver((Bounds) =>
            {
                if(temp.ContainsKey(Bounds.cor))
                {
                    temp[Bounds.cor] += 1;
                }else
                {
                    temp.Add(Bounds.cor, 1);
                }

            });

            foreach(var corcount in temp)
            {
                Debug.Log(string.Format("corlor {0} has count {1}", corcount.Key, corcount.Value));
            }


            
        }

        //2ndPass Find BranchOrMain 从原点出发 判断点是主干还是分支
        SearchRiver((bounds, beofre) =>
        {
            if (beofre == null)
            {
                bounds.type |= RiverBoundsType.Main;
                bounds.type &= ~RiverBoundsType.Branch;
            }
            else
            {
                //如果本身的颜色是边界的颜色那么就是分支的节点
                if(isBeginorEndColor(bounds.cor))
                {
                    bounds.type &= ~RiverBoundsType.Main;
                    bounds.type |= RiverBoundsType.Branch;
                }else
                { 
                    //否则这个节点类型就和延伸它的节点的类型一致
                    bounds.type |= (beofre.type & (RiverBoundsType.Main | RiverBoundsType.Branch));
                }
            }
        });
        
        //3rdPass Find In out
        SearchRiver((bounds, before) =>
        {
            //主干In
            if((bounds.type & RiverBoundsType.Main) != 0)
            {
                bounds.type |= RiverBoundsType.In;
                bounds.type &= ~RiverBoundsType.Out;
            }else
            {
                //是边界点
                if (isBeginorEndColor(bounds.cor))
                {
                    if (isOutColor(bounds.cor))
                    {
                        bounds.type |= RiverBoundsType.Out;
                        bounds.type &= ~RiverBoundsType.In;
                    }
                    else
                    {
                        bounds.type |= RiverBoundsType.In;
                        bounds.type &= ~RiverBoundsType.Out;
                    }
                }
                else
                {
                    //不是边界点
                    bounds.type |= (before.type & (RiverBoundsType.In | RiverBoundsType.Out));

                }
            }
        });

        for(int i=0,iMax = kOrigins.Count; i < iMax; ++i)
        {
            var cur = kOrigins[i];

            //Main
            if( (cur.type & RiverBoundsType.Main) != 0)
            {
                if( isOriColor(cur.cor))
                {
                    cur.type |= RiverBoundsType.BeginPoint;
                    cur.type &= ~RiverBoundsType.EndPoint;

                }else
                {
                    cur.type |= RiverBoundsType.EndPoint;
                    cur.type &= ~RiverBoundsType.BeginPoint;
                }

            }else//branch
            {
                if( isBeginColor(cur.cor))
                {
                    cur.type |= RiverBoundsType.BeginPoint;
                    cur.type &= ~RiverBoundsType.EndPoint;
                }
                else if (isEndColor(cur.cor))
                {
                    cur.type |= RiverBoundsType.EndPoint;
                    cur.type &= ~RiverBoundsType.BeginPoint;
                }else
                {
                    if( (cur.type & RiverBoundsType.Out) !=0)
                    {
                        cur.type |= RiverBoundsType.EndPoint;
                        cur.type &= ~RiverBoundsType.BeginPoint;
                    }else
                    {
                        cur.type |= RiverBoundsType.BeginPoint;
                        cur.type &= ~RiverBoundsType.EndPoint;
                    }

                    
                }
            }



        }


        SearchRiver((bounds) =>
        {
            bounds.GenWidthModify();
        });

        int begincount = 0;
        int EndCount = 0;

        SearchRiver((bounds) =>
        {
            var type = bounds.type;
            var bBegin = (type & RiverBoundsType.BeginPoint) != 0;
            var bEnd = (type & RiverBoundsType.EndPoint) != 0;

            var bMain = (type & RiverBoundsType.Main) != 0;
            var bBranch = (type & RiverBoundsType.Branch) != 0;

            var bIn = (type & RiverBoundsType.In) != 0;
            var bOut = (type & RiverBoundsType.Out) != 0;

            if(bBegin)
            {
                ++begincount;
            }
            if(bEnd)
            {
                ++EndCount;
            }

            if((bBegin ==true) && (bEnd == true))
            {
                Debug.Log("Begin == End " + index);
            }

            if(bMain == bBranch)
            {
                Debug.Log("Main == Branch " + index);
            }

            if(bIn == bOut)
            {
                Debug.Log("In == Out " + index);
            }


        });

        if(begincount != EndCount)
        {
            Debug.Log("[s : " + begincount + "] [e :" + EndCount + "] index : " + index);
        }


        for (int i=0,iMax = kOrigins.Count; i < iMax; ++i)
        {
            var ori = kOrigins[i];
            if((ori.type & RiverBoundsType.BeginPoint) != 0)
            {
                var cur = ori;
                Direction predir = Direction.NotExist;
                Bounds preb = null;

                //如果当前值不是End节点
                while ( (cur !=null) && (cur.type & RiverBoundsType.EndPoint) == 0)
                {
                    bool bStart = (cur.type & RiverBoundsType.BeginPoint) != 0;

                    if(bStart && cur != ori)
                    {
                        Debug.Log("Find break");
                        break;
                    }
                    
                    var curbMain = (cur.type & RiverBoundsType.Main) != 0;
                    var curbBranch = (cur.type & RiverBoundsType.Branch) != 0;

                    List<Bounds> temp = new List<Bounds>();
                    List<Direction> tempdir = new List<Direction>();

                    for (Direction dir = Direction.Begin; dir <= Direction.End; ++dir)
                    {
                        var next = cur.GetLink(dir);
                        if(next != null && next != preb)
                        {
                            var nextbMain = (next.type & RiverBoundsType.Main) != 0;
                            var nextbBranch = (next.type & RiverBoundsType.Branch) != 0;

                            bool bEnd = (next.type & RiverBoundsType.EndPoint) != 0;
                            bool bBegin = (next.type & RiverBoundsType.BeginPoint) != 0;

                            //if(!bBegin & !bEnd)
                            if ((curbMain == nextbMain) && (curbBranch == nextbBranch) && !bEnd && !bBegin)
                            {
                                temp.Add(next);
                                tempdir.Add(dir);
                            }
                        }
                    }

                    if(temp.Count > 0)
                    {
                        if (temp.Count > 1)
                        {
                            int tempindex = -1;
                            int min = int.MaxValue;

                            for (int j = 0, jMax = temp.Count; j < jMax; ++j)
                            {
                                var a = temp[j];
                                var linkcount = a.LinkCount();

                                if (linkcount < min)
                                {
                                    tempindex = j;
                                    min = linkcount;
                                }

                            }

                            predir = tempdir[tempindex];
                            cur.flowDir = predir;
                            preb = cur;

                            cur = temp[tempindex];


                        }
                        else if (temp.Count == 1)
                        {

                            int tempindex = 0;

                            predir = tempdir[tempindex];
                            cur.flowDir = predir;
                            preb = cur;

                            cur = temp[tempindex];
                        }
                    }
                    else
                    {
                        cur.flowDir = predir;
                        cur = null;
                    }



                }
            }
        }
    }

    public void SearchRiver(RiverSearchDelegate call)
    {
        kRecord.Clear();
        for (Direction dir = Direction.Begin; dir <= Direction.End; ++dir)
        {
            SearchRiver(call, dir, kOrigin);
        }

    }

    public void SearchRiver(RiverSearchDelegate call, Direction kDir, Bounds b)
    {
        if(b !=null)
        {
            if(!kRecord.ContainsKey(b))
            {
                call(b);
                kRecord.Add(b, 1);
            }

            for (Direction dir = Direction.Begin; dir <= Direction.End; ++dir)
            {
                if(kDir != ExDir(dir))
                {
                    Bounds next = b.GetLink(dir);
                    SearchRiver(call, dir, next);
                }
            }
        }
    }

    public  void SearchRiver(RiverSearchTDelegate call)
    {
        kRecord.Clear();
        for (Direction dir = Direction.Begin; dir <= Direction.End; ++dir)
        {
            SearchRiver(call, dir, kOrigin, null);
        }
    }

    public void SearchRiver(RiverSearchTDelegate call, Direction kDir, Bounds cur, Bounds before)
    {
        if (cur != null)
        {
            if (!kRecord.ContainsKey(cur))
            {
                call(cur, before);
                kRecord.Add(cur, 1);
            }

            for (Direction dir = Direction.Begin; dir <= Direction.End; ++dir)
            {
                if (kDir != ExDir(dir))
                {
                    Bounds next = cur.GetLink(dir);
                    if(next != null)
                    {
                        SearchRiver(call, dir, next, cur);
                    }
                }
            }
        }
    }

    
}
