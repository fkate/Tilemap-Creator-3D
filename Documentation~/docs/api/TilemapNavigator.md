# TilemapNavigator
Module component for generating a NavMesh from TilemapData and Tile colliders. <br>

## Properties
|Type|Name|Usage|
|:---|:---|:----|
|**int**|AgentPreset|Gather build settings from Navigation window preset|
|**NavGenerationSettings**|GenerationSettings|Additional settings. Refer to NavMeshBuildSettings in Unitys documentation|
|**NavLink**|NavLinks|Set Area, Start, End and Directionality of navigation links. The actual links are generated at runtime|

<br>

## Inherited Methods
|Name|Usage|
|:---|:----|
|BakePartial(Tilemap3D map, Box3D area)|Unused|
|Bake(Tilemap3D map)|Bake Unity NavMesh from tile information|
|Clear()|Clear NavMesh|