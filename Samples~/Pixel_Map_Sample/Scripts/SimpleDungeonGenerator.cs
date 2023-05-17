// Copyright (c) 2023 Felix Kate. BSD-3 license (see included license file)
// Example code for generating a simple dungeon from room prefabs

using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace TilemapCreator3D.Samples {
    [RequireComponent(typeof(Tilemap3D))]
    public class SimpleDungeonGenerator : MonoBehaviour {

        public uint Seed;
        public int RoomSize = 12;
        public int2 Size = new int2(10, 10);

        [Tooltip("Room prefabs should match the size value")]
        public SimpleDungeonRoom[] Rooms;

        private Tilemap3D _tilemap;


        public void Generate() {
            if(_tilemap == null) _tilemap = GetComponent<Tilemap3D>();

            NativeArray<TileMask> roomMask = new NativeArray<TileMask>(Size.x * Size.y, Allocator.Temp);
            NativeList<int2> roomPicker = new NativeList<int2>(Rooms.Length, Allocator.Temp);
            
            Unity.Mathematics.Random random = new Unity.Mathematics.Random(Seed);
            random.NextDouble(); // First random to avoid predictable result (will be in first row otherwise)

            int2 startPoint = Size / 2;

            // Create mask staring in the center
            Spread(startPoint, roomMask, ref random);

            _tilemap.Data.Clear();
            _tilemap.Data.Resize(RoomSize * Size.x, _tilemap.Height, RoomSize * Size.y);

            for(int y = 0; y < Size.y; y++) {                
                for(int x = 0; x < Size.x; x++) {
                    TileMask mask = roomMask[MaskIndex(x, y)];

                    int roomID, rotation;

                    // Find random matching room
                    if(ChooseRoom(mask, roomPicker, ref random, out roomID, out rotation)) {
                        _tilemap.Data.CopyDataRotated(Rooms[roomID].Data, new int3(x * RoomSize, 0, y * RoomSize), rotation);
                    }
                }
            }

            // Dispose arrays to prevent leak
            roomMask.Dispose();
            roomPicker.Dispose();

            // Rebake mesh
            _tilemap.PostProcessTiles(_tilemap.Area);
            _tilemap.BakeDynamic();
        }


        private bool ChooseRoom(TileMask mask, NativeList<int2> roomPicker, ref Unity.Mathematics.Random random, out int roomID, out int rotation) {
            roomPicker.Clear();

            roomID = -1;
            rotation = -1;

            // Search for all valid rooms
            for(int i = 0; i < Rooms.Length; i++) {
                if(Rooms[i] == null) continue;

                int result = (Rooms[i].RoomMask.CompareMask(mask));
                if(result >= 0) {
                    roomPicker.Add(new int2(i, result));
                }
            }

            if(roomPicker.Length == 0) return false;

            int2 target = roomPicker[random.NextInt(0, roomPicker.Length)];
            roomID = target.x;
            rotation = target.y;

            return true;                  
        }


        private void Spread(int2 pos, NativeArray<TileMask> mask, ref Unity.Mathematics.Random random) {
            // Left
            if(pos.x > 0 && random.NextBool() && mask[MaskIndex(pos.x - 1, pos.y)] == 0) {
                mask[MaskIndex(pos.x, pos.y)] |= TileMask.Left;
                mask[MaskIndex(pos.x - 1, pos.y)] |= TileMask.Right;
                Spread(new int2(pos.x - 1, pos.y), mask, ref random);
            }
            // Right
            if(pos.x < Size.x - 1 && random.NextBool() && mask[MaskIndex(pos.x + 1, pos.y)] == 0) {
                mask[MaskIndex(pos.x, pos.y)] |= TileMask.Right;
                mask[MaskIndex(pos.x + 1, pos.y)] |= TileMask.Left;
                Spread(new int2(pos.x + 1, pos.y), mask, ref random);
            }
            // Bottom
            if(pos.y > 0 && random.NextBool() && mask[MaskIndex(pos.x, pos.y - 1)] == 0) {
                mask[MaskIndex(pos.x, pos.y)] |= TileMask.Bottom;
                mask[MaskIndex(pos.x, pos.y - 1)] |= TileMask.Top;
                Spread(new int2(pos.x, pos.y - 1), mask, ref random);
            }
            // Top
            if(pos.y < Size.y - 1 && random.NextBool() && mask[MaskIndex(pos.x, pos.y + 1)] == 0) {
                mask[MaskIndex(pos.x, pos.y)] |= TileMask.Top;
                mask[MaskIndex(pos.x, pos.y + 1)] |= TileMask.Bottom;
                Spread(new int2(pos.x, pos.y + 1), mask, ref random);
            }
        }


        private int MaskIndex(int x, int y) => y * Size.x + x; 


    }
}

