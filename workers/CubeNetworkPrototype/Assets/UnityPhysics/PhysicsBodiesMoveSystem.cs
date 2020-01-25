using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

//[DisableAutoCreation]
//[UpdateAfter(typeof())]
public class PhysicsBodiesMoveSystem : JobComponentSystem
{
    EndSimulationEntityCommandBufferSystem m_EntityCommandBufferSystem;

    BuildPhysicsWorld m_BuildPhysicsWorldSystem;
    StepPhysicsWorld m_StepPhysicsWorldSystem;

    private EntityQuery addPhysicsQuery;
    private EntityQuery physicsQuery;

    private EntityQuery colorChangeQuery;

    private const float gravityChageRadius = 10.0f;

    private quaternion clockwiseQuat = quaternion.RotateY(90.0f);
    private quaternion anticlockwiseQuat = quaternion.RotateY(-90.0f);

    private Unity.Mathematics.Random random = new Unity.Mathematics.Random();


    private RenderMesh redRenderMesh;
    private RenderMesh blueRenderMesh;

    protected override void OnCreate()
    {
        base.OnCreate();

        m_EntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

        m_BuildPhysicsWorldSystem = World.GetOrCreateSystem<BuildPhysicsWorld>();
        m_StepPhysicsWorldSystem = World.GetOrCreateSystem<StepPhysicsWorld>();

        random.InitState(10);


        physicsQuery = GetEntityQuery(
            ComponentType.ReadOnly<PhysicsCollider>(),
            ComponentType.ReadOnly<Translation>(),
            ComponentType.ReadOnly<Rotation>(),
            ComponentType.ReadOnly<RotateDir>()
        );

        colorChangeQuery = GetEntityQuery(
            ComponentType.ReadOnly<CubeColorChanged>()
        );

        CreateWalls();
        CreateCubes();
    }

    private void CreateWalls()
    {
        var prefab = Resources.Load<GameObject>(@"Prefabs/Plane");
        var meshData = prefab.GetComponent<MeshFilter>().sharedMesh;
        var material = prefab.GetComponent<MeshRenderer>().sharedMaterial;

        var meshRender = new RenderMesh()
        {
            mesh = meshData,
            material = material
        };

        var sharedCollider = Unity.Physics.BoxCollider.Create(float3.zero,
            quaternion.identity, new float3(100, 1, 100), 0.05f,
            null,
            new Unity.Physics.Material
            {
                Friction = 0f,
                Restitution = 1f
            }
        );

        CreateStaticBody(meshRender, new float3(0, -50, 0), quaternion.identity, sharedCollider);// down
        CreateStaticBody(meshRender, new float3(0,  50, 0), quaternion.identity, sharedCollider);// up
        CreateStaticBody(meshRender, new float3(-50, 0, 0), Quaternion.AngleAxis(90f, Vector3.forward), sharedCollider);// left
        CreateStaticBody(meshRender, new float3( 50, 0, 0), Quaternion.AngleAxis(90f, Vector3.forward), sharedCollider);// right
        CreateStaticBody(meshRender, new float3(0, 0, -50), Quaternion.AngleAxis(90f, Vector3.right), sharedCollider);// front
        CreateStaticBody(meshRender, new float3(0, 0,  50), Quaternion.AngleAxis(90f, Vector3.right), sharedCollider);// end
    }

    private void CreateCubes()
    {
        var prefab = Resources.Load<GameObject>(@"Prefabs/Cube");
        var meshData = prefab.GetComponent<MeshFilter>().sharedMesh;
        var material = prefab.GetComponent<MeshRenderer>().sharedMaterial;

        material.color = Color.red;
        redRenderMesh = new RenderMesh()
        {
            mesh = meshData,
            material = material
        };

        material.color = Color.blue;
        blueRenderMesh = new RenderMesh()
        {
            mesh = meshData,
            material = material
        };

        var sharedCollider = Unity.Physics.BoxCollider.Create(float3.zero,
            quaternion.identity, new float3(1, 1, 1), 0.05f,
            null, 
            new Unity.Physics.Material {
                Friction = 0f,
                Restitution = 1f
            }
        );

        int count = 10;
        for (int i = 0; i < count; ++i)
        {
            CreateDynamicBody(redRenderMesh, sharedCollider);
        }
    }

