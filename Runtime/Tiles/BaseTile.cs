// Copyright (c) 2023 Felix Kate. BSD-3 license (see included license file)

using Unity.Mathematics;
using UnityEngine;

namespace TilemapCreator3D {
    public abstract class BaseTile : ScriptableObject {    

        public Material Material;
        public int CollisionLayer;
        public int NavigationArea;
         
        // Necessary inherited fields
        public TileInfo this[int index] => GetInfo(index);
        public abstract int Length { get; }

        // Preview
        public Vector2 PreviewOrientation = new Vector2(20.0f, 20.0f);
        public Quaternion PreviewRotation => Quaternion.Euler(PreviewOrientation.y, PreviewOrientation.x, 0);


        public abstract TileInfo GetInfo(int index);
        public virtual Mesh GetTilePreview(int index) => null;
        public virtual void PostProcessTile(TilemapData data, int3 pos) { }

    }
}