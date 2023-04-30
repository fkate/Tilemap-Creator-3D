// Copyright (c) 2023 Felix Kate. BSD-3 license (see included license file)

using Unity.Mathematics;
using UnityEngine;

namespace TilemapCreator3D.Samples {
    [CreateAssetMenu(fileName = "PrefabTile", menuName = "MapTools/PrefabTile", order = 10)]
    public class PrefabTile : BaseTile { 

        public GameObject Prefab;
        public Mesh PreviewMesh;

        public override TileInfo GetInfo(int index) => new TileInfo { };
        public override int Length => 1;

        public override Mesh GetTilePreview(int index) => PreviewMesh;

    }    
}