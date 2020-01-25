using BlankProject;
using Improbable.Gdk.Subscriptions;
using UnityEngine;

public class TransformReceiver_Client : MonoBehaviour
{
    [Require] private ClientUnitTransformReader unitTransformReader = null;

    private Vector3 originPosition = Vector3.zero;
    private Quaternion rotation = Quaternion.identity;
    private float deltaTime = 0.0f;// time to move to new position/rotation

    private Vector3 workerOrigin = Vector3.zero;

    private void Start()
    {
        workerOrigin = GetComponent<LinkedEntityComponent>().Worker.Origin;

        unitTransformReader.OnUpdate += OnUpdate;
        if (unitTransformReader.IsValid)
            originPosition = unitTransformReader.Data.Info.Position.ToVector3();
    }

    private void OnUpdate(ClientUnitTransform.Update obj)
    {
        if (obj.Info.HasValue)
        {
            originPosition = obj.Info.Value.Position.ToVector3();
            rotation = Quaternion.Euler(obj.Info.Value.Rotation.ToVector3());
            deltaTime = obj.Info.Value.DeltaTime;
        }
    }

    private void Update()
    {
        if (Time.deltaTime < deltaTime)
        {
            // update position
            // deltaTime here must > 0, cause Time.deltaTime >= 0
            var percentage = Time.deltaTime / deltaTime;
            transform.position = Vector3.Lerp(transform.position, 
                originPosition + workerOrigin,
                percentage);

            // update rotation
            transform.rotation = Quaternion.Lerp(transform.rotation, rotation, percentage);

            deltaTime -= Time.deltaTime;
        }
        else
        {
            transform.position = originPosition + workerOrigin;
            transform.rotation = rotation;
        }
    }
}
