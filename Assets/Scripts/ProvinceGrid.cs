using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

[ExecuteInEditMode]
public class ProvinceGrid : MonoBehaviour {

    public Texture2D kProvinceMap;
    public RenderTexture kProvinceColorMap;

    public int kProvinceCount;
    public Material kLut;

    private Dictionary<Color, int> kProvinceColorToIDMap = new Dictionary<Color, int>();

    private ProvinceData[] kProvinceData;

    private Texture2D kProvincePolitimap;

    public enum Type
    {

    }

    private void OnEnable()
    {
        //searDefine();
    }

    void Awake()
    {
        search();
    }

    private void search()
    {
        searDefine();
        Dictionary<Color, int> result = new Dictionary<Color, int>();


        if(kProvinceMap ==null)
        {
            return;
        }

        var width = kProvinceMap.width;
        var height = kProvinceMap.height;

        LC_Helper.Loop(width, (i) =>
       {
           LC_Helper.Loop(height, (j) =>
          {
              var col = kProvinceMap.GetPixel(i, j);

              var count = 0;
              if(result.TryGetValue(col,out count))
              {
                  result[col] = count + 1;

              }else
              {
                  result.Add(col, 1);
              }

          });

       });
        
        kProvinceCount = result.Count;
        kProvinceData = new ProvinceData[ConfigParam.MAXProvinceCount];


        int flag = 0;
        foreach(var colcount in result)
        {
            var col = colcount.Key;
            kProvinceColorToIDMap.TryGetValue(col, out flag);
            kProvinceData[flag] = new ProvinceData(flag, col);
        }

        kProvincePolitimap = new Texture2D(ConfigParam.BLOCKMAXWIDTH, ConfigParam.BLOCKMAXHEIGHT);
        Graphics.ConvertTexture(kProvinceMap, kProvincePolitimap);

        //DebugProvinceCol();
        ChangeProvinceColor();
    }

    private void searDefine()
    {
        var path = Application.dataPath;
        var searpath = Path.Combine(path, "Map/definition.csv");

        kProvinceColorToIDMap.Clear();
        if (File.Exists(searpath))
        {
            var data = OpenCSV(searpath);

            var provincetoken = data.FindToken("province");
            var redtoken = data.FindToken("red");
            var greentoken = data.FindToken("green");
            var bluetoken = data.FindToken("blue");

            int tempint = 0;
            LC_Helper.Loop(data.data.Count, (i) =>
            {
                var pid = data.FindDataAtIndexWithToken(i, provincetoken);
                var red = data.FindDataAtIndexWithToken(i, redtoken);
                var green = data.FindDataAtIndexWithToken(i, greentoken);
                var blue = data.FindDataAtIndexWithToken(i, bluetoken);

                int.TryParse(red, out tempint);
                var tempr = (float)tempint / 255f;
                int.TryParse(green, out tempint);
                var tempg = (float)tempint / 255f;
                int.TryParse(blue, out tempint);
                var tempb = (float)tempint / 255f;

                int.TryParse(pid, out tempint);

                Color nColor = new Color(tempr, tempg, tempb, 1);

                var id = -1;
                if(kProvinceColorToIDMap.TryGetValue( nColor, out id))
                {
                    //Debug.Log("has id " + id);
                }else
                {
                    kProvinceColorToIDMap.Add(nColor, tempint);
                }


            });
            


        }
    }

    public int GetProvinceID( int x, int z)
    {
        x = x % kProvinceMap.width;
        z = z % kProvinceMap.height;

        var col = kProvinceMap.GetPixel(x, z);
        int ID = -1;

        if( kProvinceColorToIDMap.TryGetValue(col, out ID))
        {

        }

        return ID;
    }

    public int GetProvinceID( Color col)
    {
        int ID = -1;
        kProvinceColorToIDMap.TryGetValue(col, out ID);
        return ID;
    }

    public ProvinceData GetProvinceData( int x, int z)
    {
        return GetProvinceData(GetProvinceID(x, z));
    }

    public ProvinceData GetProvinceData( int ID)
    {
        if( ID <0 || ID >= kProvinceData.Length)
        {
            return null;
        }
        return kProvinceData[ID];

    }

