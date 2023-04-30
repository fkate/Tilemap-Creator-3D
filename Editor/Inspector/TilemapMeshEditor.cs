// Copyright (c) 2023 Felix Kate. BSD-3 license (see included license file)

using UnityEditor;
using EditorOnly;

namespace TilemapCreator3D.EditorOnly {
    [CustomEditor(typeof(TilemapMesh))]
    public class TilemapMeshEditor : Editor {

        public override void OnInspectorGUI() {
            SerializedProperty chunkProp = serializedObject.FindProperty("ChunkSize");
            SerializedProperty vertexProp = serializedObject.FindProperty("VertexInfo");
            
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(chunkProp);
            EditorGUILayout.PropertyField(vertexProp);
            if(EditorGUI.EndChangeCheck()) serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();

            TilemapGUIUtility.ShowBakeOptions(target as TilemapMesh, true);
        }

    }
}