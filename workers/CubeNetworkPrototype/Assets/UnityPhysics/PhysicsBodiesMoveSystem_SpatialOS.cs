using BlankProject;
using Improbable.Gdk.Core;
using ServerCommon;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

[DisableAutoCreation]
[UpdateInGroup(typeof(SpatialOSUpdateGroup))]
public class PhysicsBodiesMoveSystem_SpatialOS : ComponentSystem
{
    private EntityQuery addPhysicsQuery;
    private EntityQuery physicsQuery;

    private const float gravityChageRadius = 10.0f;

    private quaternion clockwiseQuat = quaternion.RotateY(90.0f);
    private quaternion anticlockwiseQuat = quaternion.RotateY(-90.0f);

    private BlobAssetReference<Unity.Physics.Collider> sharedCollider;
    private RenderMesh cubeRenderMesh;

    private Unity.Mathematics.Random random = new Unity.Mathematics.Random();

    protected override void OnCreate()
    {
        base.OnCreate();

        random.InitState(10);

        addPhysicsQuery = GetEntityQuery(
            ComponentType.ReadOnly<MoveCube.Component>(),
            ComponentType.ReadOnly<Improbable.Position.Component>(),
            ComponentType.Exclude<PhysicsCollider>()
        );

        physicsQuery = GetEntityQuery(
            ComponentType.ReadOnly<PhysicsCollider>(),
            ComponentType.ReadWrite<PhysicsVelocity>(),
            ComponentType.ReadWrite<MovementComponent>(),
            ComponentType.ReadWrite<Translation>(),
            ComponentType.ReadOnly<Rotation>(),
            ComponentType.ReadOnly<RotateDir>()
        );

        sharedCollider = Unity.Physics.BoxCollider.Create(float3.zero,
            quaternion.identity, new float3(1,1,1), 0.05f);

        var prefab = Resources.Load<GameObject>(@"Prefabs/Cube");
        var meshData = prefab.GetComponent<MeshFilter>().sharedMesh;
        var material = prefab.GetComponent<MeshRenderer>().sharedMaterial;
        cubeRenderMesh = new RenderMesh
        {
            mesh = meshData,
            material = material
        };

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

        CreatePhysicsStep();

        CreateWalls();
    }

    void CreatePhysicsStep()
    {
        var entity = EntityManager.CreateEntity(new ComponentType[] { });

        EntityManager.AddComponentData(entity, new LocalToWorld { });
        EntityManager.AddComponentData(entity, new PhysicsStep
        {
            SimulationType = SimulationType.UnityPhysics,
            Gravity = float3.zero,
            SolverIterationCount = 4,
            ThreadCountHint = 8
        });
        EntityManager.AddComponentData(entity, new Rotation { Value = quaternion.identity });
        EntityManager.AddComponentData(entity, new Translation { Value = float3.zero });
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
        CreateStaticBody(meshRender, new float3(0, 50, 0), quaternion.identity, sharedCollider);// up
        CreateStaticBody(meshRender, new float3(-50, 0, 0), Quaternion.AngleAxis(90f, Vector3.forward), sharedCollider);// left
        CreateStaticBody(meshRender, new float3(50, 0, 0), Quaternion.AngleAxis(90f, Vector3.forward), sharedCollider);// right
        CreateStaticBody(meshRender, new float3(0, 0, -50), Quaternion.AngleAxis(90f, Vector3.right), sharedCollider);// front
        CreateStaticBody(meshRender, new float3(0, 0, 50), Quaternion.AngleAxis(90f, Vector3.right), sharedCollider);// end
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

    protected override void OnUpdate()
    {
        Entities.With(addPhysicsQuery).ForEach(
            (Entity entity,
             ref Improbable.Position.Component posComp) => 
            {
                var posV3 = posComp.Coords.ToUnityVector();

                //// Enable following 2 line to show the server cubes
                //EntityManager.AddComponent<RenderMesh>(entity);
                //EntityManager.SetSharedComponentData(entity, cubeRenderMesh);

                // add phsycis relevant comps
                EntityManager.AddComponent<LocalToWorld>(entity);

                EntityManager.AddComponent<Translation>(entity);
                EntityManager.SetComponentData(entity, new Translation
                {
                    Value = new float3(posV3.x, posV3.y, posV3.z)
                });

                EntityManager.AddComponent<Rotation>(entity);
                EntityManager.SetComponentData(entity, new Rotation
                {
                    Value = quaternion.identity
                });

                EntityManager.AddComponent<PhysicsCollider>(entity);
                var colliderComp = new PhysicsCollider
                {
                    Value = sharedCollider
                };
                EntityManager.SetComponentData(entity, colliderComp);

                EntityManager.AddComponent<PhysicsVelocity>(entity);

                EntityManager.AddComponent<PhysicsMass>(entity);
                EntityManager.SetComponentData(entity, 
                    PhysicsMass.CreateDynamic(colliderComp.MassProperties, 1f)
                );

                EntityManager.AddComponent<PhysicsDamping>(entity);
                EntityManager.SetComponentData(entity, new PhysicsDamping()
                {
                    Linear = 0.01f,
                    Angular = 0.03f
                });

                // add RotateDir
                EntityManager.AddComponent<RotateDir>(entity);
                EntityManager.SetComponentData(entity, new RotateDir
                {
                    clockwise = random.NextBool(),
                    speed = random.NextFloat(3f, 10f)
                });

                // add MovementComponent for ServerUnitTransformSyncSystem to send out the event
                var moveComp = new MovementComponent
                {
                    info = new TransformInfo(
                        posV3.ToIntAbsolute(),
                        Vector3.zero.ToIntAbsolute(),
                        0)
                };
                EntityManager.AddComponent<MovementComponent>(entity);
                EntityManager.SetComponentData(entity, moveComp);
            }
        );

        Entities.With(physicsQuery).ForEach(
            (Entity entity,
             ref PhysicsVelocity phyVel,
             ref Translation trans,
             ref Rotation rot,
             ref MovementComponent moveComp,
             ref RotateDir rotDir) => {

                 // update physics velocity
                 //CircleAroundAxis(ref trans, ref phyVel, ref rotDir);
                 MoveForward(ref phyVel, ref rotDir);

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
             }
        );
    }

    private void MoveForward(ref PhysicsVelocity phyVel,
        ref RotateDir rotDir)
    {
        var curSpeed = math.length(phyVel.Linear);
        if (curSpeed > 0.0f)
        {
            var curDir = math.normalize(phyVel.Linear);
            phyVel.Linear += curDir * (rotDir.speed - curSpeed) * Time.deltaTime;
        }
        else
        {
            phyVel.Linear = random.NextFloat3();
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
