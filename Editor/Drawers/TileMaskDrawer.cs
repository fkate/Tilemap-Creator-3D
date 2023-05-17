// Copyright (c) 2023 Felix Kate. BSD-3 license (see included license file)
// Special drawers for tilemask and an additional compound type including rotation.

using UnityEditor;
using UnityEngine;

namespace TilemapCreator3D.EditorOnly {
    [CustomPropertyDrawer(typeof(TileMask))]
    public class TileMaskDrawer : PropertyDrawer {

        private GUIStyle _box = "AvatarMappingBox";
        private GUIStyle _check = "CN EntryBackEven";


        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            EditorGUI.BeginProperty(position, label, property);
            position = EditorGUI.PrefixLabel(position, label);

            // Convert mask to bools for displaying
            TileMask mask = (TileMask) property.enumValueFlag;

            bool tl =   mask.HasFlag(TileMask.TopLeft);  
            bool t =    mask.HasFlag(TileMask.Top);
            bool tr =   mask.HasFlag(TileMask.TopRight);
            bool l =    mask.HasFlag(TileMask.Left);
            bool r =    mask.HasFlag(TileMask.Right);
            bool bl =   mask.HasFlag(TileMask.BottomLeft);
            bool b =    mask.HasFlag(TileMask.Bottom);
            bool br =   mask.HasFlag(TileMask.BottomRight);

            float singleLine = EditorGUIUtility.singleLineHeight;
            float lineOffset = singleLine + EditorGUIUtility.standardVerticalSpacing / 2.0f;

            float padded = lineOffset - 2;
            float p0 = 1;
            float p1 = lineOffset + 1;
            float p2 = lineOffset * 2 + 1;

            GUI.Box(new Rect(position.x, position.y, lineOffset * 3, lineOffset * 3), "", _box);

            // Selection matrix
            EditorGUI.BeginChangeCheck();
            tl =    EditorGUI.Toggle(new Rect(position.x + p0, position.y + p0, padded, padded), tl, _check);
            t =     EditorGUI.Toggle(new Rect(position.x + p1, position.y + p0, padded, padded), t, _check);
            tr =    EditorGUI.Toggle(new Rect(position.x + p2, position.y + p0, padded, padded), tr, _check);

            l =     EditorGUI.Toggle(new Rect(position.x + p0, position.y + p1, padded, padded), l, _check);
                    EditorGUI.Toggle(new Rect(position.x + p1, position.y + p1, padded, padded), true, _check);
            r =     EditorGUI.Toggle(new Rect(position.x + p2, position.y + p1, padded, padded), r, _check);


            bl =    EditorGUI.Toggle(new Rect(position.x + p0, position.y + p2, padded, padded), bl, _check);
            b =     EditorGUI.Toggle(new Rect(position.x + p1, position.y + p2, padded, padded), b, _check);
            br =    EditorGUI.Toggle(new Rect(position.x + p2, position.y + p2, padded, padded), br, _check);

            // Listen to toggle clicks and convert results back to mask
            if(EditorGUI.EndChangeCheck()) {
                mask = (
                    (t  ? TileMask.Top          : TileMask.None) |
                    (tr ? TileMask.TopRight     : TileMask.None) |
                    (r  ? TileMask.Right        : TileMask.None) |
                    (br ? TileMask.BottomRight  : TileMask.None) |
                    (b  ? TileMask.Bottom       : TileMask.None) |
                    (bl ? TileMask.BottomLeft   : TileMask.None) |
                    (l  ? TileMask.Left         : TileMask.None) |
                    (tl ? TileMask.TopLeft      : TileMask.None)
                );
            
                property.enumValueFlag = (int) mask;
                property.serializedObject.ApplyModifiedProperties();
            }

            EditorGUI.EndProperty();        
        }


        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            return (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * 3;
        }

    }


    [CustomPropertyDrawer(typeof(TileMaskCompound))]
    public class TileMaskCompoundDrawer : PropertyDrawer {

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            EditorGUI.BeginProperty(position, label, property);

            float singleLine = EditorGUIUtility.singleLineHeight;
            float lineOffset = singleLine + EditorGUIUtility.standardVerticalSpacing;

            if(!string.IsNullOrEmpty(label.text)) {
                GUI.Label(new Rect(position.x, position.y, EditorGUIUtility.labelWidth, singleLine), label);
                position.x += EditorGUIUtility.labelWidth;
            }

            position.width = lineOffset * 2 + singleLine - 1;

            SerializedProperty typeProperty = property.FindPropertyRelative("Type");
            SerializedProperty maskProperty = property.FindPropertyRelative("Mask");

            Rect headerRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            float offset = headerRect.height + EditorGUIUtility.standardVerticalSpacing;
            float maskHeight = EditorGUI.GetPropertyHeight(maskProperty);
       
            GUI.Box(new Rect(position.x - 2, position.y, position.width + 4, position.height - 4.0f), "", "ChannelStripBg");
        
            TileMaskCompound.CompoundType type = (TileMaskCompound.CompoundType) EditorGUI.EnumPopup(new Rect(position.x, position.y + 3.0f, position.width, singleLine), (TileMaskCompound.CompoundType) typeProperty.enumValueIndex, "ToolbarCreateAddNewDropDown");

            EditorGUI.PropertyField(new Rect(position.x, position.y + offset + 4, position.width, maskHeight), maskProperty, new GUIContent());
        
            // Get mask variations by creating a temporary comound variable
            if(EditorGUI.EndChangeCheck()) {
                TileMaskCompound tempCompound = new TileMaskCompound((TileMask) maskProperty.enumValueFlag, type);

                typeProperty.enumValueIndex = (int) type;

                SerializedProperty array = property.FindPropertyRelative("Variants");
                array.arraySize = tempCompound.Variants.Length;

                // Copy results into the property
                for(int i = 0; i < tempCompound.Variants.Length; i++) {
                    array.GetArrayElementAtIndex(i).enumValueFlag = (int) tempCompound.Variants[i];
                }

                property.serializedObject.ApplyModifiedProperties();
            }


            EditorGUI.EndProperty();        
        }


        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            float lineOffset = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            return lineOffset * 4.5f;
        }

    }
}
