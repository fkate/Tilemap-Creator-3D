// Copyright (c) 2023 Felix Kate. BSD-3 license (see included license file)

using UnityEditor;
using UnityEngine;

namespace TilemapCreator3D.EditorOnly {
    [InitializeOnLoad]
    public class TilemapRegister {

        static TilemapRegister () {
            TilemapSettings settings = TilemapSettings.instance;

            // Register flags
            settings.RegisterFlagContent(new GUIContent(EditorGUIUtility.Load("Packages/com.fkate.tilemap3d/Editor/Icons/Flag-MergeA.png") as Texture2D, "Merge Flag 0"), 0);
            settings.RegisterFlagContent(new GUIContent(EditorGUIUtility.Load("Packages/com.fkate.tilemap3d/Editor/Icons/Flag-MergeB.png") as Texture2D, "Merge Flag 1"), 1);

            // Register modules
            settings.RegisterModule<TilemapMesh>();
            settings.RegisterModule<TilemapCollider>();
            settings.RegisterModule<TilemapNavigator>();
        }

        [MenuItem("GameObject/3D Object/Tilemap3D", false, 100)]
        static void AddTilemap3D() {
            GameObject go = new GameObject("Tilemap3D");
            go.AddComponent<Tilemap3D>();
            Selection.activeGameObject = go;
        }

    }
}
