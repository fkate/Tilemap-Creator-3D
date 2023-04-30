# SingleTile
Class describing a tile without variants. <br>

## Properties
|Type|Name|Usage|
|:---|:---|:----|
|**TileInfo**|TileInfo|Settings of the tile|
|**int**|Length|Amount of sub variants. Always one|

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
|GetInfo(int index)|Always points at TileInfo|
|GetTilePreview(int index)|TileInfo.Mesh|
|PostProcessTile(TilemapData data, int3 pos)|Not implemented|