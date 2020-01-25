using BlankProject;
using Improbable.Gdk.Core;
using Improbable.Gdk.Core.Commands;
using Improbable.Gdk.Subscriptions;
using System;
using System.Collections.Generic;
using Unity.Entities;

[UpdateInGroup(typeof(SpatialOSUpdateGroup))]
public class ServerUnitTransformCmdSystem : ComponentSystem
{
    private WorkerSystem workerSystem;
    private CommandSystem commandSystem;
    private CommandCallbackSystem commandCallbackSystem;
    private ComponentUpdateSystem componentUpdateSystem;
    

    protected override void OnCreate()
    {
        base.OnCreate();

        workerSystem = World.GetExistingSystem<WorkerSystem>();
        commandSystem = World.GetExistingSystem<CommandSystem>();
        commandCallbackSystem = World.GetExistingSystem<CommandCallbackSystem>();
        componentUpdateSystem = World.GetExistingSystem<ComponentUpdateSystem>();
    }

    protected override void OnUpdate()
    {
        var testCmdrequests = commandSystem.GetRequests<SvTestCommand.TestCommand.ReceivedRequest>();
        for(int i=0; i<testCmdrequests.Count; ++i)
        {
            var req = testCmdrequests[i];
            var resp = new SvTestCommand.TestCommand.Response(req.RequestId, new BlankProject.Empty());
            commandSystem.SendResponse(resp);
        }

        var spawnMoveCubeReqs = commandSystem.GetRequests<SvTestCommand.SpawnMovecube.ReceivedRequest>();
        for (int i = 0; i < spawnMoveCubeReqs.Count; ++i)
        {
            var req = spawnMoveCubeReqs[i];
            for ( int j=0; j<req.Payload.Amount; ++j)
            {
                var cubeName = $"{req.Payload.PrefixName}";
                var moveCubeTemp = EntityTemplates.CreateMoveCubeEntityTemplate(
                    workerSystem.WorkerId,
                    cubeName
                );

                commandSystem.SendCommand(new WorldCommands.CreateEntity.Request(moveCubeTemp));
            }

            commandSystem.SendResponse<SvTestCommand.SpawnMovecube.Response>(new SvTestCommand.SpawnMovecube.Response
            {
                RequestId = req.RequestId,
                Payload = new BlankProject.Empty()
            });
        }

        var deleteMoveCubeReqs = commandSystem.GetRequests<SvTestCommand.DeleteMovecube.ReceivedRequest>();
        for (int i = 0; i < deleteMoveCubeReqs.Count; ++i)
        {
            var req = deleteMoveCubeReqs[i];
            var ids = req.Payload.Ids;
            for( int j=0; j<ids.Count; ++j )
            {
                commandSystem.SendCommand(new WorldCommands.DeleteEntity.Request(ids[j]));
            }

            commandSystem.SendResponse<SvTestCommand.DeleteMovecube.Response>(new SvTestCommand.DeleteMovecube.Response
            {
                RequestId = req.RequestId,
                Payload = new BlankProject.Empty()
            });
        }
    }
}
