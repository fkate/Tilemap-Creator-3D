// Copyright (c) 2023 Felix Kate. BSD-3 license (see included license file)
// Wrapper around preview render utility specialized at rendering tiles

using UnityEditor;
using UnityEngine;

namespace TilemapCreator3D.EditorOnly {
    public class TilePreviewRenderUtility {

        private PreviewRenderUtility _renderUtility;

        public TilePreviewRenderUtility() {
            // Create render utility for preview
            if(_renderUtility == null) {
                _renderUtility = new PreviewRenderUtility();
                _renderUtility.camera.clearFlags = CameraClearFlags.Color;
                _renderUtility.camera.backgroundColor = new Color(0, 0, 0, 0);
            }
        }

        public PreviewRenderUtility GetRenderUtility() => _renderUtility;

        public void Cleanup() {
            _renderUtility.Cleanup();
            _renderUtility = null;
        }


        // Summary
        //      Get a dynamic preview for the tile editor
        public void GetPreview(Rect rect, GUIStyle background, BaseTile tile, int variation) {
            _renderUtility.BeginPreview(rect, background);

            DrawMeshPreview(variation < 0 || variation >= tile.Length ? null : tile.GetInfo(variation).Mesh, tile.Material, tile.PreviewRotation);

            _renderUtility.EndAndDrawPreview(rect);
        }

        
        // Summary
        //      Create previews for a whole palette. Only the first variation will get a preview
        public Texture2D[] GetStaticPreviews(TilePalette palette, int width, int height) {
            if(palette.Count == 0) return new Texture2D[0];

            Texture2D[] outputs = new Texture2D[palette.Count];
            Rect rect = new Rect(0, 0, width, height);

            RenderTexture.active = _renderUtility.camera.targetTexture;

            // We don't use BeginStaticPreview due to it's limitation when it comes to transparent backgrounds
            _renderUtility.BeginPreview(new Rect(rect.x, rect.y, rect.width * 0.5f, rect.height * 0.5f), new GUIStyle());

            for(int i = 0; i < outputs.Length; i++) {
                BaseTile bTile = palette[i];

                if(bTile == null) continue;

                if(DrawMeshPreview(bTile.GetTilePreview(0), bTile.Material, bTile.PreviewRotation)) {
                    // Copy active rendertexture
                    outputs[i] = new Texture2D(width, height, TextureFormat.ARGB32, false);
                    outputs[i].ReadPixels(rect, 0, 0);
                    outputs[i].Apply();
                }  
            }

            _renderUtility.EndPreview();

            RenderTexture.active = null;

            return outputs;
        }


        // Summary
        //      Create a preview for a tile
        public Texture2D GetStaticPreview(BaseTile tile, int variation, int width, int height) {
            if(tile == null) return null;

            Texture2D output = new Texture2D(width, height, TextureFormat.ARGB32, false);
            Rect rect = new Rect(0, 0, width, height);

            _renderUtility.BeginPreview(new Rect(rect.x, rect.y, rect.width * 0.5f, rect.height * 0.5f), new GUIStyle());

            if(DrawMeshPreview(tile.GetTilePreview(variation), tile.Material, tile.PreviewRotation)) {
                // We don't use BeginStaticPreview due to it's limitation when it comes to transparent backgrounds
                RenderTexture.active = _renderUtility.camera.targetTexture;

                // Copy active rendertexture
                output.ReadPixels(rect, 0, 0);
                output.Apply();
            
                RenderTexture.active = null;
            }

            _renderUtility.EndPreview();

            return output;
        }

        
        // Summary
        //      Streamlined drawing setup for tiles
        private bool DrawMeshPreview(Mesh mesh, Material material, Quaternion orientation) {
            if(mesh == null || material == null) return false;

            bool oldFog = RenderSettings.fog;
            Unsupported.SetRenderSettingsUseFogNoDirty(false);

            _renderUtility.DrawMesh(mesh, Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one), material, 0);

            Bounds bounds = mesh.bounds;
            float halfSize = Mathf.Max(bounds.extents.magnitude, 0.0001f);
            float distance = halfSize * 7.6f;

            Quaternion rot = orientation;
            Vector3 pos = bounds.center - rot * (Vector3.forward * distance);

            _renderUtility.camera.transform.position = pos;
            _renderUtility.camera.transform.rotation = rot;
            _renderUtility.camera.nearClipPlane = distance - halfSize * 1.1f;
            _renderUtility.camera.farClipPlane = distance + halfSize * 1.1f;
        
            _renderUtility.lights[0].intensity = .7f;
            _renderUtility.lights[0].transform.rotation = rot * Quaternion.Euler(40f, 40f, 0);
            _renderUtility.lights[1].intensity = .7f;
            _renderUtility.lights[1].transform.rotation = rot * Quaternion.Euler(340, 218, 177);

            _renderUtility.ambientColor = new Color(.1f, .1f, .1f, 0);

            _renderUtility.Render(true);

            Unsupported.SetRenderSettingsUseFogNoDirty(oldFog);

            return true;
        }

    }
}
