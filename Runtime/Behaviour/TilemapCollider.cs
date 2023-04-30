// Copyright (c) 2023 Felix Kate. BSD-3 license (see included license file)
// Tilemap module to generate an optimized collider layout from the tile data.
// Combines full collision tiles into larger boxes.

using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace TilemapCreator3D {
    public class TilemapCollider : MonoBehaviour, ITilemapModule {

        // Struct for holding temporary information on a box collider
        private struct BoxHint {
            public float x, y, z;
            public float width, height, depth;
            public int layer;

            public float3 min => new float3(x, y, z);
            public float3 max => new float3(x + width - 1, y + height - 1, z + depth - 1);
            public float3 size => new float3(width, height, depth);

            public BoxHint(float x, float y, float z, float width, float height, float depth, int layer) {
                this.x = x;
                this.y = y;
                this.z = z;
                this.width = width;
                this.height = height;
                this.depth = depth;
                this.layer = layer;
            }

            public BoxHint(float3 pos, float3 size, int layer) : this(pos.x, pos.y, pos.z, size.x, size.y, size.z, layer) { }

            public static BoxHint operator * (BoxHint box, float3 value) {
                return new BoxHint(box.min * value, box.size * value, box.layer);
            }
        }


        // Struct for holding temporary information on a mesh tile (cannot hold mesh reference in native collection)
        private struct MeshHint {
            public byte ID;
            public byte Variant;
            public float3 Position;
            public quaternion Rotation;
        }

        
        public void BakePartial(Tilemap3D map, Box3D area) {
            // No dynamic baking allowed
        }


        public void Bake(Tilemap3D map) {
            if(map == null) return;

            // Grab necessary information from tilemap
            TilemapData data = map.Data;
            TilePalette palette = map.Palette;
            int capacity = data.Length;

            NativeArray<int> boxLayers = new NativeArray<int>(capacity, Allocator.Temp);
            NativeList<BoxHint> boxHints = new NativeList<BoxHint>(capacity, Allocator.Temp);
            NativeList<MeshHint> meshHints = new NativeList<MeshHint>(capacity, Allocator.Temp);

            // Get tile information (cannot move to job due to requiring multiple class references)
            for(int i = 0; i < capacity; i++) {
                // Skip condictions (empty tile or invalid reference)
                TilemapData.Tile tile = data[i];
                if(tile.id == 0 || tile.id > palette.Count) continue;

                BaseTile bTile = palette.GetTile(tile.id);
                if(bTile == null) continue;

                TileInfo tileInfo = bTile.GetInfo(tile.variant);

                // Add flag for later box processing
                if(tileInfo.Collision == TileCollision.Box) {
                    boxLayers[i] = bTile.CollisionLayer + 1;

                // Invisible walls that stop if a collider is above them
                } else if (tileInfo.Collision == TileCollision.BoxExtend) {
                    int colLayer = bTile.CollisionLayer + 1;
                    boxLayers[i] = colLayer;

                    int y = (i / data.Width) % data.Height;
                    int maxSteps = data.Height - y;

                    for(int j = 1; j < maxSteps; j++) {
                        int off = i + j * data.Width;
                        if(data[off].id == 0) boxLayers[off] = colLayer;
                        else break;
                    }
                }                

                // Expecting less meshes than boxes so we inverse calculate position
                else if(tileInfo.Collision >= TileCollision.MeshConvex && tileInfo.CollisionMesh != null) {
                    meshHints.Add(new MeshHint() {
                        ID = tile.id,
                        Variant = tile.variant,
                        Position = map.GridToLocal(map.ReconstructPosition(i)),
                        Rotation = tile.GetRotation()
                    });

                // Use mesh bounds to generate box
                } else if(tileInfo.Collision == TileCollision.MeshBounds && tileInfo.Mesh != null) {
                    Bounds bounds = tileInfo.Mesh.bounds;
                    Quaternion rotation = tile.GetRotation();
                    float3 center = rotation * bounds.center;
                    float3 size = rotation * bounds.size;

                    float3 pos = map.GridToLocal(map.ReconstructPosition(i)) + center - size * 0.5f;

                    boxHints.Add(new BoxHint(pos.x, pos.y, pos.z, size.x, size.y, size.z, bTile.CollisionLayer));
                }
            }

            // Expand box from box layer array
            for(int z = 0; z < data.Depth; z++) {
                for(int y = 0; y < data.Height; y++) {
                    for(int x = 0; x < data.Width; x++) {
                        int index = data.GetIndex(x, y, z);
                    
                        if(boxLayers[index] == 0) continue;

                        boxHints.Add(ExpandBox(data, ref boxLayers, x, y, z) * map.GridSize);
                    }
                }
            }

            Clear();
            RebuildShapes(ref boxHints, ref meshHints, map);

            // Clean up
            boxLayers.Dispose();  
            boxHints.Dispose();
            meshHints.Dispose();
        }


        public void Clear() {
            // Clean old objects. Use temporary objects to avoid overlapping with generation
            Transform clearObject = new GameObject("Collider Clear").transform;
            
            Transform selfTrs = transform;

            for(int i = selfTrs.childCount - 1; i >= 0; i--) {
                selfTrs.GetChild(i).SetParent(clearObject);
            }

            #if UNITY_EDITOR
            DestroyImmediate(clearObject.gameObject);
            #else
            Destroy(clearObject.gameObject);
            #endif
        }


        // Remove old data and create box and mesh colliders
        private void RebuildShapes(ref NativeList<BoxHint> boxHints, ref NativeList<MeshHint> meshHints, Tilemap3D map) {
            // Cache transform
            Transform selfTrs = transform;                        
            bool isStatic = gameObject.isStatic;

            // Handle box colliders if available
            if(boxHints.Length > 0) {
                // Create new colliders
                Dictionary<int, GameObject> outputObjects = new Dictionary<int, GameObject>(boxHints.Length);

                // Remove unused colliders
                for(int i = 0; i < boxHints.Length; i++) {
                    BoxHint hint = boxHints[i];

                    GameObject targetObject;
                    if(!outputObjects.TryGetValue(hint.layer, out targetObject)) {
                        targetObject = new GameObject(string.Format("BoxCollider [{0}]", LayerMask.LayerToName(hint.layer)));
                        targetObject.isStatic = isStatic;
                        targetObject.hideFlags = HideFlags.NotEditable;

                        Transform trs = targetObject.transform;
                        trs.SetParent(selfTrs);
                        trs.localPosition = Vector3.zero;
                        trs.localRotation = Quaternion.identity;
                        trs.localScale = Vector3.one;

                        outputObjects.Add(hint.layer, targetObject);
                    }

                    BoxCollider collider = targetObject.gameObject.AddComponent<BoxCollider>();
                    collider.size = math.abs(boxHints[i].size); // Fix negatives
                    collider.center = (boxHints[i].min + (boxHints[i].size * 0.5f));
                    collider.gameObject.layer = boxHints[i].layer;
                }
            }

            // Handle mesh colliders if available
            if(meshHints.Length > 0) {
                // Remove unused colliders
                for(int i = 0; i < meshHints.Length; i++) {
                    GameObject targetObject = new GameObject("Mesh Collider");
                    targetObject.isStatic = isStatic;
                    targetObject.hideFlags = HideFlags.NotEditable;

                    MeshHint hint = meshHints[i];
                    BaseTile bTile = map.Palette.GetTile(hint.ID);
                    TileInfo tileInfo = bTile.GetInfo(hint.Variant);

                    MeshCollider collider = targetObject.AddComponent<MeshCollider>();
                    collider.sharedMesh = tileInfo.CollisionMesh;
                    collider.convex = tileInfo.Collision == TileCollision.MeshConvex;
                    collider.gameObject.layer = bTile.CollisionLayer;

                    Transform trs = targetObject.transform;
                    trs.SetParent(selfTrs);
                    trs.localPosition = hint.Position;
                    trs.localRotation = hint.Rotation;
                    trs.localScale = Vector3.one;
                }       
            }

        }


        // Helper method for combining adjacent boxes into more optimal bounds
        private BoxHint ExpandBox(TilemapData data, ref NativeArray<int> flags, int x, int y, int z) {
            int next;
            int w = 1, h = 1, d = 1;

            int width = data.Width;
            int height = data.Height;
            int depth = data.Depth;
            int wh = width * height;

            // Optimization step to remove uneeded getindex offsets
            int baseIndex = data.GetIndex(x, y, z);
            int baseFlag = flags[baseIndex];

            for(w = 1; (x + w) < width; w++) {
                next = baseIndex + w;
                if(baseFlag != flags[next]) break;
            }

            for(d = 1; (z + d) < depth; d++) {
                for(int wIn = 0; wIn < w; wIn++) {
                    next = baseIndex + d * wh + wIn;
                    if(baseFlag != flags[next]) goto depthBreak;                
                }
            }

            depthBreak:
                
            for(h = 1; (y + h) < height; h++) {
                for(int dIn = 0; dIn < d; dIn++) {
                    for(int wIn = 0; wIn < w; wIn++) {
                        next = baseIndex + dIn * wh + h * width + wIn;
                        if(baseFlag != flags[next]) goto heightBreak;
                    }
                }
            }
        
            heightBreak:

            width = w;
            height = h;
            depth = d;

            // Clean set values
            for(d = 0; d < depth; d++) {
                for(h = 0; h < height; h++) {
                    for(w = 0; w < width; w++) {
                        flags[data.GetIndex(x + w, y + h, z + d)] = 0;
                    }
                }
            }

            return new BoxHint(x, y, z, w, h, d, baseFlag - 1);
        }

    }
}