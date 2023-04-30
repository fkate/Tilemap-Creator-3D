// Copyright (c) 2023 Felix Kate. BSD-3 license (see included license file)
// Script to show an example of a very simple procedural map generation algorithm.
// Only meant as a showcase so values are hardcoded for the sample

using Unity.Mathematics;
using UnityEngine;

namespace TilemapCreator3D.Samples {
    [RequireComponent(typeof(Tilemap3D))]
    public class MapRandomizer : MonoBehaviour {

        // IDs of the tiles used in the generation
        [SerializeField] private byte IDWater = 1;
        [SerializeField] private byte IDPlain = 2;
        [SerializeField] private byte IDForest = 4;
        [SerializeField] private byte IDMountain = 6;
        [SerializeField] private byte IDBuildingA = 7;
        [SerializeField] private byte IDBuildingB = 8;

        private Tilemap3D _tilemap;

        public void Start() {
            _tilemap = GetComponent<Tilemap3D>();
        }

        public void Update() {
            if(Input.GetKeyDown(KeyCode.R)) Rebuild();
        }

        private void Rebuild() {
            _tilemap.Data.Clear();

            Unity.Mathematics.Random random = new Unity.Mathematics.Random((uint) ((Time.timeSinceLevelLoad * 133) % 6973));

            // Prepare randoms for perlins
            float2 offsetFloor = random.NextFloat2() * 379321;
            float2 offsetForest = random.NextFloat2() * 7913321;
            float2 offsetMountain = random.NextFloat2() * 56731;

            for(int x = 0; x < _tilemap.Width; x++) {
                for(int z = 0; z < _tilemap.Depth; z++) {
                    float noiseFloor = Perlin(x + offsetFloor.x, z + offsetFloor.y);
                    float noiseForest = Perlin(x + offsetForest.x, z + offsetForest.y, 0.2f);
                    float noiseMountain = Perlin(x + offsetMountain.x, z + offsetMountain.y, 0.4f);

                    byte id = IDWater;

                    if(noiseFloor >= 0.5) id = IDPlain; // 1 Water 2 Plain
                    byte variant = (byte) (random.NextInt() % _tilemap.Palette.GetTile(id).Length);

                    _tilemap[new int3(x, 0, z)] = new TilemapData.Tile{ id = id, variant = variant };
                    
                    // Upper layer
                    if(id == IDPlain) {
                        id = 0;

                        if(noiseForest > 0.5f) id = IDForest;
                        if(noiseMountain > 0.75f) id = IDMountain;

                        float building = random.NextFloat() % 1;
                        if(building > 0.985f) id = IDBuildingA;
                        else if (building > 0.96f) id = IDBuildingB;

                        _tilemap[new int3(x, 1, z)] = new TilemapData.Tile{ id = id };
                    } 
                }
            }

            // Update map
            _tilemap.PostProcessTiles(_tilemap.Area);
            _tilemap.BakeDynamic();
        }

        private float Perlin(float x, float y, float scale = 0.1f) {
            x *= scale;
            y *= scale;
                        
            return Mathf.PerlinNoise(x, y);
        }

    }
}
