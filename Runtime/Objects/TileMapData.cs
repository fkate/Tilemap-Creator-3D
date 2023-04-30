// Copyright (c) 2023 Felix Kate. BSD-3 license (see included license file)
// The class that holds the tilemap data. Data is stores as int32 array for serialization purposes.

using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine;

namespace TilemapCreator3D {
    [System.Serializable]
    public struct TilemapData {

        [System.Serializable, StructLayout(LayoutKind.Sequential, Size=4)]
        public struct Tile {
            public byte id;
            public byte variant;
            public byte rotation;
            public byte flags;

            public TileFlags GetFlags() => (TileFlags) flags;
            public bool HasFlag(TileFlags flag) => GetFlags().HasFlag(flag);
            public quaternion GetRotation() => quaternion.AxisAngle(new float3(0, 1, 0), (rotation % 4) * 1.57079632679f); // Half PI for 90deg

            // These two implicit calls are introduced so that we can save the tiles inside integers instead since large arrays of structs cause huge slowdowns in Unitys serialization process
            public static implicit operator int(Tile value) => (value.id << 0) | (value.variant << 8) | (value.rotation << 16) | (value.flags << 24);
            public static explicit operator Tile(int value) => new Tile { id = (byte) (value >> 0), variant = (byte) (value >> 8), rotation = (byte) (value >> 16), flags = (byte) (value >> 24) };

        }
    
        [SerializeField] private int _width;
        [SerializeField] private int _height;
        [SerializeField] private int _depth;

        [SerializeField] private int[] _data;

        public int Width => _width;
        public int Height => _height;
        public int Depth => _depth;

        public int3 Size => new int3(_width, _height, _depth);
        public int Length => _data.Length;

        public Tile this[int x, int y, int z] {
            get => (Tile) _data[GetIndex(x, y, z)];
            set => _data[GetIndex(x, y, z)] = value;
        }

        public Tile this[int3 pos] {
            get => (Tile) _data[GetIndex(pos.x, pos.y, pos.z)];
            set => _data[GetIndex(pos.x, pos.y, pos.z)] = value;
        }

        public Tile this[int index] {
            get => (Tile) _data[index];
            set => _data[index] = value;
        }

        public TilemapData(int width, int height, int depth) {
            _width = width;
            _height = height;
            _depth = depth;

            _data = new int[_width * _height * _depth];
        }
    
        public int GetIndex(int x, int y, int z) => z * _width * _height + y * _width + x;

        public bool InRange(int x, int y, int z) => x >= 0 && x < _width && y >= 0 && y < _height && z >= 0 && z < _depth;
        public bool InRange(int index) => index >= 0 && index < Length;

        // Summary
        //      Rescale the data volume
        public void Resize(int width, int height, int depth) {
            if(_width == width && _height == height && _depth == depth) return;
                     
            // Precalculate copy step limit
            int copyWidth = math.min(width, _width);
            int copyHeight = math.min(height, _height);
            int copyDepth = math.min(depth, _depth);

            // Cache old values
            int oldWidth = _width;
            int oldHeight = _height;
        
            int oldwh = oldWidth * oldHeight;

            // Create new volume to fit copy
            int[] newData = new int[width * height * depth];
        
            int wh = width * height;

            // Copy data
            for(int z = 0; z < copyDepth; z++) {
                for(int y = 0; y < copyHeight; y++) {
                    for(int x = 0; x < copyWidth; x++) {
                        newData[z * wh + y * width + x] = _data[z * oldwh + y * oldWidth + x];
                    }
                }
            }

            this = new TilemapData { _width = width, _height = height, _depth = depth, _data = newData };
        }

    }
}