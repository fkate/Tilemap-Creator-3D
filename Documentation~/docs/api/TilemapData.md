# TilemapData
Struct holding the tilemap data inside a one dimensional int array. The array can be accessed via a set of methods. <br>

## Properties
|Type|Name|Usage|
|:---|:---|:----|
|int|Width|Width of the volume|
|int|Height|Height of the volume|
|int|Depth|Depth of the volume|
|int3|Size|Width height and depth of the volume|
|int|Length|Length of the data array|
|TileMapData.Tile|this[int, int, int]|Indexer to data from coordinate tripplet|
|TileMapData.Tile|this[int3]|Indexer to data from int3|
|TileMapData.Tile|this[int]|Indexer directly to raw data array|

<br>

## Methods
|Name|Usage|
|:---|:----|
|TilemapData(int width, int height, int depth)|Constructor|
|GetIndex(int x, int y, int z)|Convert position to array index|
|InRange(int x, int y, int z)|Check if position is inside volume|
|InRange(int index)|Check if index is inside data array|
|Resize(int width, int height, int depth)|Resize the data volume while maintaining content|

<br>
<br>

# TilemapData.Tile
Helper struct to convert stored integer into four bytes. <br>

## Properties
|Type|Name|Usage|
|:---|:---|:----|
|**byte**|id|Index of the tile data. Zero index means empty tile|
|**byte**|variant|Subvariant of the given index|
|**byte**|rotation|Rotation between 0-3|
|**byte**|flags|Aditional bitflags stored inside a byte|

<br>

## Methods
|Name|Usage|
|:---|:----|
|GetFlags()|Get flags as TileFlags bit mask|
|HasFlag(TileFlags flag)|Check if the tile has the given flag. Returns bool|
|GetRotation()|Get rotation as a quaternion|

<br>
<br>

# TilemapDataUtility
Static extension methods for TilemapData. <br>

## Static Methods
|Name|Usage|
|:---|:----|
|CopyData(this TilemapData target, TilemapData source, int3 pos)|Copy data from source to target starting at the set position|
|ClearArea(this TilemapData data, int3 pos, int3 size)|Clear all data in the given area|
|Clear(this TilemapData data)|Clear the whole TilemapData array|