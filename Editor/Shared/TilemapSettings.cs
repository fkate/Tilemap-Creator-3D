// Copyright (c) 2023 Felix Kate. BSD-3 license (see included license file)
// Central class to hold temporary values for working with the tilemap in the editor.
// Also provides utility methods for reading and writing to them.

using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace TilemapCreator3D.EditorOnly {
    [FilePath("UserSettings/Tilemap3DSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public class TilemapSettings : ScriptableSingleton<TilemapSettings> {

        [SerializeField] public int Index = 0;
        [SerializeField] public int Variant = 0;
        [SerializeField] public int Rotation = 0;
        [SerializeField] public TileFlags Flags = 0;
        [SerializeField] public bool ShowGrid = true;
        [SerializeField] public int GridLayer = 0;
        [SerializeField] public int GridAxis = 1;
        [SerializeField] public bool PreviewMode = false;
        [SerializeField] public int Randomizer = 0;
        [SerializeField] public float ExplorerTileSize = 80.0f;

        [NonSerialized] public Action OnTilePick;

        public List<Type> Modules => _modules;
        public GUIContent[] FlagContent => _flagContent;

        public List<Type> _modules = new List<Type>(0);
        public GUIContent[] _flagContent = new GUIContent[8];

        public Material PreviewMaterial {
            get {
                if(_previewMaterial == null) _previewMaterial = new Material(Shader.Find("Hidden/TilePreview"));
                return _previewMaterial;
            }
        }

        private Material _previewMaterial;


        // Summary
        //      Save current settings to file
        public void SaveSettings() {
            Save(true);
        }

        
        // Summary
        //      Limit ranges to current tilemap
        public void SyncToMap(Tilemap3D map) {
            if(map.Data.Length == 0 || map.Palette == null || map.Palette.Count == 0) return;

            GridLayer = math.clamp(GridLayer, 0, map.Data.Size[GetAxis().z] - 1);
            Index = math.clamp(Index, 0, map.Palette.Count);
            Variant = math.clamp(Variant, 0, map.Palette.GetTile(Index).Length - 1);
        }

        
        // Summary
        //      Get axis order from settings
        public int3 GetAxis() {
            switch(GridAxis) {
                case 1: // XZY
                    return new int3(0, 2, 1);
                case 2: // XYZ
                    return new int3(0, 1, 2);
                default: // ZYX
                    return new int3(2, 1, 0);
            }
        }


        // Summary
        //      Register a module to show in the module menu
        public void RegisterModule<T>() where T : ITilemapModule {
            System.Type type = typeof(T);
            if(!Modules.Contains(type)) Modules.Add(type);
        }

        
        // Summary
        //      Register a custom flag
        public void RegisterFlagContent(GUIContent content, int id) {
            if(id < 0 || id >= 8) return;
            FlagContent[id] = content;
        }


        // Summary
        //      Copy the tile data to settings and call OnTilePick
        public void SettingsFromTile(TilemapData.Tile tile) {
            Index = tile.id;
            Variant = tile.variant;
            Rotation = tile.rotation;
            Flags = tile.GetFlags();

            if(OnTilePick != null) OnTilePick.Invoke();
        }

    }
}
