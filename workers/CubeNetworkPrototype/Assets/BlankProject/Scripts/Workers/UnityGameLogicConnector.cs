using Improbable.Gdk.Core;
using Improbable.Gdk.GameObjectCreation;
using Improbable.Gdk.PlayerLifecycle;
using Improbable.Gdk.TransformSynchronization;
using Improbable.Worker.CInterop;
using System;
using UnityEngine;

namespace BlankProject
{
    public class UnityGameLogicConnector : WorkerConnector
    {
        [SerializeField] private string connectToLanIp = null;

        public event Action<ServerGameObjectCreator> OnCreatorCreated;

        public ServerGameObjectCreator TransformGameObjectCreator { get; private set; }

        private void Awake()
        {
            Application.targetFrameRate = 15;
        }

        private async void Start()
        {
            PlayerLifecycleConfig.CreatePlayerEntityTemplate = EntityTemplates.CreatePlayerEntityTemplate;

            ReceptionistFlow flow;
            ConnectionParameters connectionParameters;

            if (Application.isEditor)
            {
                flow = new ReceptionistFlow(CreateNewWorkerId(WorkerUtils.UnityGameLogic));
                if (!string.IsNullOrEmpty(connectToLanIp))
                {
                    flow.ReceptionistHost = connectToLanIp;
                }
                connectionParameters = CreateConnectionParameters(WorkerUtils.UnityGameLogic);
            }
            else
            {
                flow = new ReceptionistFlow(CreateNewWorkerId(WorkerUtils.UnityGameLogic),
                    new CommandLineConnectionFlowInitializer());
                if (!string.IsNullOrEmpty(connectToLanIp))
                {
                    flow.ReceptionistHost = connectToLanIp;
                }
                connectionParameters = CreateConnectionParameters(WorkerUtils.UnityGameLogic,
                    new CommandLineConnectionParameterInitializer());
            }

            if (!string.IsNullOrEmpty(connectToLanIp))
            {
                connectionParameters.Network.UseExternalIp = true;
            }

            var builder = new SpatialOSConnectionHandlerBuilder()
                .SetConnectionFlow(flow)
                .SetConnectionParameters(connectionParameters);

            await Connect(builder, new ForwardingDispatcher()).ConfigureAwait(false);
        }

        protected override void HandleWorkerConnectionEstablished()
        {
            Worker.World.GetOrCreateSystem<MetricSendSystem>();
            Worker.World.GetOrCreateSystem<ServerUnitTransformCmdSystem>();

            Worker.World.GetOrCreateSystem<PhysicsCubeCreateSystem_SpatialOS>();
            Worker.World.GetOrCreateSystem<PhysicsBodiesMoveSystem_SpatialOS>();

            Worker.World.GetOrCreateSystem<ServerPlayerMovementSystem>();
            Worker.World.GetOrCreateSystem<ServerUnitTransformSyncSystem>();

            PlayerLifecycleHelper.AddServerSystems(Worker.World);
            TransformGameObjectCreator = new ServerGameObjectCreator(Worker, WorkerUtils.UnityGameLogic, transform.position);
            GameObjectCreationHelper.EnableStandardGameObjectCreation(Worker.World, TransformGameObjectCreator);
            OnCreatorCreated?.Invoke(TransformGameObjectCreator);

            TransformSynchronizationHelper.AddServerSystems(Worker.World);
        }
    }
}
