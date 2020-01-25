using BlankProject;
using Improbable;
using Improbable.Gdk.Core;
using ServerCommon;
using Unity.Entities;
using UnityEngine;

[UpdateInGroup(typeof(SpatialOSUpdateGroup))]
public class ServerCubeMoveSystem : ComponentSystem
{
    private ComponentUpdateSystem componentUpdateSystem;

    private EntityQuery SMCQuery_NOMovmentComp;
    private EntityQuery SMCQuery_MovementComp;

    private EntityQuery PlayerQuery;

    private const float moveSpeed = 10.0f;

    protected override void OnCreate()
    {
        base.OnCreate();

        componentUpdateSystem = World.GetExistingSystem<ComponentUpdateSystem>();

        SMCQuery_NOMovmentComp = GetEntityQuery(
            ComponentType.ReadOnly<MoveCube.Component>(),
            ComponentType.ReadOnly<Position.Component>(),
            ComponentType.Exclude<MovementComponent>()
        );

        SMCQuery_MovementComp = GetEntityQuery(
            ComponentType.ReadOnly<MoveCube.Component>(),
            ComponentType.ReadWrite<MovementComponent>()
        );
    }

    protected override void OnUpdate()
    {
        Entities.With(SMCQuery_NOMovmentComp).ForEach(
            ( Entity entity,
            ref Position.Component posComp ) =>
            {
                var moveComp = new MovementComponent
                {
                    info = new TransformInfo(
                        posComp.Coords.ToUnityVector().ToIntAbsolute(),
                        Vector3.zero.ToIntAbsolute(),
                        0)
                };

                EntityManager.AddComponent<MovementComponent>(entity);
                EntityManager.SetComponentData(entity, moveComp);
            }
        );

        Entities.With(SMCQuery_MovementComp).ForEach(
            ( ref SpatialEntityId seId,
              ref MovementComponent moveComp ) =>
            {
                var info = moveComp.info;
                var oriToPos = info.Position.ToVector3() - Vector3.zero;
                var dir = Vector3.Cross(oriToPos, Vector3.up).normalized;
                var step = dir * moveSpeed * UnityEngine.Time.deltaTime;
                var oriToNewpOS = (oriToPos + step).normalized;
                oriToNewpOS = oriToNewpOS * oriToPos.magnitude;

                var rot = Quaternion.LookRotation(dir);

                info.Position = oriToNewpOS.ToIntAbsolute();
                info.Rotation = rot.eulerAngles.ToIntAbsolute();
                //info.DeltaTime = Time.deltaTime; // set delta time in ServerUnitTransformSyncSystem

                moveComp.info = info;
            }
        );
    }
}
