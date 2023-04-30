// Copyright (c) 2023 Felix Kate. BSD-3 license (see included license file)

using UnityEditor;
using UnityEngine;

namespace TilemapCreator3D.EditorOnly {
    [CustomPropertyDrawer(typeof(TileInfo))]
    public class TileInfoDrawer : PropertyDrawer {

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            EditorGUI.BeginProperty(position, label, property);

            position.x += 16.0f;
            position.width -= 16.0f;
       
            float singleLine = EditorGUIUtility.singleLineHeight;
            float lineOffset = singleLine + EditorGUIUtility.standardVerticalSpacing;

            SerializedProperty meshProperty = property.FindPropertyRelative("Mesh");
            SerializedProperty collisionMeshProperty = property.FindPropertyRelative("CollisionMesh");
            SerializedProperty collisionProperty = property.FindPropertyRelative("Collision");

            Rect headerRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            float offset = headerRect.height + EditorGUIUtility.standardVerticalSpacing;

            GUI.Box(new Rect(position.x - 16.0f, position.y, position.width + 18.0f, position.height - 4.0f), "", "ChannelStripBg");

            EditorGUI.LabelField(new Rect(position.x, position.y + 3.0f, position.width, singleLine), label, EditorStyles.label);
        
            position.y += offset + 4;
            position.height -= offset;

            EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, singleLine), meshProperty);
            EditorGUI.PropertyField(new Rect(position.x, position.y + lineOffset, position.width, singleLine), collisionProperty);
            EditorGUI.PropertyField(new Rect(position.x, position.y + lineOffset * 2, position.width, singleLine), collisionMeshProperty);

            EditorGUI.EndProperty();
        }


        public override float GetPropertyHeight(SerializedProperty property,GUIContent label) {
            float lineOffset = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            return lineOffset * 4.5f;
        }

    }


    [CustomPropertyDrawer(typeof(TileInfoMask))]
    public class TileInfoMaskDrawer : PropertyDrawer {

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            EditorGUI.BeginProperty(position, label, property);

            float singleLine = EditorGUIUtility.singleLineHeight;
            float lineOffset = singleLine + EditorGUIUtility.standardVerticalSpacing;

            SerializedProperty infoProp = property.FindPropertyRelative("Info");
            SerializedProperty maskProp = property.FindPropertyRelative("Mask");

            float maskSpace = lineOffset * 2 + singleLine - 1;

            EditorGUI.PropertyField(new Rect(position.x, position.y, position.width - maskSpace - 10.0f, position.height), infoProp, label);
            EditorGUI.PropertyField(new Rect(position.x + position.width - maskSpace, position.y, maskSpace, position.height), maskProp, new GUIContent());

            EditorGUI.EndProperty();
        }


        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) { 
            return EditorGUI.GetPropertyHeight(property.FindPropertyRelative("Info"));
        }

    }
}