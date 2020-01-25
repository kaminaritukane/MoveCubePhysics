using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using BlankProject;
using Improbable.Gdk.Core;
using Improbable.Gdk.GameObjectCreation;
using Improbable.Gdk.PlayerLifecycle;
using Improbable.Gdk.TransformSynchronization;
using Improbable.Worker.CInterop;
using UnityEngine;

public class SimulatedClientConnector : WorkerConnector
{
    public void SpawnPlayer(int number)
    {
        var serializedArgs = Encoding.ASCII.GetBytes($"SimPlayer_{number}");
        var sendSystem = Worker.World.GetExistingSystem<SendCreatePlayerRequestSystem>();
        sendSystem.RequestPlayerCreation(serializedArgs);
    }

    public async Task TryConnect(string simulatedPlayerDevAuthToken,
        string simulatedPlayerTargetDeployment,
        string lanIp)
    {
        var connectionParams = CreateConnectionParameters(WorkerUtils.UnityClient);
        connectionParams.Network.UseExternalIp = true;

        var builder = new SpatialOSConnectionHandlerBuilder()
            .SetConnectionParameters(connectionParams)
            .SetConnectionFlow(new ChosenDeploymentLocatorFlow(simulatedPlayerTargetDeployment)
            {
                DevAuthToken = simulatedPlayerDevAuthToken
            });

        await Connect(builder, new ForwardingDispatcher());
    }

    public async Task TryConnect(string lanIp = null)
    {
        var workerId = CreateNewWorkerId(WorkerUtils.UnityClient);

        var connParams = CreateConnectionParameters(WorkerUtils.UnityClient);
        connParams.Network.ConnectionType = NetworkConnectionType.Kcp;

        if (!string.IsNullOrEmpty(lanIp))
        {
            connParams.Network.UseExternalIp = true;
        }

        var builder = new SpatialOSConnectionHandlerBuilder()
            .SetConnectionParameters(connParams);

        if (!Application.isEditor)
        {
            var initializer = new CommandLineConnectionFlowInitializer();
            switch (initializer.GetConnectionService())
            {
                case ConnectionService.Receptionist:
                    var receptionistFlow = new ReceptionistFlow(workerId, initializer);
                    receptionistFlow.WorkerId = workerId;
                    if (!string.IsNullOrEmpty(lanIp))
                    {
                        receptionistFlow.ReceptionistHost = lanIp;
                    }
                    builder.SetConnectionFlow(receptionistFlow);
                    break;
                case ConnectionService.Locator:
                    builder.SetConnectionFlow(new LocatorFlow(initializer));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        else
        {
            var receptionistFlow = new ReceptionistFlow(workerId);
            receptionistFlow.WorkerId = workerId;
            if (!string.IsNullOrEmpty(lanIp))
            {
                receptionistFlow.ReceptionistHost = lanIp;
            }
            builder.SetConnectionFlow(receptionistFlow);
        }

        await Connect(builder, new ForwardingDispatcher()).ConfigureAwait(false);
    }

    protected override void HandleWorkerConnectionEstablished()
    {
        PlayerLifecycleHelper.AddClientSystems(Worker.World, false);

        Worker.OnDisconnect += Worker_OnDisconnect;

        var customCreator = new SimulatedUnitCreator(Worker, Worker.WorkerType, Worker.Origin);
        SimulatedUnitManager.Instance.RegisterWorkerEntityCreator(Worker, customCreator);
        GameObjectCreationHelper.EnableStandardGameObjectCreation(Worker.World, customCreator);

        TransformSynchronizationHelper.AddClientSystems(Worker.World);
    }

    protected override void HandleWorkerConnectionFailure(string errorMessage)
    {
        base.HandleWorkerConnectionFailure(errorMessage);

        Debug.Log($"HandleWorkerConnectionFailure {Worker?.WorkerId} : {errorMessage}");
    }

    private void Worker_OnDisconnect(string reason)
    {
        Debug.Log($"Worker {Worker.WorkerId} disconnected: {reason}");
    }

    public override void Dispose()
    {
        SimulatedUnitManager.Instance.UnregisterWorkerEntityCreator(Worker);

        base.Dispose();
    }
}
