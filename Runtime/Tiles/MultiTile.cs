// Copyright (c) 2023 Felix Kate. BSD-3 license (see included license file)

using UnityEngine;

namespace TilemapCreator3D {
    [CreateAssetMenu(fileName = "MultiTile", menuName = "MapTools/MultiTile", order = 1)]
    public class MultiTile : BaseTile {

        public TileInfo[] Variants;

        public override TileInfo GetInfo(int index) => index >= 0 && index < Variants.Length ? Variants[index] : new TileInfo();
        public override int Length => Variants != null ? Variants.Length : 0;

        public override Mesh GetTilePreview(int index) => GetInfo(index).Mesh;

    }
}