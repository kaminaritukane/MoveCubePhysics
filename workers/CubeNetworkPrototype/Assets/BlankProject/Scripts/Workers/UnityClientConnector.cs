using System;
using Improbable.Gdk.Core;
using Improbable.Gdk.GameObjectCreation;
using Improbable.Gdk.PlayerLifecycle;
using Improbable.Gdk.TransformSynchronization;
using Improbable.Worker.CInterop;
using UnityEngine;

namespace BlankProject
{
    public class UnityClientConnector : WorkerConnector
    {
        private static UnityClientConnector instance;
        public static UnityClientConnector Instance
        {
            get
            {
                return instance;
            }
        }

        [SerializeField] private string connectToLanIp = null;



        public ClientGameObjectCreator TransformGameObjectCreator { get; private set; }

        private void Awake()
        {
            Application.targetFrameRate = 60;
            instance = this;
        }

        private async void Start()
        {
            IConnectionFlow connectionFlow;
            var connParams = CreateConnectionParameters(WorkerUtils.UnityClient);
            connParams.Network.ConnectionType = NetworkConnectionType.Kcp;

            if (!string.IsNullOrEmpty(connectToLanIp))
            {
                connParams.Network.UseExternalIp = true;
            }

            if (!Application.isEditor)
            {
                var initializer = new CommandLineConnectionFlowInitializer();
                switch (initializer.GetConnectionService())
                {
                    case ConnectionService.Receptionist:
                        var flow = new ReceptionistFlow(CreateNewWorkerId(WorkerUtils.UnityClient), initializer);
                        if (!string.IsNullOrEmpty(connectToLanIp))
                        {
                            flow.ReceptionistHost = connectToLanIp;
                        }
                        connectionFlow = flow;
                        break;
                    case ConnectionService.Locator:
                        connParams.Network.UseExternalIp = true;
                        connectionFlow = new LocatorFlow(initializer);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            else
            {
                var flow = new ReceptionistFlow(CreateNewWorkerId(WorkerUtils.UnityClient));
                if (!string.IsNullOrEmpty(connectToLanIp))
                {
                    flow.ReceptionistHost = connectToLanIp;
                }
                connectionFlow = flow;
            }

            var builder = new SpatialOSConnectionHandlerBuilder()
                .SetConnectionFlow(connectionFlow)
                .SetConnectionParameters(connParams);

            await Connect(builder, new ForwardingDispatcher()).ConfigureAwait(false);
        }

        protected override void HandleWorkerConnectionEstablished()
        {
            //Worker.World.GetOrCreateSystem<ClientMoveCubeSyncSystem>();

            PlayerLifecycleHelper.AddClientSystems(Worker.World);
            TransformGameObjectCreator = new ClientGameObjectCreator(Worker, WorkerUtils.UnityClient, transform.position);
            GameObjectCreationHelper.EnableStandardGameObjectCreation(Worker.World, TransformGameObjectCreator);

            TransformSynchronizationHelper.AddClientSystems(Worker.World);
        }
    }
}
