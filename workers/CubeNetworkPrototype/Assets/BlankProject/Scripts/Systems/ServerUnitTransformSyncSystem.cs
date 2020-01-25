using BlankProject;
using Improbable.Gdk.Core;
using ServerCommon;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(SpatialOSUpdateGroup))]
[AlwaysUpdateSystem]
public class ServerUnitTransformSyncSystem : ComponentSystem
{
    private float cumulativeTimeDelta = 0;
    private float updateDelta = 1.0f / 15.0f;

    private EntityQuery MovementQuery;
    private EntityQuery PlayerQuery;

    private WorkerSystem workerSystem;
    private ComponentUpdateSystem componentUpdateSystem;
    private CommandSystem commandSystem;

    private bool serverMetricsInited = false;

    private List<EntityTransformInfo> entityTransInfos = new List<EntityTransformInfo>();

    private const float SpatialPosReportDelta = 1.0f;
    private float updateSpatialPosDelta = 0f;
    private EntityQuery spatialPosQuery;

    protected override void OnCreate()
    {
        base.OnCreate();

        workerSystem = World.GetExistingSystem<WorkerSystem>();
        commandSystem = World.GetExistingSystem<CommandSystem>();

        componentUpdateSystem = World.GetExistingSystem<ComponentUpdateSystem>();

        MovementQuery = GetEntityQuery(
            ComponentType.ReadOnly<MovementComponent>()
        );

        PlayerQuery = GetEntityQuery(
            ComponentType.ReadOnly<Player.Component>()
        );

        spatialPosQuery = GetEntityQuery(
            ComponentType.ReadOnly<Translation>(),
            ComponentType.ReadWrite<Improbable.Position.Component>()
        );
    }

    protected override void OnUpdate()
    {
        var delta = cumulativeTimeDelta + UnityEngine.Time.deltaTime;
        if (delta >= updateDelta)
        {
            DoSync(delta);

            if (ServerMetrics.Instance != null)
            {
                if (!serverMetricsInited)
                {
                    ServerMetrics.Instance.workerSystem = workerSystem;
                    ServerMetrics.Instance.commandSystem = commandSystem;
                    serverMetricsInited = true;
                }
                ServerMetrics.Instance.CountUpdateSent();
            }

            delta -= updateDelta;
        }
        cumulativeTimeDelta = delta;

        // Uncomment following code to report postion to spatial os inspector
        //updateSpatialPosDelta += UnityEngine.Time.deltaTime;
        //if (updateSpatialPosDelta >= SpatialPosReportDelta)
        //{
        //    updateSpatialPosDelta -= SpatialPosReportDelta;

        //    ReportSpatialPos();
        //}
    }

    private void ReportSpatialPos()
    {
        Entities.With(spatialPosQuery).ForEach(
            ( Entity entity,
              ref Translation trans,
              ref Improbable.Position.Component imPos ) => {
                  var v3Pos = new Vector3(trans.Value.x, trans.Value.y, trans.Value.z);
                  imPos.Coords = Improbable.Coordinates.FromUnityVector(v3Pos);
              }
        );
    }

    private void DoSync(float delta)
    {
        entityTransInfos.Clear();

        Entities.With(MovementQuery).ForEach(
            ( Entity entity,
              ref SpatialEntityId sId,
              ref MovementComponent moveComp ) =>
            {
                var trans = moveComp.info;
                trans.DeltaTime = delta.ToInt100k();
                entityTransInfos.Add(new EntityTransformInfo {
                    EntityId = sId.EntityId,
                    TransformInfo = trans
                });
            }
        );

        if (entityTransInfos.Count > 0)
        {
            var updatePackage = new UnitsTransformPackage(entityTransInfos);
            var syncEvent = new ServerUnitTransform.UnitsTransformSync.Event(updatePackage);
            Entities.With(PlayerQuery).ForEach(
                (ref SpatialEntityId seId) =>
                {
                    var entId = seId.EntityId;
                    componentUpdateSystem.SendEvent(
                        syncEvent,
                        entId
                    );
                }
            );
        }
    }
}
