// Copyright (c) 2023 Felix Kate. BSD-3 license (see included license file)

using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace TilemapCreator3D {
    public class Tilemap3D : MonoBehaviour { 

        public TilePalette Palette = new TilePalette();

        public TilemapData Data = new TilemapData(4, 4, 4);
        public float3 GridSize = new float3(1, 1, 1);
        
        // Move data one layer up for easier acsess
        public int Width => Data.Width;
        public int Height => Data.Height;
        public int Depth => Data.Depth;

        public int3 Size => Data.Size;
        public int Length => Data.Length;
        public Box3D Area => new Box3D(0, Size);

        public TilemapData.Tile this[int3 pos] {
            get => Data[pos];
            set => Data[pos] = value;
        }

        // Modules are only cached if needed
        public List<ITilemapModule> Modules {
            get {
                if(_modules == null) RefreshModules();
                return _modules;
            }
        }

        private List<ITilemapModule> _modules;

        public void BakeDynamic() => BakeDynamic(new Box3D(new int3(0, 0, 0), Data.Size - 1));

        public void BakeDynamic(Box3D area) {
            if(_modules == null) RefreshModules();
            
            for(int i = 0; i < _modules.Count; i++) {
                _modules[i].BakePartial(this, area);
            }
        }

        public void Bake() {
            if(_modules == null) RefreshModules();

            for(int i = 0; i < _modules.Count; i++) {
                _modules[i].Bake(this);
            }
        }

        public void RefreshModules() {
            _modules = new List<ITilemapModule>(transform.childCount);
            for(int i = 0; i < transform.childCount; i++) {
                ITilemapModule module = transform.GetChild(i).GetComponent<ITilemapModule>();
                if(module != null) _modules.Add(module);
            }
        }

    }
}