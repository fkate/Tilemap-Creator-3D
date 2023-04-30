# TilemapMesh
Module component for generating a mesh from TilePalette and TileMapData information. <br>

## Properties
|Type|Name|Usage|
|:---|:---|:----|
|**int3**|ChunkSize|Size of chunk in grid units. Zero or smaller results in the max of the dimension|
|**VertexSettings**|VertexInfo|Included attributes in baking process|

<br>

## Inherited Methods
|Name|Usage|
|:---|:----|
|BakePartial(Tilemap3D map, Box3D area)|Bake mesh information in overlapping chunks|
|Bake(Tilemap3D map)|Full rebake of all chunks|
|Clear()|Unused|