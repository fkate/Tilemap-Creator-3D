// Copyright (c) 2023 Felix Kate. BSD-3 license (see included license file)
// Window class for a more visual tile selection. Supports drag and drop to add in new tiles

using Unity.Mathematics;
using UnityEditor;
using UnityEditor.Search;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace TilemapCreator3D.EditorOnly {
    public class TileExplorer : EditorWindow {

        private const int PREVIEW_TEXTURE_SIZE = 64;

        private Tilemap3D Target;
        private int _targetID;

        private TilePreviewRenderUtility _renderUtility;

        private TilemapSettings _settings;
        
        private ToolbarSearchField _searchField;
        private ScrollView _scrollView;
        private ToolbarToggle[] _toggles;
        private Image _dragArea;

        [MenuItem("Window/Tile Explorer")]
        static void Init() {
            TileExplorer window = (TileExplorer) GetWindow(typeof(TileExplorer));
            window.titleContent = new GUIContent("Tile Explorer", EditorGUIUtility.Load("Packages/com.fkate.tilemap3d/Editor/Icons/TileExplorer.png") as Texture2D);
            window.Show();
        }

        public void OnEnable() {
            // Create render utility for preview
            if(_renderUtility == null) _renderUtility = new TilePreviewRenderUtility();

            _settings = TilemapSettings.instance;
            _settings.OnTilePick += SetToggles;

            Selection.selectionChanged += OnSelectionChanged;
            OnSelectionChanged();
        }

        public void OnDisable() {
            // Clean up render utility
            if(_renderUtility != null) _renderUtility.Cleanup();
            
            _settings.OnTilePick -= SetToggles;

            Selection.selectionChanged -= OnSelectionChanged;
        }


        // Summary
        //      Check if a new map was selected
        private void OnSelectionChanged() {
            bool hasTarget = Target != null;
            bool hasSelection = Selection.activeGameObject != null;

            bool stateChange = (hasTarget != hasSelection) || (hasSelection && _targetID != Selection.activeGameObject.GetInstanceID());
            
            if(stateChange) {
                if(!hasSelection || !Selection.activeGameObject.TryGetComponent(out Target)) Target = null;
                else _targetID = Target.gameObject.GetInstanceID();
                
                RefreshList();
            }
        }


        // Summary
        //      Create static GUI UIElements
        public void CreateGUI() {
            VisualElement root = rootVisualElement;
            root.styleSheets.Add(EditorGUIUtility.Load("Packages/com.fkate.tilemap3d/Editor/Styles/TileExplorer.uss") as StyleSheet);

            Toolbar upperBar = new Toolbar();
            upperBar.AddToClassList("tilemap-explorer-toolbar");
            
            _searchField = new ToolbarSearchField();
            _searchField.AddToClassList("tilemap-explorer-searchfield");
            _searchField.RegisterValueChangedCallback(SearchField);
            upperBar.Add(_searchField);

            _scrollView = new ScrollView(ScrollViewMode.Vertical);
            _scrollView.AddToClassList("tilemap-explorer-gridview");
            _scrollView.RegisterCallback<DragEnterEvent>(OnDragEnterEvent);
            _scrollView.RegisterCallback<DragLeaveEvent>(OnDragLeaveEvent);
            _scrollView.RegisterCallback<DragUpdatedEvent>(DragOverContainer);
            _scrollView.RegisterCallback<DragPerformEvent>(OnDragPerformEvent);
            _scrollView.RegisterCallback<KeyUpEvent>(DeleteTile, TrickleDown.TrickleDown);
            _scrollView.focusable = true;

            Toolbar lowerBar = new Toolbar();
            lowerBar.AddToClassList("tilemap-explorer-toolbar");

            Slider slider = new Slider(80, 160);
            slider.AddToClassList("tilemap-explorer-slider");
            slider.SetValueWithoutNotify(_settings.ExplorerTileSize);
            slider.RegisterValueChangedCallback(ChangePreviewSize);
            lowerBar.Add(slider);

            root.Add(upperBar);
            root.Add(_scrollView);
            root.Add(lowerBar);

            RefreshList();
        }


        // Summary
        //      Filter shown tiles by name
        private void Search(string filter) {  
            if(_toggles == null) return;
            
            bool emptyString = string.IsNullOrEmpty(filter);

            for(int i = 0; i < _toggles.Length; i++) {
                string name = _toggles[i].tooltip;

                _toggles[i].style.display = (emptyString || name.Contains(filter, System.StringComparison.CurrentCultureIgnoreCase)) ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }


        // Summary
        //      Refresh the tilemap. This wil allso recreate previews so should not be spammed
        private void RefreshList() {
            // Not initialized
            if(_scrollView == null) return;

            _scrollView.Clear();

            _toggles = null;

            if(Target == null || Target.Palette.Count == 0) return;

            TilePalette palette = Target.Palette;

            Texture2D[] previews = _renderUtility.GetStaticPreviews(Target.Palette, PREVIEW_TEXTURE_SIZE, PREVIEW_TEXTURE_SIZE);
            
            _toggles = new ToolbarToggle[palette.Count];

            for(int i = 0; i < palette.Count; i++) {
                BaseTile bTile = palette[i];

                ToolbarToggle toggle = new ToolbarToggle();
                toggle.AddToClassList("tilemap-explorer-tile");
                toggle.style.width = _settings.ExplorerTileSize;
                toggle.style.height = _settings.ExplorerTileSize;

                toggle.tooltip = bTile.name;
                toggle.userData = i + 1;
                toggle.RegisterValueChangedCallback(ChangeIndex);
                toggle.RegisterCallback<DragUpdatedEvent>(DragOverChild);
                toggle.RegisterCallback<MouseDownEvent>(ShowTileContext);

                Image image = new Image();
                image.image = previews[i];

                Label label = new Label(bTile.name);

                toggle.Add(image);
                toggle.Add(label);
                _scrollView.Add(toggle);
                _toggles[i] = toggle;
            }

            SetToggles();
            Search(_searchField.value);
        }


        // Summary
        //      Refresh toggles to match the selection
        private void SetToggles() {
            if(_toggles == null) return;

            for(int i = 0; i < _toggles.Length; i++) {
                int id = (int) _toggles[i].userData;

                _toggles[i].SetValueWithoutNotify(id == _settings.Index);
            }
        }
        

        // Summary
        //      Drag enters the container window. Will create a temporary drag indicator if dragged object is a tile
        private void OnDragEnterEvent(DragEnterEvent ev) {
            if(Target != null && DragAndDrop.objectReferences.Length > 0 && DragAndDrop.objectReferences[0] is BaseTile tile) {
                _dragArea = new Image();
                _dragArea.AddToClassList("tilemap-explorer-dragarea");
                _dragArea.style.width = _settings.ExplorerTileSize;
                _dragArea.style.height = _settings.ExplorerTileSize;
                _dragArea.image = _renderUtility.GetStaticPreview(tile, 0, PREVIEW_TEXTURE_SIZE, PREVIEW_TEXTURE_SIZE);
                _dragArea.userData = tile;
                _scrollView.Add(_dragArea);
            }
        }


        // Summary
        //      Clean up if drag leaves the area without doing anything
        private void OnDragLeaveEvent(DragLeaveEvent e) {
            if(_dragArea != null) {
                _scrollView.Remove(_dragArea);
                _dragArea = null;
            }
        }


        // Summary
        //      Basic drag update event for the container
        private void DragOverContainer(DragUpdatedEvent e) {
            if(_dragArea == null) return;

            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
        }


        // Summary
        //      Drag event over the child to decide order
        private void DragOverChild(DragUpdatedEvent ev) {
            if(_dragArea == null) return;

            VisualElement ve = (VisualElement) ev.currentTarget;

            float dir = math.sign(ev.localMousePosition.x / _settings.ExplorerTileSize - 0.5f);

            if(dir < 0) _dragArea.PlaceInFront(ve);
            else if(dir > 0) _dragArea.PlaceBehind(ve);
        }


        // Summary
        //      Execute drag. If sucessful insert tile and refresh the map
        private void OnDragPerformEvent(DragPerformEvent ev) {
            if(_dragArea == null || Target == null) return;
            
            DragAndDrop.AcceptDrag();

            BaseTile tile = (BaseTile) _dragArea.userData;
            int position = _scrollView.IndexOf(_dragArea);

            if(Target.Palette.Insert(tile, position, ref Target.Data)) ApplyToMap();
            else _scrollView.Remove(_dragArea);

            _dragArea = null;
        }


        // Summary
        //      Called upon slider adjustments to change the tiles preview size
        private void ChangePreviewSize(ChangeEvent<float> value) {
            _settings.ExplorerTileSize = value.newValue;
            
            if(_toggles == null) return;

            // Cannot acsess custom style so need to set size directly to the elements
            for(int i = 0; i <_toggles.Length; i++) {
                _toggles[i].style.width = _settings.ExplorerTileSize;
                _toggles[i].style.height = _settings.ExplorerTileSize;
            }
        }


        // Summary
        //      Change index to selected toggle
        private void ChangeIndex(ChangeEvent<bool> value) {
            VisualElement element = value.currentTarget as VisualElement;
            _settings.Index = (int) element.userData;

            SetToggles();
        }


        // Summary
        //      Left click context menu for the toggles
        private void ShowTileContext(MouseDownEvent ev) {
            if(ev.button != 1) return;

            int index = (int) ((VisualElement) ev.currentTarget).userData - 1;

            GenericMenu contextMenu = new GenericMenu();
            contextMenu.AddItem(new GUIContent("Show in explorer"), false, () => {
                Selection.activeObject = Target.Palette[index];
            });
            contextMenu.AddItem(new GUIContent("Replace"), false, () => {
                // Use Unitys shiny new UI features. Might break if Unity does any large changes over the versions
                SearchService.ShowObjectPicker((Object obj, bool value) => {
                    if(obj != null && Target.Palette.Replace(obj as BaseTile, index)) ApplyToMap();
                }, null, "", typeof(BaseTile).Name, typeof(BaseTile));
                
            });
            contextMenu.AddItem(new GUIContent("Remove"), false, () => {
                if(Target.Palette.Delete(index, ref Target.Data)) ApplyToMap();
            });
            contextMenu.ShowAsContext();
        }


        // Summary
        //      Direct deletion event only executable if the grid is focused
        private void DeleteTile(KeyUpEvent ev) {
            if(ev.keyCode != KeyCode.Delete || Target == null) return;

            int index = _settings.Index - 1;

            if(Target.Palette.Delete(index, ref Target.Data)) ApplyToMap();
        }


        // Summary
        //      Update search field string
        private void SearchField(ChangeEvent<string> value) => Search(value.newValue);


        // Summary
        //      Push changes to the tilemap
        private void ApplyToMap() {
            Target.BakeDynamic();
            EditorUtility.SetDirty(Target);
            RefreshList();
        }

    }
}
