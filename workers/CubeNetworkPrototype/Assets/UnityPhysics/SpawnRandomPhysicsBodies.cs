using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

public static class Settings
{
    public static float3 spaceRadius = new float3(50, 50, 50);
}

public struct RotateDir : IComponentData
{
    public bool clockwise;
    public float speed;
}

public struct CubeColor : IComponentData
{
    public bool isRed;
}

public struct CubeColorChanged : IComponentData
{

}

public class SpawnRandomPhysicsBodies : MonoBehaviour
{
    public GameObject prefab;
    public int count;

    public static void RandomPointsOnCircle(Unity.Mathematics.Random random,
        float3 center,
        ref NativeArray<float3> positions,
        ref NativeArray<quaternion> rotations)
    {
        var count = positions.Length;
        // initialize the seed of the random number generator 

        for (int i = 0; i < count; i++)
        {
            positions[i] = center + random.NextFloat3(-Settings.spaceRadius, Settings.spaceRadius);
            rotations[i] = random.NextQuaternionRotation();
        }
    }

    private void Start()
    {
        if (!enabled) return;

        Entity sourceEntity = GameObjectConversionUtility.ConvertGameObjectHierarchy(prefab, World.Active);
        var entityManager = World.Active.EntityManager;

        Unity.Mathematics.Random random = new Unity.Mathematics.Random();
        random.InitState(10);

        var positions = new NativeArray<float3>(count, Allocator.Temp);
        var rotations = new NativeArray<quaternion>(count, Allocator.Temp);
        RandomPointsOnCircle(random, transform.position, ref positions, ref rotations);

        var clockwiseQuat = quaternion.RotateY(90.0f);
        var anticlockwiseQuat = quaternion.RotateY(-90.0f);

        var sourceCollider = entityManager.GetComponentData<PhysicsCollider>(sourceEntity).Value;
        for ( int i=0; i<count; ++i )
        {
            var instance = entityManager.Instantiate(sourceEntity);
            entityManager.SetComponentData(instance, new Translation{ Value = positions[i] });
            entityManager.SetComponentData(instance, new Rotation { Value = rotations[i] });
            entityManager.SetComponentData(instance, new PhysicsCollider { Value = sourceCollider });

            var toCenter = float3.zero - positions[i];
            toCenter.y = 0.0f;

            bool isClockwise = random.NextBool();

            var toCenterDir = math.normalize(toCenter);
            var rotDir = math.rotate(true ? clockwiseQuat : anticlockwiseQuat, toCenterDir);
            rotDir = math.normalize(toCenterDir);
            var speedValue = random.NextFloat(3f, 10f);

            entityManager.AddComponent<RotateDir>(instance);
            entityManager.SetComponentData(instance, new RotateDir
            {
                clockwise = isClockwise,
                speed = speedValue
            });
        }

        positions.Dispose();
        rotations.Dispose();
    }
}
