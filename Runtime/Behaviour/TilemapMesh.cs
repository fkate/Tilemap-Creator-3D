// Copyright (c) 2023 Felix Kate. BSD-3 license (see included license file)

using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace TilemapCreator3D {
    public class TilemapMesh : MonoBehaviour, ITilemapModule {    

        [System.Serializable]
        private struct ChunkData : System.IDisposable {
            public GameObject GameObject;
            public Transform Transform;
            public MeshFilter Filter;
            public MeshRenderer Renderer;

            public ChunkData(Transform parent, string name) {
                GameObject = new GameObject(name);

                Transform = GameObject.transform;
                Transform.SetParent(parent);
                Transform.localPosition = Vector3.zero;
                Transform.localRotation = Quaternion.identity;
                Transform.localScale = Vector3.one;

                Filter = GameObject.AddComponent<MeshFilter>();
                Renderer = GameObject.AddComponent<MeshRenderer>();
            }

            public void Dispose() {
                #if UNITY_EDITOR
                DestroyImmediate(Filter.sharedMesh);
                DestroyImmediate(GameObject);
                #else
                Destroy(Filter.sharedMesh);
                Destroy(GameObject);
                #endif
            }
        }

        [Tooltip("Size of chunk in grid units. Zero or smaller results in the max of the dimension.")] public int3 ChunkSize = new int3(0, 0, 0);
        [Tooltip("Include attributes in baking process.")] public RawMeshCombineSettings VertexInfo = new RawMeshCombineSettings { Normal = true, UV0 = true };
        [SerializeField] private ChunkData[] _chunks;
          
        // Some cached values to increase consecutive calls
        private Dictionary<Material, int> _materials;
        private int _layer;
        private bool _isStatic;

        public void BakePartial(Tilemap3D map, Box3D area) {
            TilemapData data = map.Data;

            int3 chunkSize = new int3(ChunkSize.x > 0 ? math.min(ChunkSize.x, data.Width) : data.Width,
                                      ChunkSize.y > 0 ? math.min(ChunkSize.y, data.Height) : data.Height,
                                      ChunkSize.z > 0 ? math.min(ChunkSize.z, data.Depth) : data.Depth);

            int3 chunkCount = new int3((int) math.ceil((float) data.Width / chunkSize.x),
                                       (int) math.ceil((float) data.Height / chunkSize.y),
                                       (int) math.ceil((float) data.Depth / chunkSize.z));

            int chunkLength = chunkCount.x * chunkCount.y * chunkCount.z;
            
            ValidateChunks(chunkLength);
            
            if(_materials == null) _materials = new Dictionary<Material, int>(8);
            _layer = gameObject.layer;
            _isStatic = gameObject.isStatic;

            for(int i = 0; i < chunkLength; i++) {
                int x = i % chunkCount.x;
                int y = (i / chunkCount.x) % chunkCount.y;
                int z = i / (chunkCount.x * chunkCount.y);

                int3 minChunk = new int3(x * chunkSize.x, y * chunkSize.y, z * chunkSize.z);
                int3 maxChunk = math.min(minChunk + chunkSize, data.Size);
                Box3D chunk = new Box3D(minChunk, maxChunk);

                if(chunk.Overlaps(area)) BakeChunk(_chunks[i], chunk, map);
            }
        }               
        
        public void Bake(Tilemap3D map) {
            BakePartial(map, new Box3D(new int3(0, 0, 0), new int3(map.Data.Size)));
        }


        public void Clear() {
            // Nothing to clear
        }


        private void ValidateChunks(int chunkLength) {
            if(_chunks == null ||_chunks.Length != chunkLength) {
                ChunkData[] oldChunks = _chunks == null ? new ChunkData[0] : _chunks;
                int length = oldChunks == null ? chunkLength : math.max(chunkLength, oldChunks.Length);

                Transform trs = transform;

                _chunks = new ChunkData[chunkLength];

                for(int i = 0; i < length; i++) {
                    if(i < chunkLength) {
                        if(oldChunks.Length > i) _chunks[i] = oldChunks[i];
                        else {
                            _chunks[i] = new ChunkData (trs, string.Format("Chunk {0}", i.ToString()));
                        }
                    } else if(i >= chunkLength) oldChunks[i].Dispose();
                }
            } 
        }

        private void BakeChunk(ChunkData chunk, Box3D area, Tilemap3D map) {
            TilemapData data = map.Data;
            float3 gridSize = map.GridSize;

            // Skip if there is nothing to process
            if(area.NoArea) {
                if(chunk.Filter.sharedMesh != null) chunk.Filter.sharedMesh.Clear();
                chunk.GameObject.SetActive(false);
                return;
            }

            _materials.Clear();

            // Put tiles into data array
            NativeList<RawMeshData> meshData = new NativeList<RawMeshData>(area.Width * area.Height * area.Depth, Allocator.TempJob);
            int subMeshIndex = 0;

            for(int z = area.Min.z; z < area.Max.z; z++) {
                for(int y = area.Min.y; y < area.Max.y; y++) {
                    for(int x = area.Min.x; x < area.Max.x; x++) {
                        TilemapData.Tile tile = data[x, y, z];

                        if(tile.id == 0 || tile.id > map.Palette.Count) continue;

                        BaseTile bTile = map.Palette.GetTile(tile.id);
                        TileInfo info = bTile.GetInfo(tile.variant % bTile.Length); // Add variant here

                        if(bTile == null || info.Mesh == null || bTile.Material == null) continue;

                        // Transform
                        float3 position = new float3(x - area.Min.x + 0.5f, y - area.Min.y + 0.5f, z - area.Min.z + 0.5f) * gridSize;
                        Quaternion rotation = tile.GetRotation();
                        float3 scale = 1.0f;

                        // Submesh
                        if(!_materials.ContainsKey(bTile.Material)) _materials.Add(bTile.Material, subMeshIndex++);
                        int subMesh = _materials[bTile.Material];

                        meshData.Add(new RawMeshData(info.Mesh, subMesh, position, rotation, scale));
                    }
                }
            }

            if(meshData.Length > 0) {
                // Sort sub meshes and calculate offsets
                meshData.Sort();

                int vertexOffset = 0, indexOffset = 0, subMeshLength = 0;
                subMeshIndex = 0;

                NativeArray<int2> vertTriOffset = new NativeArray<int2>(meshData.Length, Allocator.TempJob);
                NativeArray<int2> subMeshOffset = new NativeArray<int2>(_materials.Count, Allocator.TempJob);

                for(int i = 0; i < meshData.Length; i++) {
                    vertTriOffset[i] = new int2(vertexOffset, indexOffset);

                    RawMeshData tmd = meshData[i]; 

                    if(tmd.SubMeshIndex != subMeshIndex) {
                        subMeshOffset[subMeshIndex] = new int2(subMeshOffset[subMeshIndex].x, subMeshLength);
                        subMeshOffset[++subMeshIndex] = new int2(indexOffset, 0);

                        subMeshLength = 0;
                    }

                    vertexOffset += tmd.VertexCount;
                    indexOffset += tmd.IndexCount;
                    subMeshLength += tmd.IndexCount;
                }

                subMeshOffset[subMeshIndex] = new int2(subMeshOffset[subMeshIndex].x, subMeshLength);

                // Calculate job
                RawMeshCombineJob job = new RawMeshCombineJob(VertexInfo, meshData, vertTriOffset, vertexOffset, indexOffset);
                JobHandle handle = job.Schedule(meshData.Length, 32);

                handle.Complete();

                Mesh mesh = chunk.Filter.sharedMesh;

                if(mesh == null) {
                    mesh = new Mesh();
                    mesh.name = chunk.GameObject.name;
                    mesh.indexFormat = IndexFormat.UInt32;
                    mesh.MarkDynamic();
                }

                // Get results
                job.Finalize(mesh, subMeshOffset);

                float3 bSize = area.Size * gridSize;
                mesh.bounds = new Bounds(bSize * 0.5f, bSize);

                vertTriOffset.Dispose();
                subMeshOffset.Dispose();

                chunk.Renderer.materials = _materials.Keys.ToArray();
                chunk.Filter.sharedMesh = mesh;
                chunk.Transform.localPosition = area.Min * gridSize;

                chunk.GameObject.layer = _layer;
                chunk.GameObject.isStatic = _isStatic;
                chunk.GameObject.hideFlags = HideFlags.NotEditable;
                chunk.GameObject.SetActive(true);

            } else {
                if(chunk.Filter.sharedMesh != null) chunk.Filter.sharedMesh.Clear();
                chunk.GameObject.SetActive(false);

            }

            meshData.Dispose();
        }

    }

}