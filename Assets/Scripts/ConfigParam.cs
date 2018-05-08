using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ConfigParam{

    //每一个BLOCK 修正
    public static int UVModifyer = 2;
    //一个BLOCK 世界坐标下的大小
    public static int BLOCKWIDTH = 10;
    //一个BLOCK 世界坐标下的高度
    public static float BLOCKHEIGHT = 50.0f;

    //一个BLOCK 最大的分辨率
    public static int BLOCKMAXWIDTH = 5632;
    public static int BLOCKMAXHEIGHT = 2048;

    //每一个BLOCK 所占用的贴图的值
    public static int PERBLOCKSIZE = 65;

    

    //每个BLOCK块中面片数量
    public static int PERBLOCKCOUNT = (PERBLOCKSIZE-1);
    public static int PERBLOCKCELLCOUNT = PERBLOCKCOUNT * PERBLOCKCOUNT;//256
    public static int PERBLOCKTRIANGLECOUNT = 2 * PERBLOCKCELLCOUNT;//512

    //每个BLOCK块中面片数量修正
    public static int PERBLOCKCOUNTEX = PERBLOCKCOUNT * UVModifyer;
    public static int PERBLOCKCELLCOUNTEX = PERBLOCKCOUNTEX * PERBLOCKCOUNTEX;
    public static int PERBLOCKTRIANGLECOUNTEX = 2 * PERBLOCKCELLCOUNTEX;

    //每个BLOCK 所占位置差
    public static float PERBLOCKNUMBERSIZE = PERBLOCKSIZE - 1;
    public static float PERBLOCKWORLDSIZE = (float)PERBLOCKCOUNT / PERBLOCKCOUNTEX;//1
    public static float PERBLOCKUVSIZE = (float)PERBLOCKCOUNT / PERBLOCKCOUNTEX;//1

    //BLOCK的长宽值
    public static int BLOCKWIDTHCOUNT = BLOCKMAXWIDTH / PERBLOCKCOUNT;//11
    public static int BLOCKHEIGHTCOUNT = BLOCKMAXHEIGHT / PERBLOCKCOUNT;//4

}
