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
public class PhysicsCubeCreateSystem_SpatialOS : ComponentSystem
{
    private EntityQuery addPhysicsQuery;

    private Unity.Mathematics.Random random = new Unity.Mathematics.Random();

    private BlobAssetReference<Unity.Physics.Collider> sharedCollider;
    private RenderMesh cubeRenderMesh;

    protected override void OnCreate()
    {
        base.OnCreate();

        random.InitState(10);

        addPhysicsQuery = GetEntityQuery(
            ComponentType.ReadOnly<MoveCube.Component>(),
            ComponentType.ReadOnly<Improbable.Position.Component>(),
            ComponentType.Exclude<PhysicsCollider>()
        );

        sharedCollider = Unity.Physics.BoxCollider.Create(float3.zero,
            quaternion.identity, new float3(1, 1, 1), 0.05f,
            null,
            new Unity.Physics.Material
            {
                Friction = 0f,
                Restitution = 1f,
                Flags = Unity.Physics.Material.MaterialFlags.EnableCollisionEvents
            }
        );

        var prefab = Resources.Load<GameObject>(@"Prefabs/Cube");
        var meshData = prefab.GetComponent<MeshFilter>().sharedMesh;
        var material = prefab.GetComponent<MeshRenderer>().sharedMaterial;
        cubeRenderMesh = new RenderMesh
        {
            mesh = meshData,
            material = material
        };

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
                Restitution = 1f,
                Flags = Unity.Physics.Material.MaterialFlags.EnableCollisionEvents
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

                EntityManager.AddComponent<CubeColor>(entity);
                EntityManager.SetComponentData(entity, new CubeColor
                {
                    isRed = true
                });

                // add MovementComponent for ServerUnitTransformSyncSystem to send out the event
                var moveComp = new MovementComponent
                {
                    info = new TransformInfo(
                        posV3.ToIntAbsolute(),
                        Vector3.zero.ToIntAbsolute(),
                        0),
                    isRed = true
                };
                EntityManager.AddComponent<MovementComponent>(entity);
                EntityManager.SetComponentData(entity, moveComp);
            }
        );
    }
}

