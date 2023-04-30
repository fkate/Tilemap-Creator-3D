// Copyright (c) 2023 Felix Kate. BSD-3 license (see included license file)
// Custom type for storing palette data. Does safety checks. Please use methods to add or remove tiles to avoid null entries

using Unity.Mathematics;
using UnityEngine;

namespace TilemapCreator3D {
    [System.Serializable]
    public class TilePalette { 

        private const int LIMIT = 255; // 255 indices for tile bytes - zero index for empty

        [SerializeField] private BaseTile[] _entries; // Cannot easily serialize custom lists so class wraps around the array instead
        [SerializeField] private int _count;

        public BaseTile this [int index] => _entries[index];
        public int Count => _count;


        public TilePalette() {
            _entries = new BaseTile[LIMIT]; // Capacity is also limit of tiles;
        }


        public TilePalette(BaseTile[] entries) : this() {
            int length = math.min(entries.Length, LIMIT);
            for(int i = 0; i < LIMIT; i++) _entries[i] = entries[i];

            _count = length;
        }


        // Summary
        //      Insert tile at position. Fixes map data to match changes. Returns true if rebuild is necessary
        public bool Insert(BaseTile tile, int position, ref TilemapData data) {
            if(position < 0 || position > math.min(Count, LIMIT)) return false;

            // Move all entries up by one
            for(int i = _count; i > position; i--) _entries[i] = _entries[i - 1];
            _entries[position] = tile;            
            _count++;

            int id = position + 1;

            // Fix entries to match the change
            if(position < _count) {
                for(int i = 0; i < data.Length; i++) {
                    TilemapData.Tile tData = data[i];

                    if(tData.id == 0) continue;
                    else if(tData.id >= id) tData.id++;

                    data[i] = tData;
                }

                return true;
            }

            return false;
        }


        // Summary
        //      Replace tile at position. Returns true if rebuild is necessary
        public bool Replace(BaseTile tile, int position) {
            if(position < 0 || position >= _count) return false;

            _entries[position] = tile;

            return true;
        }


        // Summary
        //      Delete index at position. Fixes map data to match changes. Returns true if rebuild is necessary
        public bool Delete(int position, ref TilemapData data) {
            if(position < 0 || position >= _count) return false;

            // Move all entries down by one
            for(int i = position; i < _count - 1; i++) _entries[i] = _entries[i + 1];
            _entries[_count - 1] = null;
            _count--;

            int id = position + 1;

            // Fix entries to match the change
            for(int i = 0; i < data.Length; i++) {
                TilemapData.Tile tData = data[i];
                if(tData.id == 0) continue;
                else if (tData.id == id) tData.id = 0;
                else if(tData.id > id) tData.id--;

                data[i] = tData;
            }

            return true;
        }


        // Get tile at id point. Will ignore zero ids since they represent empty spaces
        public BaseTile GetTile(int id) {
            return (id > 0 && id <= Count) ? _entries[id - 1] : null;
        }

    }
}
