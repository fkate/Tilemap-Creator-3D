// Copyright (c) 2023 Felix Kate. BSD-3 license (see included license file)

using UnityEngine;

namespace TilemapCreator3D {
    [CreateAssetMenu(fileName = "Tile", menuName = "MapTools/Tile", order = 0)]
    public class SingleTile : BaseTile {

        public TileInfo TileInfo;

        public override TileInfo GetInfo(int index) => TileInfo;
        public override int Length => 1;

        public override Mesh GetTilePreview(int index) => TileInfo.Mesh;

    }
}
