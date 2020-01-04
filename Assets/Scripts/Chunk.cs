using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Jobs;

[RequireComponent(typeof(MeshFilter)), RequireComponent(typeof(MeshCollider))]
public class Chunk : MonoBehaviour
{
    public int size;
    
    public Vector3 noisePosition;
    public float noiseAmplitude;
    public float noiseFrequency;
    public float threshold;

    public ComputeShader shader;

    public bool isBufferInitialized { get; private set; }

    private ComputeBuffer trianglesBuffer;
    private ComputeBuffer pointsBuffer;
    private NativeArray<BuffPoint> points;

    private Mesh mesh;
    private MeshFilter meshFilter;
    private MeshCollider meshCollider;

    public struct BuffTriangle
    {
        public Vector3 a, b, c;

        public BuffTriangle(Vector3 a, Vector3 b, Vector3 c)
        {
            this.a = a;
            this.b = b;
            this.c = c;
        }
    }

    public struct BuffPoint
    {
        public Vector3 pos;
        public float value;

        public BuffPoint(Vector3 pos, float value)
        {
            this.pos = pos;
            this.value = value;
        }
    }

    private void Start()
    {
        mesh = new Mesh();
        meshCollider = GetComponent<MeshCollider>();
        meshFilter = GetComponent<MeshFilter>();

        meshFilter.mesh = mesh;
        meshCollider.sharedMesh = mesh;

        var pointsAmount = size * size * size;

        pointsBuffer = new ComputeBuffer(pointsAmount, sizeof(float) * 4, ComputeBufferType.Structured);

        trianglesBuffer = new ComputeBuffer(pointsAmount * 5, sizeof(float) * 3 * 3, ComputeBufferType.Append);
        trianglesBuffer.SetCounterValue(0);

        isBufferInitialized = true;

        points = new NativeArray<BuffPoint>(pointsAmount, Allocator.Persistent);

        Build();
    }

    private void Update()
    {
        Vector3 dir = Vector3.zero;

        if(Input.GetKey(KeyCode.W)) dir.z =  1;
        if(Input.GetKey(KeyCode.A)) dir.x = -1;
        if(Input.GetKey(KeyCode.S)) dir.z = -1;
        if(Input.GetKey(KeyCode.D)) dir.x =  1;

        if(dir != Vector3.zero)
        {
            noisePosition += dir;
            Build();
        }
    }

    private int GetBufferCount(ComputeBuffer buffer)
    {
        var triCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
        ComputeBuffer.CopyCount(buffer, triCountBuffer, 0);
        int[] triCountArray = { 0 };
        triCountBuffer.GetData(triCountArray);
        triCountBuffer.Dispose();
        return triCountArray[0];
    }

    private void Build()
    {
        new PointJob()
        {
            size        = new int3(size),
            position    = new float3(noisePosition),
            frequency   = noiseFrequency,
            amplitude   = noiseAmplitude,
            points      = points,
        }
        .Schedule(points.Length, points.Length / SystemInfo.processorCount)
        .Complete();
        
        pointsBuffer.SetData(points);
        trianglesBuffer.SetCounterValue(0);

        int k = shader.FindKernel("MarchingCube");

        shader.SetFloat("threshold", threshold);
        shader.SetInts("chunkSize", size, size, size);
        shader.SetBuffer(k, "bufferPoints", pointsBuffer);
        shader.SetBuffer(k, "bufferTriangles", trianglesBuffer);

        var th = Mathf.CeilToInt(size / 8f);

        shader.Dispatch(k, th, th, th);

        int trisCount = GetBufferCount(trianglesBuffer);

        BuffTriangle[] triangles = new BuffTriangle[trisCount];
        trianglesBuffer.GetData(triangles, 0, 0, trisCount);

        var vertices = new NativeArray<Vector3>(trisCount * 3, Allocator.Temp);
        var indices = new NativeArray<int>(trisCount * 3, Allocator.Temp);

        System.Threading.Tasks.Parallel.For(0, trisCount, (i) => {
            int index = i * 3;

            vertices[index] = triangles[i].a;
            vertices[index + 1] = triangles[i].b;
            vertices[index + 2] = triangles[i].c;

            indices[index] = index;
            indices[index + 1] = index + 1;
            indices[index + 2] = index + 2;
        });

        mesh.Clear();

        mesh.SetVertices<Vector3>(vertices);
        mesh.SetIndices<int>(indices, MeshTopology.Triangles, 0);

        vertices.Dispose();
        indices.Dispose();

        mesh.RecalculateNormals();
        
        // If you uncommented this two lines you divide by 2 CPU performance !!! 
        // meshCollider.sharedMesh = null;
        // meshCollider.sharedMesh = mesh;
    }

    private void OnDestroy()
    {
        if(isBufferInitialized)
        {
            trianglesBuffer.Dispose();
            pointsBuffer.Dispose();
        }

        points.Dispose();
    }

    public void SetFrequency(float f)
    {
        this.noiseFrequency = f;
        Build();
    }

    public void SetAmplitude(float a)
    {
        this.noiseAmplitude = a;
        Build();
    }
}
