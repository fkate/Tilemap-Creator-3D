// Copyright (c) 2023 Felix Kate. BSD-3 license (see included license file)

using UnityEditor;
using EditorOnly;

namespace TilemapCreator3D.EditorOnly {
    [CustomEditor(typeof(TilemapCollider))]
    public class TilemapColliderEditor : Editor {

        public override void OnInspectorGUI() {
            EditorGUILayout.HelpBox("This module does not provide any configurable settings.\n Please use the colision based options inside the tile objects.", MessageType.Info);

            EditorGUILayout.Space();

            TilemapGUIUtility.ShowBakeOptions(target as TilemapCollider);
        }

    }
}