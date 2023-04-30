# MultiTile
Class describing a tile with multiple variants. <br>

## Properties
|Type|Name|Usage|
|:---|:---|:----|
|**TileInfo[]**|Variants|Collection of sub tiles|
|**int**|Length|Sub tile count|

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
|GetTilePreview(int index)|Get preview mesh at sub tile index|
|PostProcessTile(TilemapData data, int3 pos)|Not implemented|