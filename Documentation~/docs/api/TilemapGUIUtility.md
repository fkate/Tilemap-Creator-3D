# TilemapGUIUtility (Editor only)
A static helper class to draw commonly used GUI elements. <br>

## Static Methods
|Name|Usage|
|:---|:----|
|AreaPopup(string labelName, SerializedProperty areaProperty)|Popup type for navigation areas|
|AreaColorField(Rect rect, SerializedProperty areaProperty)|Popup type for navigation areas using the area color|
|AgentTypePopup(string labelName, SerializedProperty agentTypeID)|Popup type for navigation agents|
|GetNavigationAreaColor(int i)|Unity internal calculation of navigation area color|
|ToolbarField(GUIContent label, int value, GUIContent[] options)|Displays a toolbar like a field|
|ToolbarToggleField(GUIContent label, bool value, GUIContent[] options)|Draws a toggle as a toolbar (requires exactly 2 options in array)|
|ToolbarTogglePopup(GUIContent label, bool value, GUIContent[] options)|Draws a toggle as a popup (requires exactly 2 options in array)|
|ShowBakeOptions&lt;ITilemapModule&gt;(ITilemapModule obj, bool clearingDisabled, bool bakingDisabled)|Shared module buttons|