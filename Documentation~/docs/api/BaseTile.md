# BaseTile
Base class for all tiles. Inherit from it if you plan to add a custom tile.

## Properties
|Type|Name|Usage|
|:---|:---|:----|
|**Material**|Material|Shared material of all tile meshes|
|**int**|CollisionLayer|Layer generated colliders are put on|
|**int**|NavigationArea|Area for NavMesh generation|
|**TileInfo**|this[int]|Indexer to GetInfo()|
|**int**|Length|Amount of sub variants implemented by sub classes|
|**Vector2**|PreviewOrientation|Orientation of generated previews|
|**Quatenrion**|PreviewRotation|PreviewOrientation as Quaternion|

## Methods
|Name|Usage|
|:---|:----|
|GetInfo(int index)|Get tile info at index. Implemented by sub classes|
|GetTilePreview(int index)|Get mesh preview. Implemented by sub classes|
|PostProcessTile(TilemapData data, int3 pos)|Run custom logic over a placed tile. Implemented by sub classes|