// Copyright (c) 2023 Felix Kate. BSD-3 license (see included license file)
using Unity.Mathematics;

namespace TilemapCreator3D {

    public struct Box3D {
        public int3 Min { get; }
        public int3 Max { get; }
        public int3 Size { get; }

        public int Width => Size.x;
        public int Height => Size.y;
        public int Depth => Size.z;

        public bool NoArea => Min.Equals(Max);

        public Box3D(int3 min, int3 max) {
            Min = min;
            Max = math.max(min, max);
            Size = max - min;
        }


        // Summary
        //      Expand box by the given amount in all directions
        public Box3D Expand(int amount) {
            return new Box3D(Min - amount, Max + amount);
        }


        // Summary
        //      Clamp boxes size to the min and max point
        public Box3D Clamp(int3 min, int3 max) {
            return new Box3D(math.max(Min, min), math.min(Max, max));
        }


        // Summary
        //      Go through each element of the box and return the position (execution order z-y-x axis)
        public void ForEach(System.Action<int3> action) {
            for(int z = Min.z; z <= Max.z; z++) {
                for(int y = Min.y; y <= Max.y; y++) {
                    for(int x = Min.x; x <= Max.x; x++) {
                        // Measured execution time between action invoke and having the loops is almost the same
                        action.Invoke(new int3(x, y, z));
                    }
                }
            }
        }


        // Summary
        //      Does the box contain the given point
        public bool Contains(int3 point) {
            return point.x >= Min.x && point.y >= Min.y && point.z >= Min.z && point.x <= Max.x && point.y <= Max.y && point.z <= Max.z;
        }


        // Summary
        //      Does the box overlap the given box
        public bool Overlaps(Box3D box) {
            return Min.x <= box.Max.x && Max.x >= box.Min.x && Min.y <= box.Max.y && Max.y >= box.Min.y && Min.z <= box.Max.z && Max.z >= box.Min.z; 
        }


        // Summary
        //      Custom string output for debug purposes
        public override string ToString() {
            return string.Format("Min {0} | Max {1} | Size {2}", Min, Max, Size);
        }
    }


    public enum TileCollision : int {
        None = 0,
        Box = 1,
        BoxExtend = 2,
        MeshBounds = 3,
        MeshConvex = 4,
        MeshComplex = 5
    }


    public enum TileFlags : byte {
        None = 0,
        F0 = 1,
        F1 = 2,
        F2 = 4,
        F3 = 8,
        F4 = 16,
        F5 = 32,
        F6 = 64,
        F7 = 128
    }

}