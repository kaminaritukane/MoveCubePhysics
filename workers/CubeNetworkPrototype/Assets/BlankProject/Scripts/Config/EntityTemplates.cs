using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.PlayerLifecycle;
using Improbable.Gdk.TransformSynchronization;
using System.Text;
using UnityEngine;

namespace BlankProject
{
    public static class EntityTemplates
    {
        public static EntityTemplate CreatePlayerEntityTemplate(string workerId, byte[] serializedArguments)
        {
            var clientAttribute = EntityTemplate.GetWorkerAccessAttribute(workerId);
            var serverAttribute = WorkerUtils.UnityGameLogic;

            var playerName = Encoding.ASCII.GetString(serializedArguments);
            Debug.Log($"CreatePlayerEntityTemplate: {playerName}");

            var template = new EntityTemplate();
            var position = new Vector3
            {
                x = Random.Range(-50f, 50f),
                y = Random.Range(-50f, 50f),
                z = Random.Range(-50f, 50f)
            };

            template.AddComponent(new Player.Snapshot(), serverAttribute);
            template.AddComponent(new Position.Snapshot(position.ToCoordinates()), serverAttribute);
            template.AddComponent(new Metadata.Snapshot("Player"), serverAttribute);
            template.AddComponent(new ClientUnitTransform.Snapshot(
                new TransformInfo (
                    position.ToIntAbsolute(),
                    Vector3.zero.ToIntAbsolute(),
                    0)
                ), clientAttribute);
            template.AddComponent(new ServerUnitTransform.Snapshot(), serverAttribute);
            template.AddComponent(new SvTestCommand.Snapshot(), serverAttribute);
            template.AddComponent(new PlayerState.Snapshot(playerName), serverAttribute);

            PlayerLifecycleHelper.AddPlayerLifecycleComponents(template, workerId, serverAttribute);
            TransformSynchronizationHelper.AddTransformSynchronizationComponents(template, clientAttribute, position);

            template.SetReadAccess(WorkerUtils.UnityClient, serverAttribute);
            template.SetComponentWriteAccess(EntityAcl.ComponentId, clientAttribute);

            return template;
        }

        public static EntityTemplate CreateMoveCubeEntityTemplate(string workerId, string cubeName)
        {
            var serverAttribute = WorkerUtils.UnityGameLogic;
            //var clientAttribute = EntityTemplate.GetWorkerAccessAttribute(workerId);

            Debug.Log($"CreateMoveCubeEntityTemplate: {cubeName}");

            var template = new EntityTemplate();
            var position = new Vector3
            {
                x = Random.Range(-40f, 40f),
                y = Random.Range(-40f, 40f),
                z = Random.Range(-40f, 40f)
            };

            template.AddComponent(new MoveCube.Snapshot(), serverAttribute);
            template.AddComponent(new Position.Snapshot(position.ToCoordinates()), serverAttribute);
            template.AddComponent(new Metadata.Snapshot("MoveCube"), serverAttribute);
            template.AddComponent(new Persistence.Snapshot(), serverAttribute);
            template.AddComponent(new ServerUnitTransform.Snapshot(), serverAttribute);
            template.AddComponent(new PlayerState.Snapshot(cubeName), serverAttribute);

            template.SetReadAccess(WorkerUtils.UnityClient, serverAttribute);
            template.SetComponentWriteAccess(EntityAcl.ComponentId, serverAttribute);

            return template;
        }
    }
}
