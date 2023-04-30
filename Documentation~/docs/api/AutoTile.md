# AutoTile
Class describing a tile using multiple variants to create borders.

## Properties
|Type|Name|Usage|
|:---|:---|:----|
|**TileInfoMask[]**|Variants|Collection of sub tiles using TileMasks|
|**int**|Length|Sub tile count|
|**bool**|EightBitMask|Does the algorithm use eight or four directions|
|**bool**|NoBorder|Merges tiles with the border|
|**bool**|Orientation2D|Is the algorithm processing the XY layer instead of XZ|
|**bool**|Isloate|Ignore tile flags and never merge to other tiles|

<br>

## Inherited Properties
|Type|Name|Usage|
|:---|:---|:----|
|**Material**|Material|Shared material of all tile meshes|
|**int**|CollisionLayer|Layer generated colliders are put on|
|**int**|NavigationArea|Area for NavMesh generation|
|**TileInfo**|this[int]|Indexer to GetInfo()|
|**Vector2**|PreviewOrientation|Orientation of generated previews|
|**Quatenrion**|PreviewRotation|PreviewOrientation as Quaternion|

<br>

## Methods
|Name|Usage|
|:---|:----|
|GetInfo(int index)|Get sub tile at sub tile index|
|GetTilePreview(int index)|Always returns first variant|
|PostProcessTile(TilemapData data, int3 pos)|Fix data via auto tiling algorithm|