    private void CreateStaticBody(
        RenderMesh renderMesh,
        float3 position,
        quaternion orientation,
        BlobAssetReference<Unity.Physics.Collider> collider)
    {
        Entity entity = EntityManager.CreateEntity(new ComponentType[] { });

        EntityManager.AddSharedComponentData(entity, renderMesh);

        EntityManager.AddComponentData(entity, new LocalToWorld { });
        EntityManager.AddComponentData(entity, new Translation { Value = position });
        EntityManager.AddComponentData(entity, new Rotation { Value = orientation });
        EntityManager.AddComponentData(entity, new PhysicsCollider { Value = collider });
    }

    void CreateDynamicBody(RenderMesh renderMesh, BlobAssetReference<Unity.Physics.Collider> collider)
    {
        ComponentType[] compTypes = new ComponentType[10];
        compTypes[0] = typeof(RenderMesh);
        compTypes[1] = typeof(LocalToWorld);
        compTypes[2] = typeof(Translation);
        compTypes[3] = typeof(Rotation);
        compTypes[4] = typeof(PhysicsCollider);
        compTypes[5] = typeof(PhysicsVelocity);
        compTypes[6] = typeof(PhysicsMass);
        compTypes[7] = typeof(PhysicsDamping);
        compTypes[8] = typeof(RotateDir);
        compTypes[9] = typeof(CubeColor);

        var entity = EntityManager.CreateEntity(compTypes);

        EntityManager.SetSharedComponentData(entity, renderMesh);

        float3 halfRange = new float3(50, 50, 50);
        EntityManager.SetComponentData(entity, new Translation{
            Value = random.NextFloat3(-halfRange, halfRange)
        });

        EntityManager.SetComponentData(entity, new Rotation
        {
            Value = quaternion.identity
        });

        var colliderComp = new PhysicsCollider
        {
            Value = collider
        };
        EntityManager.SetComponentData(entity, colliderComp);

        //EntityManager.SetComponentData(entity, new PhysicsVelocity {
        //    Linear = 1f,
        //    Angular = 1f
        //});

        EntityManager.SetComponentData(entity, 
            PhysicsMass.CreateDynamic(colliderComp.MassProperties, 1f)
        );

        EntityManager.SetComponentData(entity, new PhysicsDamping
        {
            Linear = 0.01f,
            Angular = 0.03f
        });

        EntityManager.SetComponentData(entity, new RotateDir
        {
            clockwise = random.NextBool(),
            speed = random.NextFloat(3f, 10f)
        });

        EntityManager.SetComponentData(entity, new CubeColor
        {
            isRed = random.NextBool()
        });
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        //Entities.With(physicsQuery).ForEach(
        //    (Entity entity,
        //     ref PhysicsVelocity phyVel,
        //     ref Translation trans,
        //     ref PhysicsCollider phCol,
        //     ref Rotation rot,
        //     ref RotateDir rotDir) => {

        //         // update physics velocity
        //         MoveForward(ref phyVel, ref rotDir);
        //     }
        //);

        JobHandle moveJobHandle = new MovePhysicsCubeJob
        {
            moveEntities = physicsQuery.ToEntityArray(Allocator.TempJob),
            velocityGroup = GetComponentDataFromEntity<PhysicsVelocity>(),
            rotateDirGroup = GetComponentDataFromEntity<RotateDir>(),
            deltaTime = Time.deltaTime,
            random = random
        }.Schedule(inputDeps);

        JobHandle triggerEventHandle = new TriggerEventEntitiesJob()
        {
            CommandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer(),
            cubeColorGroup = GetComponentDataFromEntity<CubeColor>(),
        }.Schedule(m_StepPhysicsWorldSystem.Simulation,
            ref m_BuildPhysicsWorldSystem.PhysicsWorld,
            moveJobHandle
        );
        m_EntityCommandBufferSystem.AddJobHandleForProducer(triggerEventHandle);
        triggerEventHandle.Complete();

        var cubeColors = GetComponentDataFromEntity<CubeColor>();
        var changedEntities = colorChangeQuery.ToEntityArray(Allocator.TempJob);
        for (int i = 0; i < changedEntities.Length; ++i)
        {
            var entity = changedEntities[i];
            var color = cubeColors[entity];
            EntityManager.SetSharedComponentData(entity, color.isRed ? redRenderMesh : blueRenderMesh);
        }
        changedEntities.Dispose();

        return triggerEventHandle;
    }

