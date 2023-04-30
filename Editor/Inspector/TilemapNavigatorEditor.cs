// Copyright (c) 2023 Felix Kate. BSD-3 license (see included license file)

using UnityEngine;
using UnityEditor;
using UnityEditor.AI;
using UnityEditorInternal;
using Unity.Mathematics;
using Unity.Collections;
using EditorOnly;

namespace TilemapCreator3D.EditorOnly {
    [CustomEditor(typeof(TilemapNavigator))]
    public class TilemapNavigatorEditor : Editor {

        private GUIContent[] _linkDirection;
        private ReorderableList _navLinkList;

        private AnimationCurve _arrowCurve;
        private Keyframe[] _arrowCurveKeys;

        private int2 _selectionIndex;


        private void OnEnable() {
            // Request showing navmeshes
            NavMeshVisualizationSettings.showNavigation++;

            // Nav mesh links don't have a custom drawer so they are handled as part of a reordable list
            _navLinkList = new ReorderableList(serializedObject, serializedObject.FindProperty("NavLinks"), true, true, true, true);
            _navLinkList.drawHeaderCallback = (Rect rect) => EditorGUI.LabelField(rect, "Navigation Links");
            _navLinkList.drawElementCallback = NavLinkDrawer;

            // Get internal icons here since we cannot do it outside of a method
            _linkDirection = new GUIContent[] {
                EditorGUIUtility.IconContent("PlayButton"),
                EditorGUIUtility.IconContent("playLoopOff")
            };

            _arrowCurveKeys = new Keyframe[2];
            _arrowCurve = new AnimationCurve(_arrowCurveKeys);
        }


        private void OnDisable() {
            // Remove from navmesh showing
            NavMeshVisualizationSettings.showNavigation--;
        }


        public override void OnInspectorGUI() {
            SerializedProperty presetProperty = serializedObject.FindProperty("AgentPreset");
            SerializedProperty generationSettingsProperty = serializedObject.FindProperty("GenerationSettings");

            EditorGUI.BeginChangeCheck();

            TilemapGUIUtility.AgentTypePopup("Agent Type", presetProperty);
            EditorGUILayout.PropertyField(generationSettingsProperty);

            EditorGUILayout.Space();
            _navLinkList.DoLayoutList();
            EditorGUILayout.Space();

            if(EditorGUI.EndChangeCheck()) {
                serializedObject.ApplyModifiedProperties();
            }

            TilemapGUIUtility.ShowBakeOptions(target as TilemapNavigator);
        }


        private void NavLinkDrawer(Rect rect, int index, bool isActive, bool isFocused) {
            SerializedProperty property = _navLinkList.serializedProperty.GetArrayElementAtIndex(index);

            float height = EditorGUIUtility.singleLineHeight;

            rect.y += 2;
            rect.x += height + 8;
            rect.width -= height - 8;
            
            float width = rect.width / 2.0f - 24;

            GUIContent empty = new GUIContent();

            property.Next(true);
            TilemapGUIUtility.AreaColorField(new Rect(rect.x - height - 8, rect.y, height, height), property);

            property.Next(false);
            EditorGUI.PropertyField(new Rect(rect.x, rect.y, width, height), property, empty);

            property.Next(false);
            EditorGUI.PropertyField(new Rect(rect.x + width + 32, rect.y, width, height), property, empty);

            property.Next(false);
            if(GUI.Button(new Rect(rect.x + width + 8, rect.y, 16, height), _linkDirection[property.boolValue ? 1 : 0], EditorStyles.iconButton)) property.boolValue = !property.boolValue;
        }


