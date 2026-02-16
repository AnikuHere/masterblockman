using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using UnityEngine;

namespace MOBA
{
    //地图类型
    public enum MapType
    {
        NONE,
        NavMeshMap,   //NavMesh
        Cell,                          //格子
    };

    //格子阻挡类型
    public enum CellBlockType
    {
        //TODO 有优先级不能改动顺序
        NONE,
        StaticBlock = 0x01,
        DyncActorStop_Block = 0x02,
        DyncActorMoving_Block= 0x04,
    }

    //Actor格子阻挡类型
    public enum CellActorBlockType
    {
        NONE = 0,
        Stop_Block,
        Moving_Block,
    }

    public struct MapCellInfo
    {
        public byte block;  //是否静态阻挡 1是
        public CellActorBlockType actor_block;  //移动单位引起的动态障碍
        public CellActorBlockType hero_block;
        public byte hero_blockRef;   //英雄阻挡引用

        public void ReadBin(BinaryReader br)
        {
            block = br.ReadByte();
            actor_block = 0;
            hero_block = 0;
            hero_blockRef = 0;
        }

        public void writeBin(BinaryWriter bw)
        {
            bw.Write(block);
        }
    };

    //地图格式 朝向坐标系(+x ,+z)
    public class MapBaseInfo
    {
        ///版本号
        public int Version = 0;
        /// 地图ID
        public int MapID = 0;

        //地图左下角点X坐标
        public int LeftBottomPointX = 0;
        //地图左下角Z坐标
        public int LeftBottomPointZ = 0;

        /// 格子的行数
        public int RowNum = 0;
        /// 格子的列数
        public int ColumnNum = 0;
        //格子大小
        public int CellSize = 0;

        //格子信息
        public MapCellInfo[] cellInfo;

        public void ReadBin(string mapFile)
        {
            System.Object obj = Resources.Load(mapFile);
            TextAsset val = (TextAsset)(object)((obj is TextAsset) ? obj : null);
            if ((System.Object)(object)val == (System.Object)null)
            {
                Debug.LogError((object)("Resources Load ReadBin()加载地图文件出错 file:" + mapFile));
                return;
            }
            byte[] bytes = val.bytes;
            TList<byte> tList = new TList<byte>();
            using (MemoryStream input = new MemoryStream(bytes))
            {
                System.IO.BinaryReader binaryReader = new System.IO.BinaryReader(input);
                if (binaryReader.BaseStream.Position < binaryReader.BaseStream.Length)
                {
                    Version = binaryReader.ReadInt32();
                    MapID = binaryReader.ReadInt32();
                    LeftBottomPointX = binaryReader.ReadInt32();
                    LeftBottomPointZ = binaryReader.ReadInt32();
                    RowNum = binaryReader.ReadInt32();
                    ColumnNum = binaryReader.ReadInt32();
                    CellSize = binaryReader.ReadInt32();
                    int num = RowNum * ColumnNum;
                    cellInfo = new MapCellInfo[num];
                    for (int i = 0; i < num; i++)
                    {
                        cellInfo[i].ReadBin(binaryReader);
                    }
                }
                binaryReader.Close();
            }
            tList.Release();
            tList = null;
        }

        public void WriteBin(string mapFile)
        {
            FileStream fileStream = new FileStream(mapFile, FileMode.CreateNew);
            BinaryWriter binaryWriter = new BinaryWriter(fileStream);
            binaryWriter.Write(Version);
            binaryWriter.Write(MapID);
            binaryWriter.Write(LeftBottomPointX);
            binaryWriter.Write(LeftBottomPointZ);
            binaryWriter.Write(RowNum);
            binaryWriter.Write(ColumnNum);
            binaryWriter.Write(CellSize);
            int num = RowNum * ColumnNum;
            for (int i = 0; i < num; i++)
            {
                cellInfo[i].writeBin(binaryWriter);
            }
            binaryWriter.Close();
            fileStream.Close();
        }

    };

}