    struct MovePhysicsCubeJob : IJob
    {
        [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<Entity> moveEntities;

        public ComponentDataFromEntity<PhysicsVelocity> velocityGroup;
        public ComponentDataFromEntity<RotateDir> rotateDirGroup;
        [ReadOnly] public float deltaTime;
        [ReadOnly] public Unity.Mathematics.Random random;

        public void Execute()
        {
            for ( int i=0; i<moveEntities.Length; ++i )
            {
                var entity = moveEntities[i];
                var phyVel = velocityGroup[entity];
                var rotDir = rotateDirGroup[entity];
                MoveForward(ref phyVel, ref rotDir);
                velocityGroup[entity] = phyVel;
                rotateDirGroup[entity] = rotDir;
            }
        }

        private void MoveForward(ref PhysicsVelocity phyVel, ref RotateDir rotDir)
        {
            var curSpeed = math.length(phyVel.Linear);
            if (curSpeed > 0.0f)
            {
                var curDir = math.normalize(phyVel.Linear);
                phyVel.Linear += curDir * (rotDir.speed - curSpeed) * deltaTime;
            }
            else
            {
                phyVel.Linear = random.NextFloat3();
            }
        }
    }

    struct TriggerEventEntitiesJob : ITriggerEventsJob
    {
        public EntityCommandBuffer CommandBuffer;

        public ComponentDataFromEntity<CubeColor> cubeColorGroup;


        public void Execute(TriggerEvent triggerEvent)
        {
            Entity entityA = triggerEvent.Entities.EntityA;
            Entity entityB = triggerEvent.Entities.EntityB;

            if (cubeColorGroup.Exists(entityA))
            {
                var cubeColor = cubeColorGroup[entityA];
                cubeColor.isRed = !cubeColor.isRed;
                cubeColorGroup[entityA] = cubeColor;

                CommandBuffer.AddComponent<CubeColorChanged>(entityA);
            }

            if (cubeColorGroup.Exists(entityB))
            {
                var cubeColor = cubeColorGroup[entityB];
                cubeColor.isRed = !cubeColor.isRed;
                cubeColorGroup[entityB] = cubeColor;

                CommandBuffer.AddComponent<CubeColorChanged>(entityB);
            }
        }
    }

    private void CircleAroundAxis(ref Translation trans,
        ref PhysicsVelocity phyVel,
        ref RotateDir rotDir)
    {
        var pos = trans.Value;
        var toCenter = float3.zero - pos;
        toCenter.y = 0.0f;

        var toCenterDir = math.normalize(toCenter);

        var gravity = toCenterDir * 3f;

        phyVel.Linear += gravity * Time.deltaTime;

        var rotMoveDir = math.rotate(rotDir.clockwise ? clockwiseQuat : anticlockwiseQuat, toCenterDir);
        var spdOnRotMoveDir = math.dot(phyVel.Linear, rotMoveDir);

        phyVel.Linear += rotMoveDir * (rotDir.speed - spdOnRotMoveDir) * Time.deltaTime;

        if (trans.Value.y > Settings.spaceRadius.y)
        {
            phyVel.Linear += math.up() * -1f * rotDir.speed * Time.deltaTime;
        }
        else if (trans.Value.y < -Settings.spaceRadius.y)
        {
            phyVel.Linear += math.up() * rotDir.speed * Time.deltaTime;
        }
    }
}
