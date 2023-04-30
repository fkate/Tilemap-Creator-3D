// Copyright (c) 2023 Felix Kate. BSD-3 license (see included license file)

using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AI;

namespace TilemapCreator3D {
    public class TilemapNavigator : MonoBehaviour, ITilemapModule {

        [System.Serializable]
        public struct NavGenerationSettings {
            public int MinArea;
            public float VoxelSize;
            public int TileSize;
            public uint MaxJobWorkers;
        }
        
        [System.Serializable]
        public struct NavLink {
            public int Area;
            public int3 Start;
            public int3 End;
            public bool TwoDirection;
        }
       

        public int AgentPreset;
        public NavGenerationSettings GenerationSettings =
            new NavGenerationSettings { MinArea = 0, VoxelSize = 0.1f, TileSize = 128, MaxJobWorkers = 0 };

        public NavLink[] NavLinks;

        [SerializeField] private NavMeshData _navMeshData;     
        [SerializeField] private float3 _gridSize = new float3(1, 1, 1);

        private NavMeshLinkData[] _navLinkData;
        private NavMeshDataInstance _navMeshInstance;
        private NavMeshLinkInstance[] _navMeshLinkInstances;

        #if UNITY_EDITOR
        public float3 GridSize => _gridSize;
        #endif


        private void OnEnable() {
            AddData();
        }

        private void OnDisable() {
            RemoveData();
        }

        public void BakePartial(Tilemap3D map, Box3D area) {
            // No dynamic baking allowed
        }

        public void Bake(Tilemap3D map) {
            if(map == null) return;

            TilemapData rawData = map.Data;
            _gridSize = map.GridSize;
            
            List<NavMeshBuildSource> buildSources = new List<NavMeshBuildSource>(rawData.Length);

            Transform trs = transform;
            Matrix4x4 localToWorld = trs.localToWorldMatrix;

            for(int z = 0; z < rawData.Depth; z++) {
                for(int y = 0; y < rawData.Height; y++) {
                    for(int x = 0; x < rawData.Width; x++) {
                        TilemapData.Tile tile = rawData[x, y, z];
            
                        if(tile.id == 0) continue;
                        BaseTile bTile = map.Palette.GetTile(tile.id);
                        TileInfo info = bTile.GetInfo(tile.variant);
                        
                        if(info.Collision == TileCollision.Box) {
                            // Skip if top tile is a box
                            TilemapData.Tile topTile = y >= (rawData.Height - 1) ? new TilemapData.Tile() : rawData[x, y + 1, z];
                            if(topTile.id != 0 && map.Palette.GetTile(topTile.id).GetInfo(topTile.variant).Collision == TileCollision.Box) continue;

                            buildSources.Add(new NavMeshBuildSource {
                                transform = localToWorld * Matrix4x4.TRS(map.GridToLocal(new int3(x, y, z)), Quaternion.identity, Vector3.one),
                                shape = NavMeshBuildSourceShape.Box,
                                size = _gridSize,
                                area = bTile.NavigationArea
                            });

                        } else if(info.Collision == TileCollision.MeshBounds && info.Mesh != null) {
                            Bounds mBounds = info.Mesh.bounds;
    
                            Quaternion rotation = tile.GetRotation();
                            mBounds = new Bounds(rotation * mBounds.center, rotation * mBounds.size);

                            buildSources.Add(new NavMeshBuildSource {
                                transform = localToWorld * Matrix4x4.TRS(map.GridToLocal(new int3(x, y, z)) + (float3) mBounds.center, Quaternion.identity, Vector3.one),
                                shape = NavMeshBuildSourceShape.Box,
                                size = mBounds.size,
                                area = bTile.NavigationArea
                            });

                        } else if (info.Collision >= TileCollision.MeshConvex && info.CollisionMesh != null) {
                            buildSources.Add(new NavMeshBuildSource {
                                transform = localToWorld * Matrix4x4.TRS(map.GridToLocal(new int3(x, y, z)), tile.GetRotation(), Vector3.one),
                                shape = NavMeshBuildSourceShape.Mesh,
                                sourceObject = info.CollisionMesh,
                                area = bTile.NavigationArea
                            });

                        }
                    }
                }
            }

            NavMeshBuildSettings settings = NavMesh.GetSettingsByID(AgentPreset);
            settings.minRegionArea = GenerationSettings.MinArea;
            settings.overrideTileSize = true;
            settings.overrideVoxelSize = true;
            settings.voxelSize = GenerationSettings.VoxelSize;
            settings.tileSize = GenerationSettings.TileSize;
            settings.maxJobWorkers = GenerationSettings.MaxJobWorkers;

            float3 size = map.Data.Size * map.GridSize;
            Bounds bounds = new Bounds(size * 0.5f, size); // nav functions say local bounds but use global ones.... We wannt all sources to be considered so we make sure everything is within bounds

            _navMeshData = NavMeshBuilder.BuildNavMeshData(settings, buildSources, bounds, transform.position, transform.rotation);
            
            if(_navMeshData != null) {
                _navMeshData.name = gameObject.name;
                RemoveData();
                if(isActiveAndEnabled) AddData();
            }
        }

        public void Clear() {
            RemoveData();
            _navMeshData = null;
        }

        private void AddData() {
            if(_navMeshInstance.valid) return;

            if(_navMeshData != null) {
                _navMeshInstance = NavMesh.AddNavMeshData(_navMeshData, transform.position, transform.rotation);
                _navMeshInstance.owner = this;
            }

            if(Application.isPlaying && NavLinks != null && NavLinks.Length > 0) {
                _navLinkData = new NavMeshLinkData[NavLinks.Length];
                _navMeshLinkInstances = new NavMeshLinkInstance[NavLinks.Length];

                Transform trs = transform;

                for(int i = 0; i < NavLinks.Length; i++) {
                    NavLink link = NavLinks[i];

                    _navLinkData[i] = new NavMeshLinkData {
                        agentTypeID = AgentPreset,
                        startPosition = trs.TransformPoint((link.Start + new float3(0.5f, 0.0f, 0.5f)) * _gridSize),
                        endPosition = trs.TransformPoint((link.End + new float3(0.5f, 0.0f, 0.5f)) * _gridSize),
                        bidirectional = link.TwoDirection,
                        width = -1,
                        area = link.Area,
                        costModifier = -1
                    };

                    NavMeshLinkInstance instance = NavMesh.AddLink(_navLinkData[i]);
                    if(instance.valid) instance.owner = this;

                    _navMeshLinkInstances[i] = instance;
                }
            }
        }

        private void RemoveData() {
            _navMeshInstance.Remove();
            _navMeshInstance = new NavMeshDataInstance();

            if(_navMeshLinkInstances != null) for(int i = 0; i < _navMeshLinkInstances.Length; i++) _navMeshLinkInstances[i].Remove();
            _navMeshLinkInstances = null;
        }

        // Summary
        //      Manual link check since Unity functionality seems to be internal only
        public bool IsOfflink(Vector3 source, Vector3 target, float errorMargin = 0.1f) {
            if(_navLinkData == null) return false;

            foreach(NavMeshLinkData link in _navLinkData) {
                Vector3 linkSource = link.startPosition;
                Vector3 linkTarget = link.endPosition;

                if((Vector3.Distance(source, linkSource) < errorMargin && Vector3.Distance(target, linkTarget) < errorMargin) ||
                   (link.bidirectional && Vector3.Distance(source, linkTarget) < errorMargin && Vector3.Distance(target, linkSource) < errorMargin)) return true;
            }

            return false;
        }

    }    
}