        private void OnSceneGUI() {
            TilemapNavigator navigator = target as TilemapNavigator;

            TilemapNavigator.NavLink[] links = navigator.NavLinks;

            if(links == null || links.Length == 0) return;
            
            // Grid size was copied during baking process
            float3 gridSize = navigator.GridSize;
            float3 offset = new float3(gridSize.x * 0.5f, 0, gridSize.y * 0.5f);
            float3 halfSize = gridSize * 0.5f;

            // Temporary array to hold box corners
            NativeArray<float3> offsets = new NativeArray<float3>(8, Allocator.Temp);
            offsets[0] = new float3(-halfSize.x, 0, -halfSize.z);
            offsets[1] = new float3(-halfSize.x, 0, halfSize.z);
            offsets[2] = new float3(halfSize.x, 0, halfSize.z);
            offsets[3] = new float3(halfSize.x, 0, -halfSize.z);
            offsets[4] = new float3(-halfSize.x, halfSize.y, -halfSize.z);
            offsets[5] = new float3(-halfSize.x, halfSize.y, halfSize.z);
            offsets[6] = new float3(halfSize.x, halfSize.y, halfSize.z);
            offsets[7] = new float3(halfSize.x, halfSize.y, -halfSize.z);

            Color colorTransparent = Color.clear;

            // Use GL to directly draw geometry
            GL.PushMatrix();
            GL.MultMatrix(navigator.transform.localToWorldMatrix);

            TilemapSettings.instance.PreviewMaterial.SetPass(2);

            GL.Begin(GL.TRIANGLES);

            foreach(TilemapNavigator.NavLink link in links) {
                // Dim color to make it look more like the NavMesh colors
                Color areaColor = TilemapGUIUtility.GetNavigationAreaColor(link.Area);
                Color colorStart = new Color(areaColor.r * 0.6f, areaColor.g * 0.6f, areaColor.b * 0.6f, 0.8f);
                Color colorEnd = new Color(areaColor.r * 0.8f, areaColor.g * 0.8f, areaColor.b * 0.8f, 0.8f);

                float3 start = link.Start * gridSize + offset;
                float3 end = link.End * gridSize + offset;

                // Draw the elements of the link
                HalfCube(start, offsets, colorStart, colorTransparent);
                HalfCube(end, offsets, colorEnd, colorTransparent);
                Arrow(start + new float3(0, halfSize.y, 0), end + new float3(0, halfSize.y, 0), colorStart, colorEnd, link.TwoDirection);
            }

            GL.End();

            GL.PopMatrix();

            offsets.Dispose();

            // Custom handle drawing for moving link points directly
            Handles.matrix = navigator.transform.localToWorldMatrix;
            
            Quaternion faceUp = Quaternion.Euler(90, 0, 0);
            float size = math.min(gridSize.x, gridSize.z) * 0.5f;
            
            Handles.color = Color.clear;

            for(int i = 0; i < links.Length; i++) {
                TilemapNavigator.NavLink link = links[i];
               
                float3 startPos = (link.Start + new float3(0.5f, 0.0f, 0.5f)) * gridSize;
                float3 endPos = (link.End + new float3(0.5f, 0.0f, 0.5f)) * gridSize;

                if(_selectionIndex.Equals(new int2(i, 1))) {
                    float3 newPos = Handles.PositionHandle(startPos, Quaternion.identity);
                    
                    // Copy position into the start int3
                    if(!newPos.Equals(startPos)) {
                        SerializedProperty prop = serializedObject.FindProperty("NavLinks").GetArrayElementAtIndex(i).FindPropertyRelative("Start");
                        int3 result = (int3) math.floor(newPos / gridSize);

                        prop.Next(true);
                        prop.intValue = result.x;
                        prop.Next(false);
                        prop.intValue = result.y;
                        prop.Next(false);
                        prop.intValue = result.z;

                        serializedObject.ApplyModifiedProperties();
                    }
                } else {
                    if(Handles.Button(startPos, faceUp, size, size, Handles.RectangleHandleCap)) _selectionIndex = new int2(i, 1);
                }

                if(_selectionIndex.Equals(new int2(i, 2))) {
                    float3 newPos = Handles.PositionHandle(endPos, Quaternion.identity);

                    // Copy position into the end int3
                    if(!newPos.Equals(endPos)) {
                        SerializedProperty prop = serializedObject.FindProperty("NavLinks").GetArrayElementAtIndex(i).FindPropertyRelative("End");
                        int3 result = (int3) math.floor(newPos / gridSize);
                        
                        prop.Next(true);
                        prop.intValue = result.x;
                        prop.Next(false);
                        prop.intValue = result.y;
                        prop.Next(false);
                        prop.intValue = result.z;

                        serializedObject.ApplyModifiedProperties();
                    }
                } else {
                    if(Handles.Button(endPos, faceUp, size, size, Handles.RectangleHandleCap)) _selectionIndex = new int2(i, 2);
                }
            }
        }


