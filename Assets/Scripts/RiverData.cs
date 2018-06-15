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

    private static Dictionary<Color, int> kCor = new Dictionary<Color, int>();
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

        int count = 0;
        if( kCor.TryGetValue(tarcor, out count))
        {
            kCor[tarcor] = count + 1;

        }else
        {
            kCor.Add(tarcor, 1);
        }

        return true;
    }
    public class Bounds
    {
        public Vector3 pos;
        public Color cor;
        public Bounds left;
        public Bounds right;
        public Bounds up;
        public Bounds down;
        

        public Bounds(Vector3 a)
        {
            pos = a;
        }
        

        public void ExpandDir(Direction Dir, Texture2D cor, bool[][]  gened)
        {
            var x = (int)pos.x;
            var z = (int)pos.z;

            switch(Dir)
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

            if(x>=0 && x < cor.width && z>=0 && z < cor.height)
            {
                var tex = cor.GetPixel(x,z);

                if( gened[x][z] == false && isRiverTex(tex))
                {
                    gened[x][z] = true;
                    var newbounds = new Bounds(new Vector3(x,0,z));
                    LinkBounds(Dir, newbounds).Expand(cor, gened);
                }
            }


        }

        public Bounds LinkBounds(Direction Dir , Bounds newlink)
        {
            switch(Dir)
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

        public void Expand(Texture2D cor, bool[][] gened)
        {
            for(Direction i = Direction.Up; i < Direction.Left; ++i)
            {
                ExpandDir(i, cor, gened);
            }
        }
    }
    
    public enum Direction : int
    {
        Up = 0,
        Right = 1,
        Down = 2,
        Left = 3,
    }

    private Direction ExDir(Direction dir)
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

        return Direction.Up;

    }
    
    public void PointGenRiver(Vector3 point, Texture2D colortex, bool[][] gened)
    {
        Bounds origin = new Bounds(point);
        gened[(int)(point.x)][(int)(point.z)] = true;
        origin.Expand(colortex, gened);
    }

   


}
