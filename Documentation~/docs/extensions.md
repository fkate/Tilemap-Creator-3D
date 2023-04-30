# Extending the tool
The package has multiple interfaces that allow the user to add his/her custom script components into the pipeline. <br>
I reccomend taking a look at the WorldMap sample which shows some actual implementations. <br>

## Adding a custom tile type
Create a new class extending the Tilemap3D.BaseTile. <br>
Tile types are centered around the TileInfo struct which consists of { Mesh / CollisionType / CollisionMesh } <br>
Each tile also recives a sub index that can be used if a tile has multiple variants. <br>
If your tile targets a custom module that does not need this information (example one that adds GameObjects) feel free to return an empty TileInfo. <br>

```csharp
public class CustomTile:BaseTile {    
    // How many sub tile variants are expected
    public override int Length { get; }

    // Give reference to the tile info struct used to store mesh / collisiontype / collisionmesh at the requested variant index
    public override TileInfo GetInfo(int index);

    // Which mesh should the picker show?
    public override Mesh GetTilePreview(int index) => null;

    // This will run after tile placement is complete. It can be used for adjusting tiles depending on their environment
    public override void PostProcessTile(ref TileMapData data, int3 pos) { }
}
```

## Add a custom module
Create a Monobehaviour that includes the interface Tilemap3D.ITilemapModule. <br>
To register a module for easier access refer to the next section. <br>
The structure of a module is as follows: <br>

```csharp
    public class MyModule:Monobehaviour, ITilemapModule {
        // Used to update a specific part of the tilemap immediatly on tile placement. Keep heavy calculations to a minimum
        public void BakePartial(Tilemap map, Box3D area) {}

        // Bake calculation or garbage heavy data on demans
        void Bake(Tilemap map);
                
        // Clear baked data on demand
        void Clear();
    }
```

## Register an extension in the editor
Create a new static class and and give it an appropriate name. <br>
The class needs the **[InitializeOnLoad]** attribute to register before everything else. <br>
Inside the class add a static constrctor with the same name as the class. <br>
Use the TilemapSettings class inside the Tilemap3D.EditorOnly namespace to register your additions. <br>

```csharp
    [InitializeOnLoad]
    public class MyRegisterClass {
        static MyRegisterClass () {
            // Register a module to the Tilemap dropdown
            TilemapSettings.instance.RegisterModule<MyCustomModule>();

            // Register a flag to be shown in the toolbar (Up to 8 flags in total. Flag 0 and 1 are already used by autotile)
            TilemapSettings.instance.RegisterFlagContent(new GUIContent(image, tooltip), yourID);
        }
    }
```