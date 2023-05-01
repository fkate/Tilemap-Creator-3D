# FAQ
## Can this be used to create games of genre X?
| Genre     | Support |  Comments                                                             |
|:----------|:--------|:----------------------------------------------------------------------|
| Topdown   | Yes     | Main focus of the tool                                                |
| Rougelike | Yes     | Procedural map generation not included                                |
| 2.5D      | "Yes"   |  Limited support on modules                                           |
| 2D        | No      | Use Unity Tilemap instead                                             |
| Blockgame | No      | Data is stored in a fixed length array (not intended for huge worlds) |

## Why only Unity 2021+
Unity 2019 does not support the new Mesh API and 2020 does not support custom toolbars.

## Where can I find the documentation?
There is a subfolder called ~Documentation. In there you can find a selection of articles about the general usage.

## Is this URP/HDRP compatible?
Yes.

## Is the tilemap runtime compatible?
Yes and no. All the tile logic and meshing methods should be usable at runtime.<br>
The interface is Unity Editor only though so you will have to write your own runtime interface.

## I have an issue?
Since this is a hobby project I do not offer live support. If you encountered a bug file an issue on the repository page. <br>
If it's about a feature request or something covered in the FAQ I might not feel inclined to answer though.

## Why is it not on the Asset Store?
a) I don't see a lot of value from selling it. The maintenance cost / revenue ballance does not seem worth it <br>
b) I want to give users the ability to easily accsess and learn from the code <br>

## Can I include this as part of a commercial offering?
Please do not sell the source or include it inside a product sold via the Asset Store. <br>
Everything else is totally fine with me and covered by the license.

## Where do I get the tile meshes from?
Sources of compatible 3D meshes are not common so you are for most part on your own. <br>
You can use a modeling tool like Blender to create your own tiles. There is a section in the documentation that goes over this topic.

## Can I contribute?
The package itself is feature complete and I would like to keep the amount of contributors to a minimum so in most cases the answer is no. <br>
To avoid bloating the package itself new features are intended to be added as modules (check the documentation and the WorldMapSample to get a genreal idea). <br>
If you require some larger scale modifications to the source please create a fork instead.

## I created something using the package? (custom module or game)
Feel free to drop me a message (mail is at the bottom of my portfolio page) and I will give you a shoutout in the documentation.