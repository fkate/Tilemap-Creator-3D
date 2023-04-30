// Copyright (c) 2023 Felix Kate. BSD-3 license (see included license file)
// Example code for a simple box based 2D character using raw physic scripts instead of RigidBody
// Warning: Due to focusing on a box the physics won't take rotation into account

using System.Collections;
using Unity.Burst.CompilerServices;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;

namespace TilemapCreator3D.Samples {
    public class SimplePlatformerController : MonoBehaviour {

        [Header("References")]
        [SerializeField] private SpriteRenderer _sprite;
        [SerializeField] private Animator _animator;
        [SerializeField] private BoxCollider _hitBox;
        [SerializeField] private Camera _camera;
        [SerializeField] private Transform _fakeShadow;
        [SerializeField] private TilemapNavigator _navigator;
        [SerializeField] private ParticleSystem _outOfBoundsEffect;

        [Header("Values")]
        [SerializeField] private LayerMask _collisionLayers;
        [SerializeField] private LayerMask _platformLayers;
        [SerializeField] private bool _is2D = true;
        [SerializeField] private bool _useAI = false;
        [SerializeField] private float _speedGround = 3.0f;
        [SerializeField] private float _speedAir = 3.0f;
        [SerializeField] private float _accelerationGround = 24.0f;
        [SerializeField] private float _accelerationAir = 12.0f;
        [SerializeField] private float _jumpForce = 4.5f;
        [SerializeField] private float _gravity = 9.0f;

        [Space]
        [SerializeField] private float _stepHeight = 0.1f;
        [SerializeField] private float _spriteTilt = 0.0f;
        [SerializeField] private float _fakeShadowOffset = 0.01f;
        [SerializeField] private float _navPointThreshold = 0.5f;

        private float2 _input;
        private float3 _velocity;
        private float _jumpEnergy;
        private float3 _safeSpot;
        private float _coyoteTime;
        private bool _grounded;

        private Collider[] _overlaps;
        private Quaternion _spriteRotation;

        private NavMeshPath _path;
        private Vector3[] _corners;
        private bool[] _offLinks;
        private int _pathNode;
        private bool _tryJump;

        private bool _initialized;


        private IEnumerator Start() {
            Transform trs = transform;
            trs.rotation = Quaternion.identity;
            
            _overlaps = new Collider[4];
            _spriteRotation = Quaternion.identity;

            _corners = new Vector3[0];
            _offLinks = new bool[0];
            
            _safeSpot = transform.position;

            // Add delay to prevent falling through the ground on first frame
            yield return new WaitForSeconds(0.1f);
            
            _initialized = true;

            // Disable when component is missing
            if(_animator == null || _hitBox == null || _sprite == null) enabled = false;
        }

        private void Update() {
            if(!_initialized) return;

            float dt = Time.deltaTime;
            Transform trs = transform;
                        
            HandleInput(trs, dt);
            HandleMovement(trs, dt);
            HandleVisuals(trs, dt);            
        }


        private void FixedUpdate() {
            if(!_initialized) return;

            HandlePhysics();
        }


