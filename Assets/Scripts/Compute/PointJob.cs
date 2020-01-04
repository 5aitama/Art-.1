using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Jobs;

[BurstCompile]
public struct PointJob : IJobParallelFor
{
    [ReadOnly]
    public int3 size;

    [ReadOnly]
    public float3 position;

    [ReadOnly]
    public float frequency;

    [ReadOnly]
    public float amplitude;

    [WriteOnly]
    public NativeArray<Chunk.BuffPoint> points;

    public void Execute(int index)
    {
        float3 localPos = to3D(index);
        float3 worldPos = position + localPos;

        var p = new Chunk.BuffPoint();
        p.pos = localPos;
        p.value = GetNoise(worldPos, localPos);

        points[index] = p;
    }

    public float GetNoise(float3 worldPos, float3 localPos)
    {
        if(localPos.x <= 1 || localPos.y <= 1 || localPos.z <= 1) return -1f;
        if(localPos.x >= size.x - 1 || localPos.y >= size.y - 1 || localPos.z >= size.z - 1) return -1f;

        float n = noise.snoise(worldPos * frequency);
        n = (n + 1f) / 2f;
        n *= amplitude;

        return -worldPos.y + n;
    }

    public float3 to3D(int i)
    {
        float x = i % (float)size.x;
        float y = (i / (float)size.x) % (float)size.y;
        float z = i / ((float)size.x * (float)size.y);

        return new float3(x, y, z);
    }
}