# TilePalette
Class for holding BaseTile data inside an array. Maximum amout of tiles is 255 since the zero id is reserved for empty tiles. <br>
Generally I will not reccomend messing with the array to much but you might need to if you plan to implement a runtime editor. <br>

## Properties
|Type|Name|Usage|
|:---|:---|:----|
|BaseTile|this[int]|Indexer to BaseTile array|
|int|Count|How many active tiles are inside the array|

<br>

## Methods
|Name|Usage|
|:---|:----|
|TilePalette()|Constructor|
|TilePalette(BaseTile[] entries)|Special constructor creating the palette from a collection (recommended for runtime setup)|
|Insert(BaseTile tile, int position, ref TilemapData data)|Insert tile at position. Fixes map data to match changes. Returns true if rebuild is necessary|
|Replace(BaseTile tile, int position)|Replace tile at position. Returns true if rebuild is necessary|
|Delete(int position, ref TilemapData data)|Delete index at position. Fixes map data to match changes. Returns true if rebuild is necessary|
|GetTile(int id)|Get tile at id point. Will ignore zero ids since they represent empty spaces|