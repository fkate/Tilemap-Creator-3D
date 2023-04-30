// Copyright (c) 2023 Felix Kate. BSD-3 license (see included license file)
// Internal class for handling the cursor that is displayed on the map.

using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace TilemapCreator3D.EditorOnly {
    internal class TilemapCursor {

        public struct ApplyAction {
            public Box3D Area;
            public TilemapData Data;
        }

        public System.Action<ApplyAction> Apply;

        public int3 Position => _location;

        private bool _visible;
        private bool _multiTile;

        private int3 _location;
        private Box3D _area;

        private int _id;
        private Unity.Mathematics.Random _random;

        private Tilemap3D _map;
        private TilemapSettings _settings;


        public TilemapCursor (Tilemap3D map) {
            _id = -1;

            _visible = true;

            _random = new Unity.Mathematics.Random((uint) (EditorApplication.timeSinceStartup * 173.37f));

            _map = map;
            _settings = TilemapSettings.instance;
        }


        public void Cleanup() {     
            Apply = null;
        }


        public bool Update() {
            if(_settings == null) {
                Debug.LogError("Could not locate tilemap settings");
                return false;
            }

            Event ev = Event.current;
            Ray ray = HandleUtility.GUIPointToWorldRay(ev.mousePosition);

            Shortcuts(ev);

            int3 hit;  
            bool inBounds = false;

            int3 axis = _settings.GetAxis();      
            Plane layerPlane = Tilemap3DUtility.GetLayerPlane(axis.z, _settings.GridLayer);
            inBounds = _map.RaycastLayer(ray, layerPlane, out hit);      

            UpdateMouse(ev, hit, inBounds);            

            DrawPreview(inBounds);

            return inBounds;
        }


        public void Shortcuts(Event ev) {
            if(ev.type == EventType.KeyDown && Tools.viewTool != ViewTool.FPS) {
                if(ev.keyCode == KeyCode.W) {
                    _settings.GridLayer++;
                    ev.Use();
                }
                else if(ev.keyCode == KeyCode.S) {
                    _settings.GridLayer--;
                    ev.Use();
                }

                if(ev.shift) {
                    if(ev.keyCode == KeyCode.A) {
                        _settings.Variant--;
                        ev.Use();
                    } else if(ev.keyCode == KeyCode.D) {
                        _settings.Variant++;
                        ev.Use();
                    }
                } else {
                    if(ev.keyCode == KeyCode.A) {
                        _settings.Rotation = (_settings.Rotation + 1) % 4;
                        ev.Use();
                    } else if(ev.keyCode == KeyCode.D) {
                        _settings.Rotation = (_settings.Rotation + 4 - 1) % 4;
                        // ^ +4 to prevent going into minus and thus breaking modulo
                        ev.Use();
                    }                    
                }

            }
        }


        public void UpdateMouse(Event ev, int3 pos, bool inBounds) {
            int3 target = pos;
        
            if(ev.isMouse) {
                // Drag start
                if (inBounds && ev.type == EventType.MouseDown && ev.button == 0) {
                    _location = pos;
                    _id = ev.shift ? 0 : _settings.Index;

                    ev.Use();

                // Drag update
                } else if (_id >= 0 && ev.type == EventType.MouseDrag) {
                    target = pos;

                // Drag end
                } else if (_id >= 0 && ev.type == EventType.MouseUp) {
                    if(Apply != null) Apply.Invoke(CreateAction());
                    _id = -1;

                    ev.Use();

                // Pick tile
                } else if(inBounds && ev.shift && ev.type == EventType.MouseDown && ev.button == 1) {
                    TilemapData.Tile tile = _map[pos];
                    
                    if(tile.id != 0) _settings.SettingsFromTile(tile);
                    
                    ev.Use();
                }

            // No Drag
            } else if (_id == -1) {
                _location = pos;

            }

            // Update common calculations
            _area = new Box3D(math.min(_location, target), math.max(_location, target) + 1);
            _multiTile = _area.Width > 1 || _area.Height > 1 || _area.Depth > 1;
        }


        private ApplyAction CreateAction() {
            bool rndVar = _settings.Randomizer % 2 == 1;
            bool rndRot = _settings.Randomizer >= 2;

            TilemapData data = new TilemapData(_area.Width, _area.Height, _area.Depth);
            for(int i = 0; i < data.Length; i++) {
                if(_id > 0) {
                    data[i] = new TilemapData.Tile() {
                        id = (byte) _id,
                        rotation = (byte) ((_settings.Rotation + (rndRot ? _random.NextInt(0, 4) : 0)) % 4),
                        variant = (byte) (_settings.Variant + (rndVar ? _random.NextInt(0, 255) : 0)),
                        flags = (byte) _settings.Flags
                    };
                } else {
                    data[i] = new TilemapData.Tile();
                }
            }

            return new ApplyAction { Area = _area, Data = data};
        }

    
        public void DrawPreview(bool inBounds) { 
            Transform trs = _map.transform;

            if(_settings.ShowGrid) DrawGrid(trs, _settings.GridLayer);

            if(inBounds || _multiTile) {
                if(_id == 0 || _multiTile) {
                    // Draw delete box
                    Handles.matrix = trs.localToWorldMatrix;

                    float3 size = _area.Size * _map.GridSize;
                    float3 center = _area.Min * _map.GridSize + size * 0.5f;

                    Handles.color = _id == 0 ? Color.red : Color.white;
                    Handles.DrawWireCube(center, size);
                }
                
                if(_id != 0) {
                    // Draw mesh
                    BaseTile bTile = _map.Palette.GetTile(_settings.Index);
                    Mesh previewMesh = bTile != null ? bTile.GetTilePreview(_settings.Variant) : null;

                    if(previewMesh != null && _visible) {
                        if(_settings.PreviewMode && bTile.Material != null) bTile.Material.SetPass(0);
                        else {
                            _settings.PreviewMaterial.SetColor("_Color", new Color(0.5f, 0.5f, 0.5f, 0.25f));
                            _settings.PreviewMaterial.SetPass(0);
                        }

                        // Small hack to avoid zfighting without using the shader offset
                        Camera cam = Camera.current;
                        float3 toCamOffset = cam != null ? -cam.transform.forward * 0.01f : 0;

                        for(int z = 0; z < _area.Depth; z++) {
                            for(int y = 0; y < _area.Height; y++) {
                                for(int x = 0; x < _area.Width; x++) {
                                    float3 wPos = _map.GridToWorld(_area.Min + new int3(x, y, z), new float3(0.5f, 0.5f, 0.5f)) + toCamOffset;
                                    Graphics.DrawMeshNow(previewMesh, Matrix4x4.TRS(wPos, trs.rotation * Quaternion.Euler(0, _settings.Rotation * 90, 0), trs.lossyScale));
                                }
                            }
                        }              
                    }
                }

                // Steal control to prevent clicking other objects
                HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            } else if(!inBounds) {
                // Move cursor out of bounds
                _settings.PreviewMaterial.SetVector("_Cursor", new Vector4(-5, -5, -5, -5));
            }

            SceneView.RepaintAll();
        }


        public void DrawGrid(Transform trs, int layer) {
            int3 axis = _settings.GetAxis();  
            int3 dSize = _map.Data.Size;
            float3 gridSize = _map.GridSize;

            _settings.PreviewMaterial.SetVector("_Grid", new Vector4(dSize[axis.x], dSize[axis.y], gridSize[axis.x], gridSize[axis.y]));
            _settings.PreviewMaterial.SetVector("_Cursor", new Vector4(_area.Min[axis.x], _area.Min[axis.y], _area.Max[axis.x], _area.Max[axis.y]));
            _settings.PreviewMaterial.SetPass(1);

            float3 pos = gridSize * -0.5f;

            if(axis.z == 2) layer += 1; // When working on z axis put grid one step forward. Inconsistent workflow but for 2D you expect the grid to be behind
            pos[axis.z] = layer * gridSize[axis.z];

            float3 xDir = 0, yDir = 0;
            xDir[axis.x] = (dSize[axis.x] + 1) * gridSize[axis.x];
            yDir[axis.y] = (dSize[axis.y] + 1) * gridSize[axis.y];

            float3 p0 = pos;            
            float3 p1 = pos + yDir;
            float3 p2 = pos + xDir;
            float3 p3 = p1 + xDir;

            float3 t0 = new float3(-0.5f, -0.5f, 0);
            float3 t1 = new float3(-0.5f, dSize[axis.y] + 0.5f, 0);
            float3 t2 = new float3(dSize[axis.x] + 0.5f, -0.5f, 0);
            float3 t3 = new float3(dSize[axis.x] + 0.5f, dSize[axis.y] + 0.5f, 0);

            GL.PushMatrix();
            GL.MultMatrix(trs.localToWorldMatrix);

            GL.Begin(GL.TRIANGLES);

            GL.TexCoord(t0);
            GL.Vertex(p0);
            GL.TexCoord(t1);
            GL.Vertex(p1);
            GL.TexCoord(t2);
            GL.Vertex(p2);

            GL.TexCoord(t3);
            GL.Vertex(p3);
            GL.TexCoord(t2);
            GL.Vertex(p2);
            GL.TexCoord(t1);
            GL.Vertex(p1);            

            GL.End();
            GL.PopMatrix();
        }


        public void SetVisible(bool value) {
            _visible = value;
        }

    }
}
