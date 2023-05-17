// Copyright (c) 2023 Felix Kate. BSD-3 license (see included license file)
// Additional mask to use on tilemap prefabs

using UnityEngine;

namespace TilemapCreator3D.Samples {
    [RequireComponent(typeof(Tilemap3D))]
    public class SimpleDungeonRoom : MonoBehaviour {
        public TileMaskCompound RoomMask;

        public TilemapData Data => GetComponent<Tilemap3D>().Data;

    }
}

