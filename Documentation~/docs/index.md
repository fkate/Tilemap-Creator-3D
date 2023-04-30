# Tilemap Creator 3D Overview

![Preview](img/preview.png)

This package contains an editor editor extension for speeding up the development with mesh based tiles inside the Unity engine. <br>
It's features and modular extensibility make it widely usable over  all kind of projects both at editor and runtime. <br>

## Features
- Components for authoring tile based maps (processing via modular sub components)
- Mesh based tile setups via different included types (Single / Multi / Auto)
- Mesh module using Unitys Job System to efficiently combine Mesh Tiles into a large mesh (supports chunking)
- Collision module to create optimized box compound colliders (supports mesh colliders if needed)
- Navigation module for baking a Unity NavMesh for the map
- Interfaces to provide custom modules / tiletypes if required

## Requirements
Unity 2021+ (tested on 2021.3)

## Install instructions
Start a new Unity Project and navigate to the Package Manager (Window > Package Manager). <br>
Open the [+] dropdown and select "Add package from git URL ...". The URL is the adress of this repository. <br>