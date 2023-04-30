// Copyright (c) 2023 Felix Kate. BSD-3 license (see included license file)

using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace TilemapCreator3D {
    // Summary
    //      Serializable struct for calculating attributes and stride for a raw mesh combine job
    [System.Serializable]
    public struct RawMeshCombineSettings {
        public bool Normal;
        public bool Tangent;
        public bool Color;
        public bool UV0;
        public bool UV1;

        // Amount of floats per attribute
        private int positionOffset => 3;
        private int normalOffset => 3;
        private int tangentOffset => 4;
        private int colorOffset => 1;
        private int texcoordOffset => 2;

        public int Stride => (positionOffset) + (Normal ? normalOffset : 0) + (Tangent ? tangentOffset : 0) + (Color ? colorOffset : 0) + (UV0 ? texcoordOffset : 0) + (UV1 ? texcoordOffset : 0);
            
        public NativeArray<VertexAttributeDescriptor> AttributeDescriptors(Allocator allocator) {
            int attrCount = 1 + (Normal ? 1 : 0) + (Tangent ? 1 : 0) + (Color ? 1 : 0) + (UV0 ? 1 : 0) + (UV1 ? 1 : 0);
            int attrIndex = 0;

            NativeArray<VertexAttributeDescriptor> descriptors = new NativeArray<VertexAttributeDescriptor>(attrCount, allocator, NativeArrayOptions.UninitializedMemory);
            descriptors[attrIndex++] = new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3, 0);
            if(Normal) descriptors[attrIndex++] = new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3, 0);
            if(Tangent) descriptors[attrIndex++] = new VertexAttributeDescriptor(VertexAttribute.Tangent, VertexAttributeFormat.Float32, 4, 0);
            if(Color) descriptors[attrIndex++] = new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.UNorm8, 4, 0);
            if(UV0) descriptors[attrIndex++] = new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2, 0);
            if(UV1) descriptors[attrIndex++] = new VertexAttributeDescriptor(VertexAttribute.TexCoord1, VertexAttributeFormat.Float32, 2, 0);

            return descriptors;
        }

    }


    // Summary
    //      Struct holding temporary mesh information / similar to CombineInstance
    public struct RawMeshData : System.IComparable<RawMeshData> {
        public Mesh.MeshData Data;
        public float4x4 Transform;

        public int PositionStride;
        public int NormalStride;
        public int TangentStride;
        public int ColorStride;
        public int UV0Stride;
        public int UV1Stride;

        public int VertexCount;
        public int IndexCount;
        public int SubMeshIndex;

        public RawMeshData(Mesh mesh, int subMesh, float3 offset, quaternion rotation, float3 scale) {
            Data = Mesh.AcquireReadOnlyMeshData(mesh)[0];

            Transform = float4x4.TRS(offset, rotation, scale);

            SubMeshIndex = subMesh;

            PositionStride = -1;
            NormalStride = -1;
            TangentStride = -1;
            ColorStride = -1;
            UV0Stride = -1;
            UV1Stride = -1;

            int stride = 0;

            for(int i = 0; i < mesh.vertexAttributeCount; i++) {
                VertexAttributeDescriptor attribute = mesh.GetVertexAttribute(i);
                int stream = attribute.stream;

                // Only read from first stream (I would assume imported meshes only use the first)
                if(stream != 0) continue;

                // Cannot lay out in array since nested arrays are not allowed in jobs
                if(attribute.attribute == VertexAttribute.Position) PositionStride = stride;
                else if(attribute.attribute == VertexAttribute.Normal) NormalStride = stride;
                else if(attribute.attribute == VertexAttribute.Tangent) TangentStride = stride;
                else if(attribute.attribute == VertexAttribute.Color) ColorStride = stride;
                else if(attribute.attribute == VertexAttribute.TexCoord0) UV0Stride = stride;
                else if(attribute.attribute == VertexAttribute.TexCoord1) UV1Stride = stride;

                // Stride from bytes to floats (should work with imported meshes but might break with custom ones using weird formats)
                stride += GetStride(attribute) / 4;
            }

            VertexCount = mesh.vertexCount;
            IndexCount = (int) mesh.GetIndexCount(0);
        }

        public float3 GetPosition(NativeArray<float> rawData, int vertexStride) {
            vertexStride += PositionStride;                
            return PositionStride >= 0 ? math.mul(Transform, new float4(rawData[vertexStride], rawData[vertexStride + 1], rawData[vertexStride + 2], 1)).xyz : new float3(0, 0, 0); 
        }

        public float3 GetNormal(NativeArray<float> rawData, int vertexStride) {
            vertexStride += NormalStride;                
            return NormalStride >= 0 ? math.mul(Transform, new float4(rawData[vertexStride], rawData[vertexStride + 1], rawData[vertexStride + 2], 0)).xyz : new float3(0, 0, 0); 
        }

        public float4 GetTangent(NativeArray<float> rawData, int vertexStride) {
            vertexStride += TangentStride;
            return TangentStride >= 0 ? new float4(math.mul(Transform, new float4(rawData[vertexStride], rawData[vertexStride + 1], rawData[vertexStride + 2], 0)).xyz, rawData[vertexStride + 3]) : new float4(0, 0, 0, 0); // Seems to be right?
        }

        public float GetColor(NativeArray<float> rawData, int vertexStride) {
            vertexStride += ColorStride;                
            return ColorStride >= 0 ? rawData[vertexStride] : 0; 
        }

        public float2 GetUV0(NativeArray<float> rawData, int vertexStride) {
            vertexStride += UV0Stride;                
            return UV0Stride >= 0 ? new float2(rawData[vertexStride], rawData[vertexStride + 1]) : new float2(0, 0); 
        }

        public float2 GetUV1(NativeArray<float> rawData, int vertexStride) {
            vertexStride += UV1Stride;                
            return UV1Stride >= 0 ? new float2(rawData[vertexStride], rawData[vertexStride + 1]) : new float2(0, 0); 
        }

        public float4 CalculateTangent(float3 normal) {
            float3 t0 = math.cross(normal, new float3(0, 0, 1));
            float3 t1 = math.cross(normal, new float3(0, 1, 0));

            if(math.length(t0) > math.length(t1)) return new float4(t0, 0);
            else return new float4(t1, 0);
        }

        // Summary
        //      Get attribute stride in bytes
        private static int GetStride(VertexAttributeDescriptor attribute) {
            VertexAttributeFormat format = attribute.format;
            int dimensions = attribute.dimension;

            switch(format) {
                case VertexAttributeFormat.Float32:
                    return 4 * dimensions;
                case VertexAttributeFormat.Float16:
                    return 2 * dimensions;
                case VertexAttributeFormat.UNorm16:
                    return 2 * dimensions;
                case VertexAttributeFormat.SNorm16:
                    return 2 * dimensions;
                case VertexAttributeFormat.UInt16:
                    return 2 * dimensions;
                case VertexAttributeFormat.SInt16:
                    return 2 * dimensions;
                case VertexAttributeFormat.UInt32:
                    return 2 * dimensions;
                case VertexAttributeFormat.SInt32:
                    return 2 * dimensions;
                default:
                    return dimensions;
            }
        }

        public int CompareTo(RawMeshData b) {
            return SubMeshIndex.CompareTo(b.SubMeshIndex);
        }

    }


    // Summary
    //      Job for processing all tile copies in parallel / could make it using Burst but it's not a performance bottleneck at the moment
    public struct RawMeshCombineJob : IJobParallelFor {
        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public struct Vertex {
            public float3 Position;
            public float3 Normal;
            public Color32 Color;
            public float2 Texcoord0;
            public float2 Texcoord1;
        }

        [ReadOnly] public RawMeshCombineSettings Settings;
        [ReadOnly] public NativeList<RawMeshData> Data;
        [ReadOnly] public NativeArray<int2> Offsets;
        [ReadOnly] public int VertexStride;

        // Use floats instead of bytes for a) less array lookups and b) because we need to transform vectors
        [NativeDisableParallelForRestriction] public NativeArray<float> VertexElements;
        [NativeDisableParallelForRestriction] public NativeArray<int> Indices;

        public RawMeshCombineJob(RawMeshCombineSettings settings, NativeList<RawMeshData> data, NativeArray<int2> offsets, int vertexCount, int indexCount) {
            Settings = settings;
            Data = data;
            Offsets = offsets;
            VertexStride = settings.Stride;
            VertexElements = new NativeArray<float>(vertexCount * VertexStride, Allocator.TempJob);
            Indices = new NativeArray<int>(indexCount, Allocator.TempJob);
        }

        public void Execute(int i) {
            RawMeshData current = Data[i];
            int2 offset = Offsets[i];
                
            Mesh.MeshData mData = Data[i].Data;

            // Grab current tiles mesh information
            NativeArray<float> rawData = mData.GetVertexData<float>();
            int dataStride = mData.GetVertexBufferStride(0) / 4;
            NativeArray<ushort> indices = mData.GetIndexData<ushort>();

            // Copy vertex information
            for(int vert = 0; vert < current.VertexCount; vert++) {
                int vertPos = (offset.x + vert) * VertexStride;
                int rawStride = vert * dataStride;

                float3 position = current.GetPosition(rawData, rawStride);
                VertexElements[vertPos++] = position.x;
                VertexElements[vertPos++] = position.y;
                VertexElements[vertPos++] = position.z;

                if(Settings.Normal) {
                    float3 normal = current.GetNormal(rawData, rawStride);
                        
                    VertexElements[vertPos++] = normal.x;
                    VertexElements[vertPos++] = normal.y;
                    VertexElements[vertPos++] = normal.z;
                }

                if(Settings.Tangent) {
                    float4 tangent = current.GetTangent(rawData, rawStride);

                    VertexElements[vertPos++] = tangent.x;
                    VertexElements[vertPos++] = tangent.y;
                    VertexElements[vertPos++] = tangent.z;
                    VertexElements[vertPos++] = tangent.w;
                }

                if(Settings.Color) {
                    float compactColor = current.GetColor(rawData, rawStride);
                        
                    VertexElements[vertPos++] = compactColor;
                }
                    
                if(Settings.UV0) {
                    float2 uv0 = current.GetUV0(rawData, rawStride);                        

                    VertexElements[vertPos++] = uv0.x;
                    VertexElements[vertPos++] = uv0.y;
                }
     
                if(Settings.UV1) {
                    float2 uv1 = current.GetUV1(rawData, rawStride);                        

                    VertexElements[vertPos++] = uv1.x;
                    VertexElements[vertPos++] = uv1.y;
                }

            }

            // Copy index information
            for(int ind = 0; ind < current.IndexCount; ind++) {
                int indPos = offset.y + ind;

                Indices[indPos] = indices[ind] + offset.x;
            }

            // Clean up
            indices.Dispose();
        }

        public void Finalize(Mesh mesh, NativeArray<int2> subMeshes) {
            mesh.Clear();

            NativeArray<VertexAttributeDescriptor> attributes = Settings.AttributeDescriptors(Allocator.Temp);

            int vertexCount = VertexElements.Length / VertexStride;
            int indexCount = Indices.Length;

            mesh.SetVertexBufferParams(vertexCount, attributes);
            mesh.SetVertexBufferData(VertexElements, 0, 0, VertexElements.Length, 0, MeshUpdateFlags.DontRecalculateBounds);

            mesh.SetIndexBufferParams(indexCount, IndexFormat.UInt32);

            // Process sub mesh splits
            mesh.subMeshCount = subMeshes.Length;
            mesh.SetIndexBufferData(Indices, 0, 0, indexCount);

            mesh.subMeshCount = subMeshes.Length;
            for(int i = 0; i < subMeshes.Length; i++) {
                mesh.SetSubMesh(i, new SubMeshDescriptor(subMeshes[i].x, subMeshes[i].y), MeshUpdateFlags.DontRecalculateBounds);
            }

            // Clean up
            attributes.Dispose();
            VertexElements.Dispose();
            Indices.Dispose();
        }

    }
}
    
