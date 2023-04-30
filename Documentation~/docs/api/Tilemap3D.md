# Tilemap3D
Main component for tilemap generation. Use Data modification in combination with one of the bake methods to update the map. <br>

## Properties
|Type|Name|Usage|
|:---|:---|:----|
|**TilePalette**|Palette|Tile palette stored in the map|
|**TileMapData**|Data|Map data. Read and write to it to modify the map|
|**float3**|GridSize|Size of a single cell. Should match the provided  tile mesh size|
|int|Width|Getter to Data.Width|
|int|Height|Getter to Data.Height|
|int|Depth|Getter to Data.Depth|
|int3|Size|Getter to Data.Size|
|int|Length|Getter to Data.Length|
|Box3D|Area|Local bounds of the map|
|TileMapData.Tile|this[int3]|Indexer to Data[int3]|
|ITilemapModule|Modules|Current list of tilemap modules in children|

<br>

## Methods
|Name|Usage|
|:---|:----|
|BakeDynamic(Box3D area)|Rebake dynamic module content within area|
|BakeDynamic()|Rebake dynamic module content for the whole map|
|Bake()|Bake static module content for the whole map|
|RefreshModules()|Force update to module list|

<br>
<br>

# Tilemap3DUtility
Static extension methods for Tilemap3D. <br>

## Static Methods
|Name|Usage|
|:---|:----|
|GetLayerPlane(int axis, int layer)|Calculate a local space plane on the given axis (0x-1y-2z) for the given layer|
|RaycastLayer(this Tilemap3D map, Ray ray, Plane plane, out int3 hit)|Raycast a local space plane. Input ray is in worldspace and will be transformed into grid space. Returns false if hit is outside bounds|
|GridToWorld(this Tilemap3D map, int3 position, float3 pivot)|Returns the map position at the given pivot (0-1 range) in world coordinates|
|GridToWorld(this Tilemap3D map, int3 position)|Returns the map position in world coordinates|
|GridToLocal(this Tilemap3D map, int3 position, float3 pivot)|Returns the map position at the given pivot (0-1 range) in local coordinates|
|GridToLocal(this Tilemap3D map, int3 position)|Returns the map position in local coordinates|
|WorldToGrid(this Tilemap3D map, float3 position)|Returns the map grid position at given world position (this includes out of bounds)|
|ReconstructPosition(this Tilemap3D map, int index)|Converts index into xyz coordinate|
|InBounds(this Tilemap3D map, int3 position)|Checks if the given grid coordinates are in bounds|
|PostProcessTiles(this Tilemap3D map, Box3D area)|Post processes area and it's neighbours. Returns the area containing it's neighbours|