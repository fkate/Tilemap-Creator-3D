// Copyright (c) 2023 Felix Kate. BSD-3 license (see included license file)
// Structs that hold the per tile information

using UnityEngine;

namespace TilemapCreator3D {
    [System.Serializable]
    public struct TileInfo {
        public Mesh Mesh;
        public Mesh CollisionMesh;
        public TileCollision Collision;
    }


    [System.Serializable]
    public struct TileInfoMask {
        public TileInfo Info;
        public TileMaskCompound Mask;
    }
}