        private void HandleInput(Transform trs, float dt) {
            // Manual input
            if(_is2D || !_useAI) {
                _input.x = Input.GetAxis("Horizontal");
                _input.y = Input.GetAxis("Vertical");

                // Cull out low results to counter Unity standard settings
                _input.x = math.abs(_input.x) > 0.25f ? math.sign(_input.x) : 0;
                _input.y = math.abs(_input.y) > 0.25f ? math.sign(_input.y) : 0;

                if(_coyoteTime > 0) {
                    _coyoteTime = math.max(_coyoteTime - 4 * Time.deltaTime, 0);

                    // Charged jump for variable jump height
                    if(Input.GetButton("Jump")) _jumpEnergy = math.min(_jumpEnergy + 4.0f * dt, 1.0f);
                    else if(Input.GetButtonUp("Jump")) _velocity.y = math.max(_jumpEnergy * _jumpForce, _jumpForce * 0.5f);
                } else {
                    _jumpEnergy = 0;
                }            
            } else {
                // AI find target
                if(_camera != null && Input.GetMouseButtonDown(0)) {
                    Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
                    RaycastHit hit;

                    if(Physics.Raycast(ray, out hit, 100.0f)) {
                        if(_path == null) _path = new NavMeshPath();
                        bool checkLinks = _navigator != null;

                        NavMeshHit sourceHit;

                        if(NavMesh.SamplePosition(transform.position, out sourceHit, 5.0f, NavMesh.AllAreas) && NavMesh.CalculatePath(sourceHit.position, hit.point, NavMesh.AllAreas, _path)) {
                            _corners = _path.corners;

                            if(checkLinks) {
                                _offLinks = new bool[_corners.Length];

                                for(int i = 0; i <_path.corners.Length - 1; i++) {             
                                    _offLinks[i + 1] = _navigator.IsOfflink(_path.corners[i], _path.corners[i + 1]);
                                }    
                            }
                            
                            _pathNode = 0;
                            _tryJump = false;
                        }
                    }
                }

                // Try navigating path
                if(_pathNode < _corners.Length) {
                    Vector3 source = trs.position;
                    Vector3 target = _corners[_pathNode];

                    _input = new float2(target.x - source.x, target.z - source.z);
                    if(math.length(_input) > 0.1f) _input = math.normalize(_input);
                    else _input = 0;

                    if(Vector3.Distance(source, target) < _navPointThreshold) {
                        _pathNode++;
                        if(_pathNode < _offLinks.Length && _offLinks[_pathNode]) _tryJump = true;
                        else _tryJump = false;
                    }

                    if(_grounded && _tryJump) _velocity.y = _jumpForce;
                } else {
                    _input = 0;
                }
            }
        }

        private void HandleMovement(Transform trs, float dt) {
            // Get acceleration for input
            float2 acceleration = 0;            
            if(_is2D && (!_grounded || _input.y >= 0)) acceleration.x += _input.x;
            else if(!_is2D && math.length(_input) > 0) {
                acceleration.xy += math.normalize(_input.xy);

                // Transform to camera orientation
                if(_camera != null) {
                    float2 cforward = math.normalize(((float3) _camera.transform.forward).xz);
                    
                    float angle = -Mathf.Atan2(cforward.x, cforward.y);

                    acceleration = new float2(
                        acceleration.x * math.cos(angle) - acceleration.y * math.sin(angle),
                        acceleration.x * math.sin(angle) + acceleration.y * math.cos(angle)
                    );
                }
            }
            acceleration.xy *= (_grounded ? _speedGround : _speedAir);

            // Update velocity
            _velocity.xz = MoveTowards(_velocity.xz, acceleration.xy, (_grounded ? _accelerationGround : _accelerationAir) * dt);
            _velocity.y = math.max(_velocity.y - _gravity * dt, -_gravity);

            transform.position = (float3) transform.position + _velocity * dt;
        }


        private void HandleVisuals(Transform trs, float dt) {
            _animator.SetBool("Grounded", _grounded);
            _animator.SetFloat("Velocity", math.length(_velocity.xz));

            if(_is2D) {
                if(math.abs(_velocity.x) > 0) _sprite.flipX = _velocity.x < 0;
                _animator.SetBool("Crouch", _grounded && _input.y < 0);

            } else {
                if(math.length(_velocity.xz) > 0.25f) _spriteRotation = Quaternion.LookRotation(new Vector3(_velocity.x, 0, _velocity.z));
                _sprite.transform.rotation = _camera != null ? Quaternion.AngleAxis(_spriteTilt, _camera.transform.right) * _spriteRotation : _spriteRotation;

            }

            // Handle fake shadow if needed
            if(_fakeShadow != null) {
                Ray ray = new Ray(trs.position + _hitBox.center, new Vector3(0, -1, 0));
                RaycastHit hit;

                if(Physics.Raycast(ray, out hit, 10.0f, _collisionLayers)) {
                    _fakeShadow.gameObject.SetActive(true);
                        float3 shadowPos = _fakeShadow.position;
                    shadowPos.y = hit.point.y + _fakeShadowOffset;

                    _fakeShadow.position = shadowPos;
                } else {
                    _fakeShadow.gameObject.SetActive(false);
                }
            }
        }


