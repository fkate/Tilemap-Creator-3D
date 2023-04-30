// Copyright (c) 2023 Felix Kate. BSD-3 license (see included license file)
// Example code for a simple camera controller that only moves when the player is outside it's focus bounds

using Unity.Mathematics;
using UnityEngine;

namespace TilemapCreator3D.Samples {
    public class SimpleCameraController : MonoBehaviour {
    
        public Camera Camera;
        public Transform Target;
        public float3 FocusBounds = new float3(5, 5, 5);
        public float CameraSpeed = 2.5f;

        public bool LimitCamera = false;
        public Bounds CameraBounds = new Bounds(Vector3.zero, Vector3.one);
        
        public void LateUpdate() {
            if(Target == null) return;

            float dt = Time.deltaTime;

            Transform trs = transform;

            float3 position = trs.position;
            quaternion rotation = trs.rotation;
            float3 globalTarget = Target.position;
            float3 localTarget = trs.InverseTransformPoint(globalTarget);
            float3 halfBounds = FocusBounds * 0.5f;

            float3 absTarget = math.abs(localTarget);

            // If player is out of focus move camera anchor towards it
            if(absTarget.x > halfBounds.x) position += math.mul(rotation, new float3(math.sign(localTarget.x) * absTarget.x, 0, 0));
            if(absTarget.y > halfBounds.y) position += math.mul(rotation, new float3(0, math.sign(localTarget.y) * absTarget.y, 0));
            if(absTarget.z > halfBounds.z) position += math.mul(rotation, new float3(0, 0, math.sign(localTarget.z) * absTarget.z));

            if(LimitCamera) {
                position = math.clamp(position, CameraBounds.min, CameraBounds.max);
            }

            trs.position = Vector3.MoveTowards(trs.position, position, CameraSpeed * dt);
        }


        public void OnDrawGizmosSelected() {
            if(LimitCamera) {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireCube(CameraBounds.center, CameraBounds.size);
            }

            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = Color.white;

            Gizmos.DrawWireCube(new float3(0, 0, 0), FocusBounds);
        }

    }
}