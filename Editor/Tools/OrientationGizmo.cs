// Copyright (c) 2023 Felix Kate. BSD-3 license (see included license file)
// Used to display a small cube gizmo that allows changing tile preview orientation.

using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace TilemapCreator3D.EditorOnly {
    public class OrientationGizmo : System.IDisposable {
        public Texture2D Texture => _texture;

        private Mesh _cube;
        private Material _materialGizmo;

        private Texture2D _texture;
        private float2 _lastOrientation;

        private int _dragPhase;
        private Vector2 _dragPosition;
        private int _contextAction;

        public OrientationGizmo() {
            _cube = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            _materialGizmo = new Material(Shader.Find("Hidden/OrientationGizmo"));

            Texture tex = EditorGUIUtility.Load("Packages/com.fkate.tilemap3d/Editor/Icons/Gizmo.Orientation.png") as Cubemap;   
            _materialGizmo.SetTexture("_MainTex", tex);
        }

        public void Dispose() {
            Object.DestroyImmediate(_materialGizmo);
            Object.DestroyImmediate(_texture);
        }        

        public Vector2 OnGUI(PreviewRenderUtility renderUtility, Rect rect, Vector2 orientation) {
            if(_lastOrientation.Equals(orientation) && _texture == null) return orientation;

            Quaternion asRotation = Quaternion.Euler(orientation.y, orientation.x, 0);

            renderUtility.BeginStaticPreview(rect);

            float diagonal = _cube.bounds.size.sqrMagnitude;

            renderUtility.camera.transform.position = asRotation * new Vector3(0, 0, -diagonal * 2.1f);
            renderUtility.camera.transform.rotation = asRotation;
            renderUtility.camera.nearClipPlane = 0.01f;
            renderUtility.camera.farClipPlane = 20.0f;

            renderUtility.DrawMesh(_cube, Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one), _materialGizmo, 0);
            renderUtility.Render(false);

            _texture = renderUtility.EndStaticPreview();

            return HandleEvent(renderUtility.camera, rect, orientation);
        }

        public Vector2 HandleEvent(Camera camera, Rect rect, Vector2 orientation) {
            Event ev = Event.current;
            
            if(_dragPhase > 0 && ev.isMouse && ev.button == 0 && ev.type == EventType.MouseUp) _dragPhase = 0;

            if(_dragPhase == 0 && ev.isMouse && rect.Contains(ev.mousePosition) && ev.type == EventType.MouseDown) {
                if(ev.button == 0) {

                    Vector2 localMouse = (ev.mousePosition - rect.min) / rect.size;
                    localMouse = new Vector2(localMouse.x, 1 - localMouse.y);

                    Ray ray = camera.ViewportPointToRay(localMouse);
                    Bounds bounds = _cube.bounds;

                    float distance;

                    if(bounds.IntersectRay(ray, out distance)) {
                        _dragPhase = 1;
                        _dragPosition = ev.mousePosition;

                    }
                } else if (ev.button == 1) {
                    GenericMenu contextMenu = new GenericMenu();
                    contextMenu.AddItem(new GUIContent("Z-"), false, () => _contextAction = 1);
                    contextMenu.AddItem(new GUIContent("Z+"), false, () => _contextAction = 2);
                    contextMenu.AddItem(new GUIContent("X-"), false, () => _contextAction = 3);
                    contextMenu.AddItem(new GUIContent("X+"), false, () => _contextAction = 4);
                    contextMenu.AddItem(new GUIContent("Y-"), false, () => _contextAction = 5);
                    contextMenu.AddItem(new GUIContent("Y+"), false, () => _contextAction = 6);
                    contextMenu.AddItem(new GUIContent("Default"), false, () => _contextAction = 7);

                    contextMenu.ShowAsContext();

                }
            } else if(_dragPhase == 1 && Vector2.Distance(ev.mousePosition, _dragPosition) > 20) {
                _dragPhase = 2;

            } else if(_dragPhase == 2) {
                Vector2 deltaPos = ev.mousePosition - _dragPosition;
                deltaPos *= 1;

                return new float2(math.fmod(_lastOrientation.x + deltaPos.x, 360), math.clamp(_lastOrientation.y + deltaPos.y, -89.999f, 89.999f));
            }

            // Move context result here since it's not possible to change references inside their action
            if(_contextAction > 0) {
                int executeAction = _contextAction;
                _contextAction = 0;

                switch(executeAction) {
                    case 1: return new Vector2(0, 0);
                    case 2: return new Vector2(180, 0);
                    case 3: return new Vector2(90, 0);
                    case 4: return new Vector2(-90, 0);
                    case 5: return new Vector2(0, -90);
                    case 6: return new Vector2(0, 90);
                    case 7: return new Vector2(20, 20);
                }
            }

            _lastOrientation = orientation;

            return orientation;
        }

    }    
}