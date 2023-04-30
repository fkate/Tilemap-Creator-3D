// Copyright (c) 2023 Felix Kate. BSD-3 license (see included license file)

namespace TilemapCreator3D {
    public interface ITilemapModule {

        // Summary
        //      Bake module only in specified area
        void BakePartial(Tilemap3D map, Box3D area);

        // Summary
        //      Bake module
        void Bake(Tilemap3D map);
                
        // Summary
        //      Clear module
        void Clear();

    }
}
