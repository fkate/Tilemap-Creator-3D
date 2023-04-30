# TilemapCollider
Module component for generating collider components from TilePalette and TileMapData information. <br>
Will combine adjacent box colliders into bigger boxes. <br>
Should only be called sparsly due to the baking process generating a lot of components. <br>

## Inherited Methods
|Name|Usage|
|:---|:----|
|BakePartial(Tilemap3D map, Box3D area)|Unused|
|Bake(Tilemap3D map)|Bake box and mesh colliders to sub objects|
|Clear()|Clear generated sub objects|