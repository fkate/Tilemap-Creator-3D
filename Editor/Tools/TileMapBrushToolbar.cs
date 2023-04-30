// Copyright (c) 2023 Felix Kate. BSD-3 license (see included license file)
// Internal classes for handling the toolbar

using EditorOnly;
using System.Collections;
using System.Collections.Generic;
using Unity.EditorCoroutines.Editor;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.Toolbars;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace TilemapCreator3D.EditorOnly {
    [Overlay(typeof(SceneView), ID, true)]
    [Icon("Icons/Overlays/GridAndSnap.png")]
    internal class TileMapBrushToolbar : ToolbarOverlay {

        public const string ID = "TileMap Brush";


        public TileMapBrushToolbar() : base(
            "TileMap/Grids",
            "TileMap/Tile",
            "TileMap/Flags"
        ){ }

    }
    
    [EditorToolbarElement("TileMap/Grids", typeof(SceneView))]
    internal class TileMapGridToggle : EditorToolbarDropdownToggle, IAccessContainerWindow {

        private class GridPopup : PopupWindowContent {

            private readonly GUIContent[] _gridAxisOptions = new GUIContent[3] {
                new GUIContent("X"),
                new GUIContent("Y"),
                new GUIContent("Z")
            };
            
            private readonly GUIContent[] _previewModeOptions = new GUIContent[2] {
                new GUIContent("Shape"),
                new GUIContent("Material")
            };


            public override Vector2 GetWindowSize() => new Vector2(300, (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * 4 + 8);


            public override void OnGUI(Rect rect) {
                rect = new Rect(rect.x + 4, rect.y + 2, rect.width - 8, rect.height - 4);

                GUILayout.BeginArea(rect);

                EditorGUILayout.LabelField("Grid Settings", EditorStyles.boldLabel);
                TilemapSettings settings = TilemapSettings.instance;

                settings.GridAxis = TilemapGUIUtility.ToolbarField(new GUIContent("Axis"), settings.GridAxis, _gridAxisOptions);
                settings.PreviewMode = TilemapGUIUtility.ToolbarToggleField(new GUIContent("Preview Mode"), settings.PreviewMode, _previewModeOptions);
                settings.GridLayer = EditorGUILayout.IntField("Working layer", settings.GridLayer);

                GUILayout.EndArea();
            }
        }

    
        public EditorWindow containerWindow { get; set; }


        public TileMapGridToggle() {
            name = "TileMap Grid";
            tooltip = "Toggle grid";
            icon = EditorGUIUtility.Load("Packages/com.fkate.tilemap3d/Editor/Icons/TileGrid.png") as Texture2D;

            this.RegisterValueChangedCallback(delegate(ChangeEvent<bool> evt) { TilemapSettings.instance.ShowGrid = evt.newValue; });

            RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }


        private void OnDropdownClicked() {
            Event ev = Event.current;

            UnityEditor.PopupWindow.Show(worldBound, new GridPopup());
        }


        private void OnAttachedToPanel(AttachToPanelEvent evt) {
            value = TilemapSettings.instance.ShowGrid;
            dropdownClicked += OnDropdownClicked;
        }


        private void OnDetachFromPanel(DetachFromPanelEvent evt) {
            dropdownClicked -= OnDropdownClicked;
        }

    }

    [EditorToolbarElement("TileMap/Tile", typeof(SceneView))]
    internal class TileMapTileDropdown : EditorToolbarDropdown {

        private enum TileRotation {
            _0deg = 0,
            _90deg = 1,
            _180deg = 2,
            _270deg = 3
        };

        private enum TileRandomizer {
            None = 0,
            Variant = 1,
            Rotation = 2,
            Both = 3
        }

    
        private class TilePopup : PopupWindowContent {

            public override Vector2 GetWindowSize() => new Vector2(300, (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * 4 + 8);


            public override void OnGUI(Rect rect) {
                rect = new Rect(rect.x + 4, rect.y + 2, rect.width - 8, rect.height - 4);

                TileRotation rotation = (TileRotation) TilemapSettings.instance.Rotation;
                TileRandomizer randomizer = (TileRandomizer) TilemapSettings.instance.Randomizer;

                GUILayout.BeginArea(rect);

                EditorGUILayout.LabelField("Tile Settings", EditorStyles.boldLabel);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("ID", GUILayout.Width(EditorGUIUtility.labelWidth));
                TilemapSettings.instance.Index = EditorGUILayout.IntField(TilemapSettings.instance.Index);
                TilemapSettings.instance.Variant = EditorGUILayout.IntField(TilemapSettings.instance.Variant);
                EditorGUILayout.EndHorizontal();

                rotation = (TileRotation) EditorGUILayout.EnumPopup("Rotation", rotation);

                randomizer = (TileRandomizer) EditorGUILayout.EnumPopup("Tile randomization", randomizer);

                GUILayout.EndArea();

                TilemapSettings.instance.Rotation = (int) rotation;
                TilemapSettings.instance.Randomizer = (int) randomizer;
            }

        }


        public TileMapTileDropdown() {
            name = "TileMap Brush";
            tooltip = "Brush settings";
            icon = EditorGUIUtility.Load("Packages/com.fkate.tilemap3d/Editor/Icons/TileBrush.png") as Texture2D;

            clicked += OnDropdownClicked;
        }


        private void OnDropdownClicked() {
            Event ev = Event.current;

            UnityEditor.PopupWindow.Show(worldBound, new TilePopup());
        }

    }

    [EditorToolbarElement("TileMap/Flags", typeof(SceneView))]
    internal class TileMapFlagsFoldout : VisualElement {

        private TilemapSettings _settings;

        private List<ToolbarToggle> _toggles;

        private Mesh _cube;
        private List<Matrix4x4> _transforms;


        public TileMapFlagsFoldout() {
            name = "TileMap Flags";
            tooltip = "Flag settings";
            
            StyleSheet style = EditorGUIUtility.Load("Packages/com.fkate.tilemap3d/Editor/Styles/TilemapToolbar.uss") as StyleSheet;

            _settings = TilemapSettings.instance;
            _settings.OnTilePick += RefreshToggles;

            VisualElement root = this;
            root.AddToClassList("unity-base-field");
            root.AddToClassList("unity-base-field--no-label");
            root.AddToClassList("unity-editor-toolbar-element");
            root.AddToClassList("tilemap-editor-toolbar-flags");

            root.styleSheets.Add(style);

            VisualElement toolbar = new VisualElement();
            toolbar.AddToClassList("tilemap-editor-flagToolbar");

            _toggles = new List<ToolbarToggle>(8);

            int flagValue = 1;

            for(int i = 0; i < 8; i++) {
                GUIContent flagContent = _settings.FlagContent[i];

                TileFlags flag = (TileFlags) flagValue;
                flagValue *= 2;

                if(flagContent == null || flagContent.image == null) continue;

                ToolbarToggle toggle = new ToolbarToggle();                

                toggle = new ToolbarToggle();
                toggle.userData = flag;
                toggle.tooltip = flagContent.tooltip;
                toggle.AddToClassList("tilemap-editor-flagToggle");
                toggle.SetValueWithoutNotify(_settings.Flags.HasFlag(flag));
                
                toggle.RegisterValueChangedCallback(OnFlagToggle);

                Image flagIcon = new Image();
                flagIcon.image = flagContent.image;

                VisualElement toggleLabel = toggle.Query<VisualElement>(className: "unity-toggle__input");
                toggleLabel.Add(flagIcon);

                toolbar.Add(toggle);
                _toggles.Add(toggle);
            }

            root.Add(toolbar);
        }


        private void RefreshToggles() {
            for(int i = 0; i < _toggles.Count; i++) {
                TileFlags flag = (TileFlags) _toggles[i].userData;
                _toggles[i].SetValueWithoutNotify(_settings.Flags.HasFlag(flag));
            }
        }


        private void OnFlagToggle(ChangeEvent<bool> evt) {
            TileFlags flag = (TileFlags) (evt.target as VisualElement).userData;

            if(evt.newValue) _settings.Flags |= flag;
            else _settings.Flags &= ~flag;

            if(evt.newValue) {
                Tilemap3D target = Selection.activeGameObject != null ? Selection.activeGameObject.GetComponent<Tilemap3D>() : null;
                if(target == null) return;

                if(_cube == null) _cube = Resources.GetBuiltinResource<Mesh>("Cube.fbx");

                TilemapData data = target.Data;
                Transform trs = target.transform;

                Vector3 position;
                Vector3 scale = new Vector3(0.15f, 0.15f, 0.15f);
                Quaternion rotation = trs.rotation;

                if(_transforms == null || _transforms.Capacity != data.Length) _transforms = new List<Matrix4x4>(data.Length);
                _transforms.Clear();

                for(int z = 0; z < data.Depth; z++) {
                    for(int y = 0; y < data.Height; y++) {
                        for(int x = 0; x < data.Width; x++) {
                            TilemapData.Tile tile = data[x, y, z];
                            if(tile.id != 0 && tile.GetFlags().HasFlag(flag)) {
                                position = target.GridToWorld(new int3(x, y, z), new float3(0.5f, 0.5f, 0.5f));
                                
                                _transforms.Add(Matrix4x4.TRS(position, rotation, scale));
                            }
                        }
                    }
                }

                EditorCoroutineUtility.StartCoroutine(FlashTiles(Color.grey), this);
            }
        }


        private IEnumerator FlashTiles(Color color) {               
            SceneView.duringSceneGui += DrawTiles;

            Color transparent = new Color(color.r, color.g, color.b, 0);
            color.a = 0.5f;

            double start = EditorApplication.timeSinceStartup;

            double difference = 0;

            AnimationCurve curve = new AnimationCurve(new Keyframe(0, 0, 0, 0), new Keyframe(0.25f, 1.0f, 0, 0), new Keyframe(0.75f, 1.0f, 0, 0), new Keyframe(1.0f, 0.0f, 0, 0));
            curve.SmoothTangents(1, 1.0f);
            curve.SmoothTangents(2, 1.0f);

            while(difference <= 1) {
                difference = EditorApplication.timeSinceStartup - start;

                TilemapSettings.instance.PreviewMaterial.SetColor("_Color", Color.Lerp(transparent, color, curve.Evaluate((float) difference / 1)));

                yield return null;
            }

            SceneView.duringSceneGui -= DrawTiles;
        }


        private void DrawTiles(SceneView sceneView) {               
            TilemapSettings.instance.PreviewMaterial.SetPass(0);
            for(int i = 0; i < _transforms.Count; i++) {
                Graphics.DrawMeshNow(_cube, _transforms[i], 0);
            }
        }

    }

}