// Copyright (c) 2023 Felix Kate. BSD-3 license (see included license file)

using Unity.Mathematics;

namespace TilemapCreator3D {
    public static class TilemapDataUtility {

        // Summary
        //      Copy volume data from source to target volume with offset
        public static void CopyData(this TilemapData target, TilemapData source, int3 pos) {
            pos = math.min(pos, target.Size - 1);
            int3 size = math.min(source.Size, target.Size - pos);

            // Out of range
            if(size.x == 0 || size.y == 0 || size.z == 0) return;

            for(int z = 0; z < size.z; z++) {
                for(int y = 0; y < size.y; y++) {
                    for(int x = 0; x < size.x; x++) {
                        int3 tar = pos + new int3(x, y, z);

                        target[tar.x, tar.y, tar.z] = source[x, y, z];
                    }
                }
            }
        }


        // Summary
        //      Copy volume data with rotation on Y axis
        public static void CopyDataRotated(this TilemapData target, TilemapData source, int3 pos, int rotation) {
            bool axisSwap = rotation % 2 == 1;
            bool invertX = rotation == 1 || rotation == 2;
            bool invertZ = rotation == 3 || rotation == 2;

            int3 sourceSize = axisSwap ? new int3(source.Depth, source.Height, source.Width) : source.Size;

            pos = math.min(pos, target.Size - 1);
            int3 size = math.min(sourceSize, target.Size - pos);

            // Out of range
            if(size.x == 0 || size.y == 0 || size.z == 0) return;

            for(int z = 0; z < size.z; z++) {
                for(int y = 0; y < size.y; y++) {
                    for(int x = 0; x < size.x; x++) {
                        int3 tar = pos + new int3(x, y, z);

                        int3 p = axisSwap ? new int3(z, y, x) : new int3(x, y, z);
                        //if(inverted) p = sourceSize - p - 1;
                        if(invertX) p.x = sourceSize.x - p.x - 1;
                        if(invertZ) p.z = sourceSize.z - p.z - 1;

                        TilemapData.Tile tile = source[p.x, p.y, p.z];
                        tile.rotation = (byte) ((tile.rotation + rotation) % 4);

                        target[tar.x, tar.y, tar.z] = tile;
                    }
                }
            }
        }
    
        
        // Summary
        //      Copy volume data from source to target volume with offset
        public static void ClearArea(this TilemapData data, int3 pos, int3 size) {
            pos = math.min(pos, data.Size - 1);
            size = math.min(size, data.Size - pos);

            // Out of range
            if(size.x == 0 || size.y == 0 || size.z == 0) return;

            for(int z = 0; z < size.z; z++) {
                for(int y = 0; y < size.y; y++) {
                    for(int x = 0; x < size.x; x++) {
                        int3 tar = pos + new int3(x, y, z);

                        data[tar.x, tar.y, tar.z] = new TilemapData.Tile();
                    }
                }
            }
        }


        // Summary
        //      Clear all data
        public static void Clear(this TilemapData data) {
            for(int i = 0; i < data.Length; i++) data[i] = new TilemapData.Tile();
        }    

    }
}