        private void HandlePhysics() {
            _grounded = false;

            // Get relevant data for physic calculation
            Transform trs = transform;
            float3 position = trs.position;
            quaternion orientation = trs.rotation;

            float3 boxOffset = _hitBox.center;
            float3 boxSize = _hitBox.size;
            float3 halfSize = boxSize * 0.5f;
            float3 stepOffset = new float3(0, _stepHeight, 0);

            // Overlap check and push out at target position in two steps
            Vector3 pushDirection;
            float pushDistance;

            // Check if can stand on platform            
            LayerMask upDownLayers = _collisionLayers;
            if(_is2D && _velocity.y < 0 && _input.y >= 0) upDownLayers |= _platformLayers;

            RaycastHit hit;
            Collider col;
            Transform colTrs;

            Debug.DrawRay(position, stepOffset, Color.red);

            // Collider resolving
            int overlapCount = Physics.OverlapBoxNonAlloc(position + boxOffset + stepOffset, halfSize - stepOffset * 0.25f, _overlaps, orientation, _collisionLayers);

            for(int i = 0; i < overlapCount; i++) {
                col = _overlaps[i];
                colTrs = col.transform;

                if(Physics.ComputePenetration(_hitBox, position, orientation, col, colTrs.position, colTrs.rotation, out pushDirection, out pushDistance)) {
                    float3 push = pushDirection * (pushDistance + 0.001f); // Small offset to avoid hanging on cliffs

                    position.xz += push.xz;

                    // Reset velocity to prevent visual set backs
                    if(math.abs(push.x) > 0) _velocity.x = 0;
                    if(math.abs(push.z) > 0) _velocity.z = 0;

                    // Hit ceeling
                    if(_velocity.y > 0 && push.y < 0) {
                        position.y += push.y;
                        _velocity.y = 0;
                    }
                }
            }

            // Floor testing
            if(_velocity.y < 0 && Physics.BoxCast(position + boxOffset + stepOffset, new Vector3(halfSize.x - 0.01f, halfSize.y, halfSize.z - 0.01f), new float3(0, -1, 0), out hit, orientation, _stepHeight, upDownLayers)) {
                col = hit.collider;

                float push = _stepHeight - hit.distance;
                    
                if(push > 0) {
                    _grounded = true;
                    _coyoteTime = 1.0f;
                    position.y += push;
                    _velocity.y = 0;
                }
            }

            // Prevent player from falling out the map
            if(position.y < 0) {
                if(_outOfBoundsEffect != null) {
                    _outOfBoundsEffect.transform.position = position;
                    _outOfBoundsEffect.Emit(1);
                }

                // Stop navigation to prevent moving when not on the calculated path
                if(_corners.Length > 0) _corners = new Vector3[0];

                position = _safeSpot;
                _velocity = 0;
            }

            trs.position = position;
        }


        // Since math has not movetowards create a simple method similar to the one in Vector3
        private float2 MoveTowards(float2 source, float2 target, float a) {
            float xAxis = target.x - source.x;
            float yAxis = target.y - source.y;
            float a2 = a * a;
            float accum = xAxis * xAxis + yAxis * yAxis;

            if (accum == 0f || (a >= 0f && accum <= a2)) return target;
            
            float sqrt = (float) math.sqrt(accum);
            return new float2(source.x + xAxis / sqrt * a, source.y + yAxis / sqrt * a);
        }


        // Draw a preview for the ai path
        private void OnDrawGizmosSelected() {
            if(_useAI && _corners != null && _corners.Length > 0) {
                bool useOfflinks = _offLinks.Length == _corners.Length;
                Vector3 last = _corners[0];

                for(int i = 1; i < _corners.Length; i++) {
                    Vector3 point = _corners[i];
                    
                    Gizmos.color = (useOfflinks && _offLinks[i]) ? Color.red : Color.yellow;
                    Gizmos.DrawLine(last, _corners[i]);
                    
                    last = point;
                }
            }
        }

    }
}
