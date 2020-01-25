using BlankProject;
using Improbable;
using Improbable.Gdk.Core;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using ServerCommon;

[UpdateInGroup(typeof(SpatialOSUpdateGroup))]
public class ServerPlayerMovementSystem : ComponentSystem
{
    private WorkerSystem workerSystem;
    private CommandSystem commandSystem;
    private ComponentUpdateSystem componentUpdateSystem;

    protected override void OnCreate()
    {
        base.OnCreate();

        workerSystem = World.GetExistingSystem<WorkerSystem>();
        commandSystem = World.GetExistingSystem<CommandSystem>();
        componentUpdateSystem = World.GetExistingSystem<ComponentUpdateSystem>();
    }

    protected override void OnUpdate()
    {
        var moveEvents = componentUpdateSystem.GetEventsReceived<ClientUnitTransform.UnitsTransformChanged.Event>();
        if (moveEvents.Count > 0)
        {
            for (int i = 0; i < moveEvents.Count; ++i)
            {
                var moveEvt = moveEvents[i];

                if ( !workerSystem.TryGetEntity(moveEvt.EntityId, out var entity) )
                {
                    continue;
                }

                var moveComp = new MovementComponent
                {
                    info = new TransformInfo(
                        moveEvt.Event.Payload.Position,
                        moveEvt.Event.Payload.Rotation,
                        moveEvt.Event.Payload.DeltaTime)
                };

                if ( !EntityManager.HasComponent<MovementComponent>(entity) )
                {
                    EntityManager.AddComponent<MovementComponent>(entity);
                }

                EntityManager.SetComponentData(entity, moveComp);
            }            
        }
    }
}
