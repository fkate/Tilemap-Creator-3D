# ITilemapModule
Interface for implementing modules. Should always be used in combination with Monobehaviour. <br>
Extend this class if you want to write a custom module. <br>

## Methods
|Name|Usage|
|:---|:----|
|BakePartial(Tilemap3D map, Box3D area)|Called when map is baked dynamicly|
|Bake(Tilemap3D map)|Called when map is baked|
|Clear()|Clean up module data on demand|