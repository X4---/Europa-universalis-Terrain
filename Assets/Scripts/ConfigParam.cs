using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ConfigParam{

    //一个BLOCK 世界坐标下的大小
    public static int BLOCKWIDTH = 10;
    //一个BLOCK 世界坐标下的高度
    public static int BLOCKHEIGHT = 50;

    //一个BLOCK 最大的分辨率
    public static int BLOCKMAXWIDTH = 5632;
    public static int BLOCKMAXHEIGHT = 2048;

    //每一个BLOCK 所占用的贴图的值
    public static int PERBLOCKSIZE = 257;
    //BLOCK的长宽值
    public static int BLOCKWIDTHCOUNT = BLOCKMAXWIDTH / PERBLOCKSIZE;//11
    public static int BLOCKHEIGHTCOUNT = BLOCKMAXHEIGHT / PERBLOCKSIZE;//4

    //每个BLOCK块中面片数量
    public static int PERBLOCKCOUNT = PERBLOCKSIZE-1;
    public static int PERBLOCKCELLCOUNT = PERBLOCKCOUNT * PERBLOCKCOUNT;//256
    public static int PERBLOCKTRIANGLECOUNT = 2 * PERBLOCKCELLCOUNT;//512

    //每个BLOCK 所占位置差
    public static float PERBLOCKNUMBERSIZE = PERBLOCKCOUNT;
    public static float PERBLOCKWORLDSIZE = (float)PERBLOCKSIZE / PERBLOCKCOUNT;//1
    public static int PERBLOCKUVSIZE = PERBLOCKSIZE / PERBLOCKCOUNT ;//1

}
