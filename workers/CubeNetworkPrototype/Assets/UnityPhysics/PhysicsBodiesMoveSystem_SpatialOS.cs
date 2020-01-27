using BlankProject;
using Improbable.Gdk.Core;
using ServerCommon;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

[DisableAutoCreation]
[UpdateInGroup(typeof(SpatialOSUpdateGroup))]
public class PhysicsBodiesMoveSystem_SpatialOS : JobComponentSystem
{
    EndSimulationEntityCommandBufferSystem m_EntityCommandBufferSystem;

    BuildPhysicsWorld m_BuildPhysicsWorldSystem;
    StepPhysicsWorld m_StepPhysicsWorldSystem;

    private EntityQuery physicsQuery;
    private EntityQuery colorChangeQuery;

    private const float gravityChageRadius = 10.0f;

    private quaternion clockwiseQuat = quaternion.RotateY(90.0f);
    private quaternion anticlockwiseQuat = quaternion.RotateY(-90.0f);

    private Unity.Mathematics.Random random = new Unity.Mathematics.Random();

    protected override void OnCreate()
    {
        base.OnCreate();

        m_EntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

        m_BuildPhysicsWorldSystem = World.GetOrCreateSystem<BuildPhysicsWorld>();
        m_StepPhysicsWorldSystem = World.GetOrCreateSystem<StepPhysicsWorld>();

        random.InitState(10);

        physicsQuery = GetEntityQuery(
            ComponentType.ReadOnly<PhysicsCollider>(),
            ComponentType.ReadWrite<PhysicsVelocity>(),
            ComponentType.ReadWrite<MovementComponent>(),
            ComponentType.ReadWrite<Translation>(),
            ComponentType.ReadOnly<Rotation>(),
            ComponentType.ReadOnly<RotateDir>()
        );

        colorChangeQuery = GetEntityQuery(
            ComponentType.ReadOnly<CubeColor>(),
            ComponentType.ReadOnly<CubeColorChanged>()
        );

        World.GetOrCreateSystem<BuildPhysicsWorld>();
        World.GetOrCreateSystem<StepPhysicsWorld>();
        World.GetOrCreateSystem<ExportPhysicsWorld>();
        World.GetOrCreateSystem<EndFramePhysicsSystem>();

        // for render mesh on server
        //{
        //World.GetOrCreateSystem<EndFrameTRSToLocalToWorldSystem>();

        //World.GetOrCreateSystem<CreateMissingRenderBoundsFromMeshRenderer>();
        //World.GetOrCreateSystem<RenderBoundsUpdateSystem>();
        //World.GetOrCreateSystem<RenderMeshSystemV2>();
        //}
    }
    

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        JobHandle moveJobHandle = new MovePhysicsCubeJob
        {
            moveEntities = physicsQuery.ToEntityArray(Allocator.TempJob),

            moveComps = GetComponentDataFromEntity<MovementComponent>(),
            transGroup = GetComponentDataFromEntity<Translation>(),
            rotGroup = GetComponentDataFromEntity<Rotation>(),
            
            velocityGroup = GetComponentDataFromEntity<PhysicsVelocity>(),
            rotateDirGroup = GetComponentDataFromEntity<RotateDir>(true),
            deltaTime = Time.deltaTime,
            random = random
        }.Schedule(inputDeps);

        JobHandle collisionEventHandle = new CollisionEventJob()
        {
            //CommandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer(),
            moveGroup = GetComponentDataFromEntity<MovementComponent>(),
            cubeColorGroup = GetComponentDataFromEntity<CubeColor>()
        }.Schedule(m_StepPhysicsWorldSystem.Simulation,
            ref m_BuildPhysicsWorldSystem.PhysicsWorld,
            moveJobHandle
        );
        m_EntityCommandBufferSystem.AddJobHandleForProducer(collisionEventHandle);
        collisionEventHandle.Complete();

        //var changedEntities = colorChangeQuery.ToEntityArray(Allocator.TempJob);
        //for (int i = 0; i < changedEntities.Length; ++i)
        //{
        //    var entity = changedEntities[i];
        //    var color = EntityManager.GetComponentData<CubeColor>(entity);
            

