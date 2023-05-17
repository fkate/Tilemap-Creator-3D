// Copyright (c) 2023 Felix Kate. BSD-3 license (see included license file)

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.EditorCoroutines.Editor;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace TilemapCreator3D.EditorOnly {
    [CustomEditor(typeof(Tilemap3D))]
    public class Tilemap3DEditor : Editor {

        private TilemapCursor _cursor;

        private EditorCoroutine _dragCorountine;
        private bool _lockDrag;

        private Tilemap3D _map;
        private TilemapSettings _settings;

        private VisualElement _moduleList;


        public void OnEnable() {
            _map = target as Tilemap3D;
            _settings = TilemapSettings.instance;

            _cursor = new TilemapCursor(_map);
            _cursor.Apply += SetTiles;

            Undo.undoRedoPerformed += OnUndoRedo;

            _map.RefreshModules();
            _map.BakeDynamic();
        }


        public void OnDisable() {
            _cursor.Cleanup();
            _settings.SaveSettings();

            Undo.undoRedoPerformed -= OnUndoRedo;
        }


        public override VisualElement CreateInspectorGUI() {
            // Using visual elements for once (started as a performance measure and just stayed this way)
            VisualElement root = new VisualElement();
            root.styleSheets.Add(EditorGUIUtility.Load("Packages/com.fkate.tilemap3d/Editor/Styles/TilemapEditor.uss") as StyleSheet);

            Tilemap3D map = target as Tilemap3D;            
                        
            Vector3IntField dataSizeField = new Vector3IntField("Map Size");
            dataSizeField.Query<IntegerField>().ForEach((IntegerField field) => field.isDelayed = true);
            dataSizeField.SetValueWithoutNotify(new Vector3Int(map.Data.Width, map.Data.Height, map.Data.Depth));
            dataSizeField.RegisterValueChangedCallback(OnMapSizeChange);

            Vector3Field gridSizeField = new Vector3Field("GridSize");
            gridSizeField.SetValueWithoutNotify(new Vector3(map.GridSize.x, map.GridSize.y, map.GridSize.z));
            gridSizeField.RegisterValueChangedCallback(OnGridSizeChange);

            Label moduleLabel = new Label("Modules");
            moduleLabel.AddToClassList("tilemap-editor-modules-header");

            _moduleList = new VisualElement();
            _moduleList.AddToClassList("unity-help-box");
            _moduleList.AddToClassList("tilemap-editor-modules-list");
            GetModules();

            VisualElement buttonBar = new VisualElement();
            buttonBar.AddToClassList("tilemap-editor-modules-buttonbar");

            Button moduleProcessButton = new Button(ProcessModuleMenu);
            moduleProcessButton.AddToClassList("unity-help-box");
            moduleProcessButton.AddToClassList("tilemap-editor-modules-button");
            moduleProcessButton.RemoveFromClassList("unity-button");
            moduleProcessButton.style.borderRightWidth = 0;
            moduleProcessButton.style.borderBottomRightRadius = 0;

            Image processImage = new Image();
            processImage.image = EditorGUIUtility.Load("Packages/com.fkate.tilemap3d/Editor/Icons/Module.Process.png") as Texture2D;
            moduleProcessButton.Add(processImage);

            Button moduleAddButton = new Button(AddModuleMenu);
            moduleAddButton.AddToClassList("unity-help-box");
            moduleAddButton.AddToClassList("tilemap-editor-modules-button");
            moduleAddButton.RemoveFromClassList("unity-button");
            moduleAddButton.style.borderLeftWidth = 0;
            moduleAddButton.style.borderBottomLeftRadius = 0;
           
            Image addImage = new Image();
            addImage.image = EditorGUIUtility.Load("Packages/com.fkate.tilemap3d/Editor/Icons/Module.Add.png") as Texture2D;
            moduleAddButton.Add(addImage);

            buttonBar.Add(moduleAddButton);
            buttonBar.Add(moduleProcessButton);

            root.Add(dataSizeField);
            root.Add(gridSizeField);
            root.Add(moduleLabel);
            root.Add(_moduleList);
            root.Add(buttonBar);

            root.Bind(serializedObject);

            return root;
        }


        // Parse modules from interface array
        public void GetModules() {
            List<ITilemapModule> modules = _map.Modules;

            VisualElement[] elements = new VisualElement[modules.Count];

            _moduleList.Clear();

            if(modules.Count > 0) {
                // Create array of found sub modules
                for(int i = 0; i < modules.Count; i++) {
                    Object obj = modules[i] as Object;

                    GUIContent content = EditorGUIUtility.ObjectContent(obj, modules[i].GetType());

                    elements[i] = new VisualElement();
                    elements[i].userData = i;
                    elements[i].AddToClassList("tilemap-editor-modules-element");

                    elements[i].RegisterCallback<ClickEvent>(PerModuleMenu);

                    Image image = new Image();
                    image.image = content.image;

                    Label nameLabel = new Label(string.Format("{0} ({1})", obj.name, obj.GetType().Name));

                    elements[i].Add(image);
                    elements[i].Add(nameLabel);

                    _moduleList.Add(elements[i]);
                }            
            } else {
                Label emptyLabel = new Label("No modules found. Start by adding a mesh module.");
                emptyLabel.AddToClassList("tilemap-editor-modules-empty");

                _moduleList.Add(emptyLabel);
            }
        }


        // Context menu that finds Module implementations and displays them as a context menu
        private void AddModuleMenu() {
            GenericMenu menu = new GenericMenu();

            menu.AddDisabledItem(new GUIContent("Add module"));
            menu.AddSeparator("");

            List<ITilemapModule> modules = _map.Modules;

            System.Type[] attatchedTypes = new System.Type[_map.Modules.Count];
            for(int i = 0; i< _map.Modules.Count; i++) attatchedTypes[i] = _map.Modules[i].GetType();

            foreach(System.Type type in _settings.Modules) {
                if(attatchedTypes.Contains(type)) {
                    menu.AddDisabledItem(new GUIContent(type.Name));
                } else {                    
                    menu.AddItem(new GUIContent(type.Name), false, (object obj) => {
                        // Create new sub gameobject with the module and select it
                        System.Type t = obj as System.Type;
                        Transform parent = (target as Tilemap3D).transform;
                   
                        GameObject go = new GameObject(t.Name, t);
                        Transform trs = go.transform;
                        trs.SetParent(parent);
                        trs.localPosition = Vector3.zero;
                        trs.localRotation = Quaternion.identity;
                        trs.localScale = Vector3.one;

                        _map.RefreshModules();
                        _map.BakeDynamic();
                        GetModules();

                        SaveChanges();
                    }, type);
                }
            }
            
            menu.ShowAsContext();
        }

        
        // Context menu for processing modules (mostly for baking)
        private void ProcessModuleMenu() {
            GenericMenu menu = new GenericMenu();

            menu.AddDisabledItem(new GUIContent("Process modules"));
            menu.AddSeparator("");

            menu.AddItem(new GUIContent("Refresh"), false, () => { GetModules(); });

            menu.AddItem(new GUIContent("Bake"), false, () => {
                List<ITilemapModule> modules = _map.Modules;

                foreach(ITilemapModule module in modules) module.Bake(_map);
                SaveChanges();
            });

            menu.AddItem(new GUIContent("Clear"), false, () => {
                List<ITilemapModule> modules = _map.Modules;

                foreach(ITilemapModule module in modules) module.Clear();
                SaveChanges();
            });

            menu.ShowAsContext();
        }


        // Context menu for processing modules (mostly for baking)
        private void PerModuleMenu(ClickEvent ev) {
            VisualElement ve = ev.currentTarget as VisualElement;
            int id = (int) ve.userData;
            
            if(id >= 0 && id < _map.Modules.Count) {
                ITilemapModule module = _map.Modules[id];

                GenericMenu menu = new GenericMenu();

                menu.AddItem(new GUIContent("Select"), false, () => {
                    Selection.activeGameObject = (module as Behaviour).gameObject;
                });
                        
                menu.AddItem(new GUIContent("Bake"), false, () => {
                    module.Bake(_map);
                    SaveChanges();
                });

                menu.AddItem(new GUIContent("Clear"), false, () => {
                    module.Clear();
                    SaveChanges();
                });

                menu.AddItem(new GUIContent("Delete"), false, () => {
                    DestroyImmediate((module as Behaviour).gameObject);
                    _map.RefreshModules();
                    GetModules();
                    SaveChanges();
                });

                menu.ShowAsContext();
            }
        }


        // Draw invisible gizmo when not selected to make the tilemap selectable
        [DrawGizmo(GizmoType.NotInSelectionHierarchy | GizmoType.Pickable)]
        static void RenderBoxGizmoNotSelected(Tilemap3D map, GizmoType gizmoType) {
            Gizmos.matrix = map.transform.localToWorldMatrix;
            Gizmos.color = Color.clear;

            Vector3 size = map.Data.Size * map.GridSize;

            Gizmos.DrawCube(size * 0.5f, size);
        }


        private void OnGridSizeChange(ChangeEvent<Vector3> ev) {
            if(ev.newValue.x != _map.GridSize.x ||ev.newValue.y != _map.GridSize.y || ev.newValue.z != _map.GridSize.z) {
                _map.GridSize = ev.newValue;

                SaveChanges();
            }   
        }


        private void OnMapSizeChange(ChangeEvent<Vector3Int> ev) {           
            if(ev.newValue.x != _map.Data.Width ||ev.newValue.y != _map.Data.Height || ev.newValue.z != _map.Data.Depth) {
                _map.Data.Resize(ev.newValue.x, ev.newValue.y, ev.newValue.z);
                _map.BakeDynamic();

                SaveChanges();
            }   
        }


        private void OnUndoRedo() {
            (target as Tilemap3D).BakeDynamic();
        }
                

        public void OnSceneGUI() {
            _settings.SyncToMap(_map);
            bool inBounds = _cursor.Update();
            _cursor.SetVisible(_dragCorountine == null);

            HandleSceneDrag(inBounds);
        }


        // Handle custom drag event for gameobjects
        private void HandleSceneDrag(bool inBounds) {
            Event ev = Event.current;
            if(inBounds) {

                // Use corountine to overwrite drag position
                if(!_lockDrag && _dragCorountine == null && DragAndDrop.objectReferences.Length > 0) {
                    GameObject reference = DragAndDrop.objectReferences[0] as GameObject;

                    if(reference != null && reference.scene.name == null && PrefabUtility.IsPartOfAnyPrefab(reference)) {
                        // Find internal drag object
                        IEnumerable<GameObject> objects = FindObjectsOfType<GameObject>().Where(obj => obj.name == reference.name && obj.hideFlags == HideFlags.HideInHierarchy);

                        if(objects.Count() > 0) _dragCorountine = EditorCoroutineUtility.StartCoroutine(DragEnumerator(objects.First()), _map);

                        // Lock drag to prevent further searches for the same drag
                        _lockDrag = true;
                    }
                }

            } else {
                if(_dragCorountine != null) {
                    EditorCoroutineUtility.StopCoroutine(_dragCorountine);
                    _dragCorountine = null;
                }

                _lockDrag = false;
            }
        }


        // Overwrite internal drag objects position
        private IEnumerator DragEnumerator(GameObject target) {           
            Transform trs = target.transform;

            while(target != null && target.hideFlags == HideFlags.HideInHierarchy) {
                trs.position = _map.GridToWorld(_cursor.Position, new float3(0.5f, 0.0f, 0.5f));

                yield return null;
            }

            _dragCorountine = null;
        }


        // Set tiles as a result of a cursor action
        private void SetTiles(TilemapCursor.ApplyAction result) {
            Undo.RecordObject(target, "Set tiles");

            _map.Data.CopyData(result.Data, result.Area.Min);
            Box3D rebakeArea = _map.PostProcessTiles(result.Area);

            _map.BakeDynamic(rebakeArea);
            SaveChanges();
        }


        // Push changes to hierachy
        private void SaveChanges() => EditorUtility.SetDirty(_map);

    }
}
