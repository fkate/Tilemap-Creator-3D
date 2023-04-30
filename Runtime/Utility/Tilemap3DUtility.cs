// Copyright (c) 2023 Felix Kate. BSD-3 license (see included license file)

using Unity.Mathematics;
using UnityEngine;

namespace TilemapCreator3D {
    public static class Tilemap3DUtility {

        // Summary
        //      Calculate a local space plane on the given axis (0x-1y-2z) for the given layer
        public static Plane GetLayerPlane(int axis, int layer) {
            float3 up = 0;
            up[axis] = 1;
            
            return new Plane(up, -layer - 0.001f);
        }


        // Summary
        //      Raycast a local space plane. Input ray is in worldspace and will be transformed into grid space
        public static bool RaycastLayer(this Tilemap3D map, Ray ray, Plane plane, out int3 hit) {
            Transform trs = map.transform;

            hit = new int3(0, 0, 0);

            int3 size = map.Data.Size;

            float3 gridSize = map.GridSize;        

            // Local ray
            ray = new Ray(trs.InverseTransformPoint(ray.origin) / gridSize, trs.InverseTransformDirection(ray.direction) / gridSize);
            float hitDistance = 0;

            if(plane.Raycast(ray, out hitDistance)) {
                // Get hit and scale to local grid size
                float3 hitPoint = math.floor(ray.GetPoint(hitDistance));

                hit = new int3((int) hitPoint.x, (int) hitPoint.y, (int) hitPoint.z);
   
                // Allow out of bounds for better multi select experience but give a negative and clamp result
                bool inBounds = hit.x >= 0 && hit.y >= 0 && hit.z >= 0 && hit.x < size.x && hit.y < size.y && hit.z < size.z;
                if(!inBounds) hit = new int3(math.clamp(hit.x, 0, size.x - 1), math.clamp(hit.y, 0, size.y - 1), math.clamp(hit.z, 0, size.z - 1));
            
                return inBounds;
            };


            return false;
        }


        // Summary
        //      Returns the map position at the given pivot (0-1 range) in world coordinates
        public static float3 GridToWorld(this Tilemap3D map, int3 position, float3 pivot) {
            return map.transform.TransformPoint((position + pivot) * map.GridSize);
        }


         // Summary
        //      Returns the map position in world coordinates
        public static float3 GridToWorld(this Tilemap3D map, int3 position) {
            return GridToWorld(map, position, new float3(0.5f, 0.5f, 0.5f));
        }


        // Summary
        //      Returns the map position at the given pivot (0-1 range) in local coordinates
        public static float3 GridToLocal(this Tilemap3D map, int3 position, float3 pivot) {
            return ((float3) position + pivot) * map.GridSize;
        }


        // Summary
        //      Returns the map position in local coordinates
        public static float3 GridToLocal(this Tilemap3D map, int3 position) {
            return GridToLocal(map, position, new float3(0.5f, 0.5f, 0.5f));
        }


        // Summary
        //      Returns the map grid position at given world position (this includes out of bounds)
        public static int3 WorldToGrid(this Tilemap3D map, float3 position) {
            position = map.transform.InverseTransformPoint(position);
            return (int3) math.floor(position / map.GridSize);
        }

        
        // Summary
        //      Converts index into xyz coordinate
        public static int3 ReconstructPosition(this Tilemap3D map, int index) {
            return new int3(index % map.Width, (index / map.Width) % map.Height, index / (map.Width * map.Height));
        }


        // Summary
        //      Checks if the given grid coordinates are in bounds
        public static bool InBounds(this Tilemap3D map, int3 position) {
            return position.x >= 0 && position.y >= 0 && position.z >= 0 && position.x < map.Data.Width && position.y < map.Data.Height && position.z < map.Data.Depth;
        }


        // Summary
        //      Post processes area and it's neighbours. Returns the area containing it's neighbours
        public static Box3D PostProcessTiles(this Tilemap3D map, Box3D area) {
            byte baseID = map.Data[area.Min.x, area.Min.y, area.Min.z].id;

            // Spread out box by one
            area = area.Expand(1).Clamp(0, map.Data.Size - 1);

            area.ForEach((int3 pos) => {
                byte id = map.Data[pos.x, pos.y, pos.z].id;

                if(id != 0) {
                    BaseTile tile = map.Palette.GetTile(id);
                    if(tile != null) tile.PostProcessTile(map.Data, pos);
                }
            });

            return area;
        }

    }
}