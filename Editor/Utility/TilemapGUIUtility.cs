// Copyright (c) 2023 Felix Kate. BSD-3 license (see included license file)
// Includes methods from NavMeshComponentsGUIUtility.cs of the NavMeshComponents Repository by Unity-Technologies

//The MIT License (MIT)

//Copyright (c) 2016, Unity Technologies

//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in
//all copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//THE SOFTWARE.

using TilemapCreator3D;
using UnityEditor;
using UnityEditor.AI;
using UnityEngine;
using UnityEngine.AI;

namespace EditorOnly {
    public static class TilemapGUIUtility {

        // Summary:
        //     Popup type for navigation areas
        public static void AreaPopup(string labelName, SerializedProperty areaProperty) {
            var areaIndex = -1;
            var areaNames = GameObjectUtility.GetNavMeshAreaNames();
            for (var i = 0; i < areaNames.Length; i++) {
                var areaValue = GameObjectUtility.GetNavMeshAreaFromName(areaNames[i]);
                if (areaValue == areaProperty.intValue)
                    areaIndex = i;
            }
            ArrayUtility.Add(ref areaNames, "");
            ArrayUtility.Add(ref areaNames, "Open Area Settings...");

            var rect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight);
            EditorGUI.BeginProperty(rect, GUIContent.none, areaProperty);

            EditorGUI.BeginChangeCheck();
            areaIndex = EditorGUI.Popup(rect, labelName, areaIndex, areaNames);

            if (EditorGUI.EndChangeCheck()) {
                if (areaIndex >= 0 && areaIndex < areaNames.Length - 2)
                    areaProperty.intValue = GameObjectUtility.GetNavMeshAreaFromName(areaNames[areaIndex]);
                else if (areaIndex == areaNames.Length - 1)
                    NavMeshEditorHelpers.OpenAreaSettings();
            }

            EditorGUI.EndProperty();
        }


        // Summary:
        //     Popup type for navigation areas using the area color
        public static void AreaColorField(Rect rect, SerializedProperty areaProperty) {
            var areaIndex = -1;
            var areaNames = GameObjectUtility.GetNavMeshAreaNames();
            for (var i = 0; i < areaNames.Length; i++) {
                var areaValue = GameObjectUtility.GetNavMeshAreaFromName(areaNames[i]);
                if (areaValue == areaProperty.intValue)
                    areaIndex = i;
            }
            ArrayUtility.Add(ref areaNames, "");
            ArrayUtility.Add(ref areaNames, "Open Area Settings...");

            EditorGUI.BeginProperty(rect, GUIContent.none, areaProperty);

            GUIStyle style = new GUIStyle("grey_border");
            style.imagePosition = ImagePosition.ImageOnly;

            EditorGUI.BeginChangeCheck();
            EditorGUI.DrawRect(rect, GetNavigationAreaColor(areaIndex));
            areaIndex = EditorGUI.Popup(rect, "", areaIndex, areaNames, style);

            if (EditorGUI.EndChangeCheck()) {
                if (areaIndex >= 0 && areaIndex < areaNames.Length - 2)
                    areaProperty.intValue = GameObjectUtility.GetNavMeshAreaFromName(areaNames[areaIndex]);
                else if (areaIndex == areaNames.Length - 1)
                    NavMeshEditorHelpers.OpenAreaSettings();
            }

            EditorGUI.EndProperty();
        }


        // Summary:
        //     Popup type for navigation agents
        public static void AgentTypePopup(string labelName, SerializedProperty agentTypeID) {
            var index = -1;
            var count = NavMesh.GetSettingsCount();
            var agentTypeNames = new string[count + 2];
            for (var i = 0; i < count; i++) {
                var id = NavMesh.GetSettingsByIndex(i).agentTypeID;
                var name = NavMesh.GetSettingsNameFromID(id);
                agentTypeNames[i] = name;
                if (id == agentTypeID.intValue)
                    index = i;
            }
            agentTypeNames[count] = "";
            agentTypeNames[count + 1] = "Open Agent Settings...";

            bool validAgentType = index != -1;
            if (!validAgentType) {
                EditorGUILayout.HelpBox("Agent Type invalid.", MessageType.Warning);
            }

            var rect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight);
            EditorGUI.BeginProperty(rect, GUIContent.none, agentTypeID);

            EditorGUI.BeginChangeCheck();
            index = EditorGUI.Popup(rect, labelName, index, agentTypeNames);
            if (EditorGUI.EndChangeCheck()) {
                if (index >= 0 && index < count) {
                    var id = NavMesh.GetSettingsByIndex(index).agentTypeID;
                    agentTypeID.intValue = id;
                }
                else if (index == count + 1) {
                    NavMeshEditorHelpers.OpenAgentSettings(-1);
                }
            }

            EditorGUI.EndProperty();
        }


        // Summary
        //      Unity internal calculation of navigation area color
        public static Color GetNavigationAreaColor(int i) {
            if (i == 0) return new Color(0, 0.75f, 1.0f, 0.5f);

            int r = (((i & (1 << 4)) >> 4) + ((i & (1 << 1)) >> 1) * 2 + 1) * 63;            
            int g = (((i & (1 << 3)) >> 3) + ((i & (1 << 2)) >> 2) * 2 + 1) * 63;
            int b = (((i & (1 << 5)) >> 5) + ((i & (1 << 0)) >> 0) * 2 + 1) * 63;

            return new Color(r / 255.0f, g / 255.0f, b / 255.0f, 0.5f);
        }


        // Summary
        //      Displays a toolbar like a field
        public static int ToolbarField(GUIContent label, int value, GUIContent[] options) {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(EditorGUIUtility.labelWidth - 1));
            value = GUILayout.Toolbar(value, options);
            EditorGUILayout.EndHorizontal();

            return value;
        }


        // Summary
        //      Draws a toggle as a toolbar (requires exactly 2 options in array)
        public static bool ToolbarToggleField(GUIContent label, bool value, GUIContent[] options) {
            return ToolbarField(label, value ? 1 : 0, options) == 1;
        }


        // Summary
        //      Draws a toggle as a popup (requires exactly 2 options in array)
        public static bool ToolbarTogglePopup(GUIContent label, bool value, GUIContent[] options) {
            return EditorGUILayout.Popup(label, value ? 1 : 0, options) == 1;
        }


        // Summary
        //      Shared module buttons
        public static void ShowBakeOptions<T>(T obj, bool clearingDisabled = false, bool bakingDisabled = false) where T : MonoBehaviour, ITilemapModule {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            EditorGUI.BeginDisabledGroup(clearingDisabled);
            if(GUILayout.Button("Clear", GUILayout.Width(100))) {           
                obj.Clear();

                EditorUtility.SetDirty(obj);
            }
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(bakingDisabled);
            if(GUILayout.Button("Bake", GUILayout.Width(100))) {            
                Tilemap3D map = obj.GetComponentInParent<Tilemap3D>();

                if(map != null) {
                    obj.Bake(map);

                    EditorUtility.SetDirty(obj);
                }
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();
        }

    }
}
