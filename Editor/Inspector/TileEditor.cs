// Copyright (c) 2023 Felix Kate. BSD-3 license (see included license file)
// Inspector classes for all tile types included in the Tilemap3D package.

using EditorOnly;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace TilemapCreator3D.EditorOnly {
    [CustomEditor(typeof(BaseTile))]
    public class BaseTileEditor : Editor {

        protected TilePreviewRenderUtility RenderUtility;
        protected  OrientationGizmo OrientationGizmo;
        protected int PreviewIndex;


        public virtual void OnEnable() {
            // Create render utility for preview
            if(RenderUtility == null) RenderUtility = new TilePreviewRenderUtility();
            OrientationGizmo = new OrientationGizmo();
        }


        public virtual void OnDisable() {
            // Clean up render utility
            if(RenderUtility != null) RenderUtility.Cleanup();
            OrientationGizmo.Dispose();
        }


        public override void OnInspectorGUI() {
            serializedObject.Update();

            CommonGUI();

            serializedObject.ApplyModifiedProperties();

            PreviewIndex = 0;
        }    
        
        // Shared properties by all tiles.
        protected void CommonGUI() {
            EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);

            SerializedProperty materialProperty = serializedObject.FindProperty("Material");
            SerializedProperty collisionLayerProperty = serializedObject.FindProperty("CollisionLayer");
            SerializedProperty navigationAreaProperty = serializedObject.FindProperty("NavigationArea");

            EditorGUILayout.PropertyField(materialProperty);      
            collisionLayerProperty.intValue = EditorGUILayout.LayerField("Collision Layer", collisionLayerProperty.intValue);
            TilemapGUIUtility.AreaPopup("Navigation Area", navigationAreaProperty);

            EditorGUILayout.Space();
        }


        public override bool HasPreviewGUI() => RenderUtility != null;


        public override void OnPreviewSettings() {
            SerializedProperty orientationProp = serializedObject.FindProperty("PreviewOrientation");
            
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(orientationProp, new GUIContent());
            if(EditorGUI.EndChangeCheck()) serializedObject.ApplyModifiedProperties();
        }


        public override void OnPreviewGUI(Rect rect, GUIStyle background) {
            BaseTile bTile = target as BaseTile;

            RenderUtility.GetPreview(rect, background, bTile, PreviewIndex);

            Rect orientationRect = new Rect(rect.x + rect.width - 100, rect.y + 25, 60, 60);

            SerializedProperty orientationProp = serializedObject.FindProperty("PreviewOrientation");

            Vector2 orientation = OrientationGizmo.OnGUI(RenderUtility.GetRenderUtility(), orientationRect, orientationProp.vector2Value);

            if(!orientation.Equals(orientationProp.vector2Value)) {
                GUIUtility.hotControl = -1;

                serializedObject.FindProperty("PreviewOrientation").vector2Value = orientation;
                serializedObject.ApplyModifiedProperties();
            };

            EditorGUI.DrawPreviewTexture(orientationRect, OrientationGizmo.Texture);

            Repaint();
        }

    }


    [CustomEditor(typeof(SingleTile))]
    public class SingleTileEditor : BaseTileEditor {

        public override void OnInspectorGUI() {
            serializedObject.Update();

            CommonGUI();

            SerializedProperty tileProperty = serializedObject.FindProperty("TileInfo");
            EditorGUILayout.PropertyField(tileProperty, new GUIContent("Tile"));

            serializedObject.ApplyModifiedProperties();

            PreviewIndex = 0;
        }

    }


    [CustomEditor(typeof(MultiTile))]
    public class MultiTileEditor : BaseTileEditor {

        protected ReorderableList _tileList;


        public override void OnEnable() {
            base.OnEnable();

            // Create an internal list to make it possible to read selection
            _tileList = new ReorderableList(serializedObject, serializedObject.FindProperty("Variants"), true, true, true, true);
            _tileList.drawHeaderCallback += (Rect rect) => GUI.Label(rect, "Tiles");
            _tileList.drawElementCallback += (Rect rect, int index, bool isActive, bool isFocused) => EditorGUI.PropertyField(rect, _tileList.serializedProperty.GetArrayElementAtIndex(index));
            _tileList.elementHeightCallback += (int index) => EditorGUI.GetPropertyHeight(_tileList.serializedProperty.GetArrayElementAtIndex(index));
            _tileList.index = 0;
        }


        public override void OnInspectorGUI() {
            serializedObject.Update();

            CommonGUI();
            TileListGUI();

            serializedObject.ApplyModifiedProperties();

            PreviewIndex = _tileList.index;
        }


        protected void TileListGUI() {
            EditorGUILayout.Space();

            _tileList.DoLayoutList();
        }

    }


    [CustomEditor(typeof(AutoTile))]
    public class AutoTileEditor : MultiTileEditor {

        private GUIContent[] _bitModeOptions = new GUIContent[2] {
            new GUIContent("4 bit mask"),
            new GUIContent("8 bit mask")
        };

        private GUIContent[] _orientationOptions = new GUIContent[2] {
            new GUIContent("XZ"),
            new GUIContent("XY")
        };

        private GUIContent[] _borderOptions = new GUIContent[2] {
            new GUIContent("Solid Border"),
            new GUIContent("No Border")
        };

        private GUIContent[] _mergeOptions = new GUIContent[2] {
            new GUIContent("Merge"),
            new GUIContent("Isolate")
        };


        public override void OnEnable() {
            base.OnEnable();

            _tileList.drawHeaderCallback = BitModeField;
        }


        public override void OnInspectorGUI() {
            serializedObject.Update();

            CommonGUI();

            AutoTileGUI();

            TileListGUI();

            serializedObject.ApplyModifiedProperties();

            PreviewIndex = _tileList.index;
        }


        private void AutoTileGUI() {            
            EditorGUILayout.LabelField("Auto Tiling", EditorStyles.boldLabel);

            SerializedProperty borderProp = serializedObject.FindProperty("NoBorder");
            borderProp.boolValue = TilemapGUIUtility.ToolbarTogglePopup(new GUIContent("Border behaviour"), borderProp.boolValue, _borderOptions);

            SerializedProperty orientationProp = serializedObject.FindProperty("Orientation2D");
            orientationProp.boolValue = TilemapGUIUtility.ToolbarTogglePopup(new GUIContent("Orientation"), orientationProp.boolValue, _orientationOptions);

            SerializedProperty isolateProp = serializedObject.FindProperty("Isolate");
            isolateProp.boolValue = TilemapGUIUtility.ToolbarTogglePopup(new GUIContent("Merge Flag Behaviour"), isolateProp.boolValue, _mergeOptions);

            EditorGUILayout.Space();
        }


        private void BitModeField(Rect rect) {
            SerializedProperty bitProperty = serializedObject.FindProperty("EightBitMask");

            int mode = (bitProperty.boolValue ? 1 : 0);

            EditorGUI.BeginChangeCheck();

            mode = EditorGUI.Popup(rect, new GUIContent("Tiles"), mode, _bitModeOptions);

            if(EditorGUI.EndChangeCheck()) {
                bitProperty.boolValue = mode == 1;
            }
        }

    }
}