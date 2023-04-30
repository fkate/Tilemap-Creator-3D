// Copyright (c) 2023 Felix Kate. BSD-3 license (see included license file)
// Class to show how to manipulate the tilemap at runtime

using System.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace TilemapCreator3D.Samples {
    public class BuildingSelector : MonoBehaviour {

        private const byte WATER_INDEX = 1;

        private static ParticleSystem _sharedEffect;

        [SerializeField] private Mesh _selectionMesh;
        [SerializeField] private Material _selectionMaterial;
        [SerializeField] private ParticleSystem _explosionEffect;
        [SerializeField] private int _explosionRadius = 2;

        private bool _selected;

        public void Update() {
            if(_selected != false) {
                Transform trs = transform;
                Vector3 scale = Vector3.one * (1.0f + Mathf.Sin(Time.timeSinceLevelLoad * 4.0f) * 0.05f);

                Graphics.DrawMesh(_selectionMesh, Matrix4x4.TRS(trs.position, trs.rotation, scale), _selectionMaterial, gameObject.layer);

                if(Input.GetKeyDown(KeyCode.Delete)) {
                    StartCoroutine("Explode");
                    _selected = false;
                }
            }
        }

        private IEnumerator Explode() {
            // Create single reusable effect instance and explode it at position
            if(_sharedEffect == null) {
                _sharedEffect = Instantiate(_explosionEffect.gameObject).GetComponent<ParticleSystem>();
            }

            ParticleSystem.MainModule mainMod = _sharedEffect.main;
            mainMod.startSizeMultiplier = _explosionRadius * 2 + 2;

            _sharedEffect.transform.position = transform.position;
            if(!_sharedEffect.isPlaying) _sharedEffect.Play();
            _sharedEffect.Emit(1);

            yield return new WaitForSeconds(0.5f);

            // Go over explosion area in tilemap and set ids (ground floor covered in water everything else clear)
            Tilemap3D map = GetComponentInParent<Tilemap3D>();
            if(map != null) {
                int2 mapCoordinate = map.WorldToGrid(transform.position).xz;

                int2 min = mapCoordinate - _explosionRadius;
                int2 max = mapCoordinate + _explosionRadius;

                Box3D area = new Box3D(new int3(min.x, 0, min.y), new int3(max.x, map.Height - 1, max.y));                        

                area.ForEach((int3 pos) => {
                    if(!map.InBounds(pos) || math.length(new int2(pos.xz - mapCoordinate)) > _explosionRadius + 0.5f) return;

                    map.Data[pos] = new TilemapData.Tile { id = (pos.y == 0 ? WATER_INDEX : (byte) 0) };
                });

                // Call map methods for update (warning: rebuilding mesh at runtime needs models to be read/write enabled)
                map.PostProcessTiles(area);
                map.BakeDynamic();
            }
        }

        public void OnMouseDown() {
            _selected = !_selected;
        }

    }
}