        // Helper method for drawing only the side faces of a cube with a alpha transition to the top
        private void HalfCube(float3 position, NativeArray<float3> offsets, Color colorLower, Color colorUpper) {
            for(int i = 0; i < 4; i++) {
                int turn0 = i;
                int turn3 = (i + 3) % 4;

                GL.Color(colorLower);
                GL.Vertex(position + offsets[turn0]);
                GL.Color(colorUpper);
                GL.Vertex(position + offsets[turn0 + 4]);
                GL.Color(colorLower);
                GL.Vertex(position + offsets[turn3]);
                GL.Color(colorUpper);
                GL.Vertex(position + offsets[turn3 + 4]);
                GL.Color(colorLower);
                GL.Vertex(position + offsets[turn3]);
                GL.Color(colorUpper);
                GL.Vertex(position + offsets[turn0 + 4]);
            }
        }


        // Helper method for drawing a line with optional arrow heads
        private void Arrow(float3 start, float3 end, Color colorStart, Color colorEnd, bool bidirectional) { 
            float3 forward = end - start;
            float distance = math.length(forward);
            
            forward = math.normalize(forward);
            float3 up = new float3(0, 1, 0);    
            float3 right = math.cross(up, forward) * 0.2f;
            float3 dRight = right * 2;

            int steps = (int) math.floor(distance * 8);

            float3 p0 = start;
            float3 p1;

            Color c0 = colorStart;
            Color c1;

            float peak = math.max(start.y, end.y) + 2.5f;

            _arrowCurveKeys[0] = new Keyframe(0.0f, start.y, 0, (peak - start.y));
            _arrowCurveKeys[1] = new Keyframe(1.0f, end.y, -(peak - end.y), 0);

            _arrowCurve.keys = _arrowCurveKeys;

            for(int i = 1; i <= steps; i++) {
                float percentage = (float) i / steps;

                p1 = math.lerp(start, end, percentage);
                p1.y = _arrowCurve.Evaluate(percentage);

                c1 = Color.Lerp(colorStart, colorEnd, percentage);

                if(i == 1 && bidirectional) {                    
                    GL.Color(c0);
                    GL.Vertex(p0);
                    GL.Color(c1);
                    GL.Vertex(p1 - dRight);
                    GL.Color(c1);
                    GL.Vertex(p1 + dRight);
                } else if (i == steps) {
                    GL.Color(c0);
                    GL.Vertex(p0 + dRight);
                    GL.Color(c0);
                    GL.Vertex(p0 - dRight);
                    GL.Color(c1);
                    GL.Vertex(p1);
                } else {
                    GL.Color(c0);
                    GL.Vertex(p0 - right);
                    GL.Color(c1);
                    GL.Vertex(p1 - right);
                    GL.Color(c0);
                    GL.Vertex(p0 + right);
                    GL.Color(c1);
                    GL.Vertex(p1 + right);
                    GL.Color(c0);
                    GL.Vertex(p0 + right);
                    GL.Color(c1);
                    GL.Vertex(p1 - right);
                }

                p0 = p1;
                c0 = c1;
            }
        }

    }
}