    public ProvinceData GetProvinceData( Color col)
    {
        return GetProvinceData(GetProvinceID(col));
    }

    public void ChangeProvinceColor()
    {
        var width = kProvinceMap.width;
        var height = kProvinceMap.height;

        LC_Helper.Loop(width, (i) =>
        {
            LC_Helper.Loop(height, (j) =>
            {
                var col = kProvinceMap.GetPixel(i, j);
                var pdata = GetProvinceData(col);

                if(pdata != null)
                {
                    kProvincePolitimap.SetPixel(i, j, pdata.kShowColor);
                }

            });

        });

        kProvincePolitimap.Apply();

        Graphics.Blit(kProvincePolitimap, kProvinceColorMap);
    }

    public void ChangeTargetProvinceColor(Color kBaseColor, Color kTargeColor)
    {
        kLut.SetColor("_BaseColor", kBaseColor);
        kLut.SetColor("_TarColor", kTargeColor);
        kLut.SetTexture("_MainTex", kProvinceColorMap);

        Graphics.Blit(kProvinceColorMap, kProvinceColorMap, kLut);
    }

    public void DebugProvinceCol()
    {
        LC_Helper.Loop(kProvinceData.Length, (i) =>
        {
            var data = kProvinceData[i];

            if(data.ID %2 == 0)
            {
                data.kShowColor = Color.white;
                data.kShowColor.a = 1;
            }
            else
            {
                data.kShowColor = Color.black;
                data.kShowColor.a = 0;

            }



        });

    }

