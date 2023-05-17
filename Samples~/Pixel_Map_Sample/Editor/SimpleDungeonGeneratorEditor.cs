// Copyright (c) 2023 Felix Kate. BSD-3 license (see included license file)

using UnityEditor;
using UnityEngine;

namespace TilemapCreator3D.Samples {
    [CustomEditor(typeof(SimpleDungeonGenerator))]
    public class SimpleDungeonGeneratorEditor : Editor {
        public override void OnInspectorGUI() {
            base.OnInspectorGUI();

            if(GUILayout.Button("Generate")) {
                (target as SimpleDungeonGenerator).Generate();
                EditorUtility.SetDirty(target);
            }
        }
    }
}
