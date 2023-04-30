// Copyright (c) 2023 Felix Kate. BSD-3 license (see included license file)
// Example of a custom module to add prefab tiles to the tile map. Warning: This might not be the most performant approach and is meant as an example only.

using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace TilemapCreator3D.Samples {
    public class TilemapPrefabModule : MonoBehaviour, ITilemapModule {

        private const int BASE_CAPACITY = 64;

        // Use dictionary for faster lookup
        private Dictionary<int3, GameObject> _cachedObjects;

        public void Bake(Tilemap3D map) {
            // Nothing static to bake because we want immediate visual feedback
        }

        public void BakePartial(Tilemap3D map, Box3D area) {
            Transform trs = transform;

            // Cache preexisting gameobjects if necessary (since dictionaries cannot be serialized)
            if(_cachedObjects == null) {
                _cachedObjects = new Dictionary<int3, GameObject>(math.max(trs.childCount, BASE_CAPACITY));
                for(int i = 0; i < trs.childCount; i++) {
                    Transform childTrs = trs.GetChild(i);
                    int3 pos = (int3) math.floor((float3) childTrs.localPosition);

                    _cachedObjects.Add(pos, childTrs.gameObject);
                }
            }

            TilemapData data = map.Data;
            TilePalette palette = map.Palette;
            
            area.ForEach((int3 pos) => {
                TilemapData.Tile tile = data[pos];
                BaseTile bTile = palette.GetTile(tile.id);
                PrefabTile pTile = bTile as PrefabTile;

                // Search for existing object in dictionary
                GameObject existingInstance = null, newInstance = null;
                _cachedObjects.TryGetValue(pos, out existingInstance);

                // Only process if tile is valid
                if(pTile != null && pTile.Prefab != null) {
                    // If we are updating in editor create prefab instance
                    #if UNITY_EDITOR
                    newInstance = (GameObject) UnityEditor.PrefabUtility.InstantiatePrefab(pTile.Prefab, transform);
                    #else
                    newInstance = Instantiate(pTile.Prefab, transform);
                    #endif

                    // Always correct transform due to possible changes in rotation
                    Transform instTrs = newInstance.transform;
                    instTrs.localPosition = map.GridToLocal(pos);
                    instTrs.localRotation = tile.GetRotation();
                    instTrs.localScale = Vector3.one;
                }

                // Remove existing if no longer needed
                if(existingInstance != null) {
                    #if UNITY_EDITOR
                    DestroyImmediate(existingInstance);
                    #else
                    Destroy(existingInstance);
                    #endif
                }

                if(newInstance != null) {
                    if(_cachedObjects.ContainsKey(pos)) _cachedObjects[pos] = newInstance;
                    else _cachedObjects.Add(pos, newInstance);
                } else {
                    if(_cachedObjects.ContainsKey(pos)) _cachedObjects.Remove(pos);
                }
            });
        }

        public void Clear() {
            // Nothing to clear
        }

    }
}