    public void RayTest()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Debug.DrawRay(ray.origin, ray.direction, Color.red);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, int.MaxValue, 1 << LayerMask.NameToLayer("Terrain")))
        {
            Debug.Log("检测到物体" + hit.point);
            //Pickat(hit.point);
            PickEx(hit.point);
        }


    }

    public void Update()
    {
        if (Input.GetMouseButtonUp(0))
        {
            RayTest();
        }
    }

    public void Pickat(Vector3 pos)
    {
        Debug.Log("选择到省份" + GetProvinceID((int)pos.x, (int)pos.z));

        var Pdata = GetProvinceData((int)pos.x, (int)pos.z);
        if(Pdata != null)
        {
            if(Pdata.kShowColor.a > 0.5)
            {
                Pdata.kShowColor = Color.black;
                Pdata.kShowColor.a = 0;
            }
            else
            {
                Pdata.kShowColor = Color.white;
                Pdata.kShowColor.a = 1;
            }

            ChangeProvinceColor();
        }
    }

    public void PickEx(Vector3 pos)
    {
        Debug.Log("选择到省份" + GetProvinceID((int)pos.x, (int)pos.z));

        var Pdata = GetProvinceData((int)pos.x, (int)pos.z);
        if (Pdata != null)
        {
            var col = kProvinceMap.GetPixel((int)pos.x, (int)pos.z);
            var tarcol = Color.white;

            if (Pdata.kShowColor.a > 0.5)
            {
                tarcol = Color.black;
                tarcol.a = 0;
            }
            else
            {
                tarcol = Color.white;
                tarcol.a = 1;
            }

            Pdata.kShowColor = tarcol;
            ChangeTargetProvinceColor(col, tarcol);
        }
    }


    public class CSVCache
    {
        public string[] head;
        public List<string[]> data = new List<string[]>();

        public string FindDataAtIndex( int index, string kTar)
        {
            int flag = -1;
            for(int i=0,iMax = head[i].Length; i < iMax; ++i)
            {
                if( head[i] == kTar)
                {
                    flag = i;
                    break;
                }
            }

            if( index >=0 && index < data.Count)
            {
                var pars = data[index];
                if(flag >=0 && flag < pars.Length)
                {
                    return pars[flag];
                }
            }
            return null;
        }

        public int FindToken(string kTar)
        {
            int flag = -1;
            for (int i = 0, iMax = head[i].Length; i < iMax; ++i)
            {
                if (head[i] == kTar)
                {
                    flag = i;
                    break;
                }
            }

            return flag;

        }

        public string FindDataAtIndexWithToken( int index, int flag)
        {
            if (index >= 0 && index < data.Count)
            {
                var pars = data[index];
                if (flag >= 0 && flag < pars.Length)
                {
                    return pars[flag];
                }
            }
            return null;
        }
    }

    public static CSVCache OpenCSV(string filePath)//从csv读取数据返回table
    {
        CSVCache result = new CSVCache();

        System.Text.Encoding encoding = GetType(filePath); //Encoding.ASCII;//
        System.IO.FileStream fs = new System.IO.FileStream(filePath, System.IO.FileMode.Open,
            System.IO.FileAccess.Read);

        System.IO.StreamReader sr = new System.IO.StreamReader(fs, encoding);

        //记录每次读取的一行记录
        string strLine = "";
        //记录每行记录中的各字段内容
        string[] aryLine = null;
        string[] tableHead = null;
        //标示列数
        int columnCount = 0;
        //标示是否是读取的第一行
        bool IsFirst = true;
        //逐行读取CSV中的数据
        while ((strLine = sr.ReadLine()) != null)
        {
            if (IsFirst == true)
            {
                tableHead = strLine.Split(';');
                result.head = tableHead;
                IsFirst = false;
            }
            else
            {
                aryLine = strLine.Split(';');
                result.data.Add(aryLine);

            }
        }
      
        sr.Close();
        fs.Close();

        return result;
    }

    public static System.Text.Encoding GetType(string FILE_NAME)
    {
        System.IO.FileStream fs = new System.IO.FileStream(FILE_NAME, System.IO.FileMode.Open,
            System.IO.FileAccess.Read);
        System.Text.Encoding r = GetType(fs);
        fs.Close();
        return r;
    }

    /// 通过给定的文件流，判断文件的编码类型
    /// <param name="fs">文件流</param>
    /// <returns>文件的编码类型</returns>
    public static System.Text.Encoding GetType(System.IO.FileStream fs)
    {
        byte[] Unicode = new byte[] { 0xFF, 0xFE, 0x41 };
        byte[] UnicodeBIG = new byte[] { 0xFE, 0xFF, 0x00 };
        byte[] UTF8 = new byte[] { 0xEF, 0xBB, 0xBF }; //带BOM
        System.Text.Encoding reVal = System.Text.Encoding.Default;

        System.IO.BinaryReader r = new System.IO.BinaryReader(fs, System.Text.Encoding.Default);
        int i;
        int.TryParse(fs.Length.ToString(), out i);
        byte[] ss = r.ReadBytes(i);
        if (IsUTF8Bytes(ss) || (ss[0] == 0xEF && ss[1] == 0xBB && ss[2] == 0xBF))
        {
            reVal = System.Text.Encoding.UTF8;
        }
        else if (ss[0] == 0xFE && ss[1] == 0xFF && ss[2] == 0x00)
        {
            reVal = System.Text.Encoding.BigEndianUnicode;
        }
        else if (ss[0] == 0xFF && ss[1] == 0xFE && ss[2] == 0x41)
        {
            reVal = System.Text.Encoding.Unicode;
        }
        r.Close();
        return reVal;
    }
    private static bool IsUTF8Bytes(byte[] data)
    {
        int charByteCounter = 1;  //计算当前正分析的字符应还有的字节数
        byte curByte; //当前分析的字节.
        for (int i = 0; i < data.Length; i++)
        {
            curByte = data[i];
            if (charByteCounter == 1)
            {
                if (curByte >= 0x80)
                {
                    //判断当前
                    while (((curByte <<= 1) & 0x80) != 0)
                    {
                        charByteCounter++;
                    }
                    //标记位首位若为非0 则至少以2个1开始 如:110XXXXX...........1111110X　
                    if (charByteCounter == 1 || charByteCounter > 6)
                    {
                        return false;
                    }
                }
            }
            else
            {
                //若是UTF-8 此时第一位必须为1
                if ((curByte & 0xC0) != 0x80)
                {
                    return false;
                }
                charByteCounter--;
            }
        }
        return true;
    }
}
