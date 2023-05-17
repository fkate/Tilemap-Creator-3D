// Copyright (c) 2023 Felix Kate. BSD-3 license (see included license file)

using Unity.Mathematics;
using UnityEngine;

namespace TilemapCreator3D {
    [CreateAssetMenu(fileName = "AutoTile", menuName = "MapTools/AutoTile", order = 2)]
    public class AutoTile : BaseTile { 

        public bool EightBitMask;
        public bool NoBorder;
        public bool Orientation2D;
        public bool Isolate;
        public TileInfoMask[] Variants;

        public override TileInfo GetInfo(int index) => index >= 0 && index < Variants.Length ? Variants[index].Info : new TileInfo();
        public override int Length => Variants != null ? Variants.Length : 0;

        public override Mesh GetTilePreview(int index) => GetInfo(0).Mesh;

        public override void PostProcessTile(TilemapData data, int3 pos) {
            TileMask mask = TileMask.None;

            int axis0 = 0;
            int axis1 = Orientation2D ? 1 : 2;

            int3 dir0 = 0, dir1 = 0, size = data.Size;
            dir0[axis0] = 1;
            dir1[axis1] = 1;

            int index = data.GetIndex(pos.x, pos.y, pos.z);
            TilemapData.Tile tile = data[index];

            bool borderL = pos[axis0] - 1 < 0;
            bool borderR = pos[axis0] + 1 >= size[axis0];
            bool borderB = pos[axis1] - 1 < 0;
            bool borderT = pos[axis1] + 1 >= size[axis1];
               
            if(!borderL && CompareTiles(tile, data[pos - dir0])) mask |= TileMask.Left;
            if(!borderR && CompareTiles(tile, data[pos + dir0])) mask |= TileMask.Right;
            if(!borderB && CompareTiles(tile, data[pos - dir1])) mask |= TileMask.Bottom;
            if(!borderT && CompareTiles(tile, data[pos + dir1])) mask |= TileMask.Top;

            if(EightBitMask) {
                if(mask.HasFlag(TileMask.Bottom) && !borderB) {
                    if(mask.HasFlag(TileMask.Left)  && !borderL && CompareTiles(tile, data[pos - dir1 - dir0])) mask |= TileMask.BottomLeft;
                    if(mask.HasFlag(TileMask.Right) && !borderR && CompareTiles(tile, data[pos - dir1 + dir0])) mask |= TileMask.BottomRight;
                }

                if(mask.HasFlag(TileMask.Top) && !borderT) {
                    if(mask.HasFlag(TileMask.Left)  && !borderL && CompareTiles(tile, data[pos + dir1 - dir0])) mask |= TileMask.TopLeft;
                    if(mask.HasFlag(TileMask.Right) && !borderR && CompareTiles(tile, data[pos + dir1 + dir0])) mask |= TileMask.TopRight;
                }    
            }

            // Additional cases if borders count as tiles
            if(NoBorder) {  
                if(borderL) mask |= TileMask.Left;
                if(borderR) mask |= TileMask.Right;
                if(borderB) mask |= TileMask.Bottom;
                if(borderT) mask |= TileMask.Top;

                if(EightBitMask) {
                    if(borderL) {
                        if(mask.HasFlag(TileMask.Bottom)) mask |= TileMask.BottomLeft;
                        if(mask.HasFlag(TileMask.Top)) mask |= TileMask.TopLeft;
                    }

                    if(borderR) {
                        if(mask.HasFlag(TileMask.Bottom)) mask |= TileMask.BottomRight;
                        if(mask.HasFlag(TileMask.Top)) mask |= TileMask.TopRight;
                    }

                    if(borderB) {
                        if(mask.HasFlag(TileMask.Left)) mask |= TileMask.BottomLeft;
                        if(mask.HasFlag(TileMask.Right)) mask |= TileMask.BottomRight;
                    }

                    if(borderT) {
                        if(mask.HasFlag(TileMask.Left)) mask |= TileMask.TopLeft;
                        if(mask.HasFlag(TileMask.Right)) mask |= TileMask.TopRight;
                    }
                }
            }

            for(int i = 0; i < Length; i++) {
                int result = Variants[i].Mask.CompareMask(mask);

                if(result >= 0) {
                    data[index] = new TilemapData.Tile { id = tile.id, variant = (byte) i, rotation = (byte) result, flags = tile.flags };
                    return;
                }
            }

            data[index] = new TilemapData.Tile { id = tile.id, variant = 0, rotation = 0, flags = tile.flags };
        }

        private bool CompareTiles(TilemapData.Tile a, TilemapData.Tile b) {
            bool empty = a.id == 0 || b.id == 0;
            bool equal = a.id == b.id;
            bool flag0 = a.HasFlag(TileFlags.F0) && b.HasFlag(TileFlags.F0);
            bool flag1 = a.HasFlag(TileFlags.F1) && b.HasFlag(TileFlags.F1);

            return !empty && (equal || ((flag0 || flag1) && !Isolate));
        }

    }    
}