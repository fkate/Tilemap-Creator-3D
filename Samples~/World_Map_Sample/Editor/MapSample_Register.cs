// Copyright (c) 2023 Felix Kate. BSD-3 license (see included license file)

using TilemapCreator3D.EditorOnly;
using UnityEditor;

namespace TilemapCreator3D.Samples.EditorOnly {
    [InitializeOnLoad]
    public class MapSample_Register {
        static MapSample_Register () {
            TilemapSettings settings = TilemapSettings.instance;

            settings.RegisterModule<TilemapPrefabModule>();
        }

    }
}
