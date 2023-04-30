# TilemapSettings (Editor only)

Settings object that gets stored in ProjectRoot/UserSettings. Save serialization is called when switching away from an active tilemap. <br>
Values are mainly modified by package interfaces but can be freely set for extensions as well. <br>

## Properties
|Type|Name|Usage|
|:---|:---|:----|
|**int**|Index|Active index of the cursor (zero is empty)|
|**int**|Variant|Active sub index of the cursor|
|**int**|Rotation|Active rotation of the cursor (0-3)|
|**TileFlags**|Flags|Active flags of the cursor|
|**bool**|ShowGrid|Shows or hides the grid|
|**int**|GridLayer|Active working layer of the grid|
|**int**|GridAxis|Display axis of the grid (0=x 1=y 2=z)|
|**float3**|GridSize|Size of a single cell. Should match the provided  tile mesh size|
|**bool**|PreviewMode|Toggles between shape and textured preview|
|**int**|Randomizer|Tile placing setting (0=none 1=variant 2=rotation 3=both)|
|**float**|ExplorerTileSize|Display size of tiles in the explorer (should be set via slider)|
|**Action**|OnTilePick|Subscribe to recive a change notification once a tile is picked|
|List&lt;System.Type&gt;|Modules|List of registered modules|
|GUIContent[]|FlagContent|List of registered flag buttons|
|Material|PreviewMaterial|Single instance to ("Hidden/TilePreview")|

<br>

## Methods
|Name|Usage|
|:---|:----|
|SaveSettings()|Save current settings to file|
|SyncToMap(Tilemap3D map)|Limit ranges to current tilemap|
|GetAxis()|Get axis order from settings as int3|
|RegisterModule&lt;iTilemapModule&gt;()|Register a module to show in the module menu|
|RegisterFlagContent(GUIContent content, int id)|Register a custom flag (0 and 1 are used by autotiling)|
|SettingsFromTile(TilemapData.Tile tile)|Copy the tile data to settings and call OnTilePick|