        //    EntityManager.RemoveComponent<CubeColorChanged>(entity);
        //}
        //changedEntities.Dispose();

        return moveJobHandle;
    }

    struct MovePhysicsCubeJob : IJob
    {
        [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<Entity> moveEntities;

        public ComponentDataFromEntity<MovementComponent> moveComps;
        public ComponentDataFromEntity<PhysicsVelocity> velocityGroup;

        [ReadOnly] public ComponentDataFromEntity<Translation> transGroup;
        [ReadOnly] public ComponentDataFromEntity<Rotation> rotGroup;
        [ReadOnly] public ComponentDataFromEntity<RotateDir> rotateDirGroup;
        [ReadOnly] public float deltaTime;
        [ReadOnly] public Unity.Mathematics.Random random;

        public void Execute()
        {
            for (int i = 0; i < moveEntities.Length; ++i)
            {
                var entity = moveEntities[i];
                var phyVel = velocityGroup[entity];
                var rotDir = rotateDirGroup[entity];
                MoveForward(ref phyVel, ref rotDir);
                velocityGroup[entity] = phyVel;
                //rotateDirGroup[entity] = rotDir;

                if ( moveComps.Exists(entity) 
                    && transGroup.Exists(entity)
                    && rotGroup.Exists(entity) )
                {
                    var trans = transGroup[entity];
                    var rot = rotGroup[entity];
                    var moveComp = moveComps[entity];

                    // update movement comp
                    var v3Pos = new Vector3(trans.Value.x, trans.Value.y, trans.Value.z);
                    var posInfo = moveComp.info;
                    {
                        posInfo.Position = v3Pos.ToIntAbsolute();

                        var phyQuat = rot.Value.value;
                        var quat = new Quaternion(phyQuat.x, phyQuat.y, phyQuat.z, phyQuat.w);
                        posInfo.Rotation = quat.eulerAngles.ToIntAbsolute();
                    }
                    moveComp.info = posInfo;
                    moveComps[entity] = moveComp;
                }
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

    struct CollisionEventJob : ICollisionEventsJob
    {
        //public EntityCommandBuffer CommandBuffer;
        public ComponentDataFromEntity<MovementComponent> moveGroup;
        public ComponentDataFromEntity<CubeColor> cubeColorGroup;

        //public RenderMesh redRenderMesh;
        //public RenderMesh blueRenderMesh;

        public void Execute(CollisionEvent collisionEvent)
        {
            //Debug.Log($"{collisionEvent.Entities.EntityA} <-> {collisionEvent.Entities.EntityB}");
            var entityA = collisionEvent.Entities.EntityA;
            var entityB = collisionEvent.Entities.EntityB;

            if (cubeColorGroup.Exists(entityA))
            {
                var cubeColor = cubeColorGroup[entityA];
                cubeColor.isRed = !cubeColor.isRed;
                cubeColorGroup[entityA] = cubeColor;

                if (moveGroup.Exists(entityA))
                {
                    var moveComp = moveGroup[entityA];
                    moveComp.isRed = cubeColor.isRed;
                    moveGroup[entityA] = moveComp;
                }

                //CommandBuffer.AddComponent<CubeColorChanged>(entityA);

                //CommandBuffer.SetSharedComponent(entityA, cubeColor.isRed ? redRenderMesh : blueRenderMesh);
            }

            if (cubeColorGroup.Exists(entityB))
            {
                var cubeColor = cubeColorGroup[entityB];
                cubeColor.isRed = !cubeColor.isRed;
                cubeColorGroup[entityB] = cubeColor;

                if (moveGroup.Exists(entityB))
                {
                    var moveComp = moveGroup[entityB];
                    moveComp.isRed = cubeColor.isRed;
                    moveGroup[entityB] = moveComp;
                }

                //CommandBuffer.AddComponent<CubeColorChanged>(entityB);

                //CommandBuffer.SetSharedComponent(entityA, cubeColor.isRed ? redRenderMesh : blueRenderMesh);
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
