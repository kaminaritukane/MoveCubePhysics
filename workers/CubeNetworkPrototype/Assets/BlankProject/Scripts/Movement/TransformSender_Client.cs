using System;
using BlankProject;
using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.Subscriptions;
using UnityEngine;

public class TransformSender_Client : MonoBehaviour
{
    public delegate void TransformSendDelegate();
    public event TransformSendDelegate OnTransformSent;

    [Require] private ClientUnitTransformWriter clientTransformWriter = null;

    [SerializeField] private float updateHz = 1.0f;
    public float UpdateHz
    {
        get { return updateHz; }
        set
        {
            if (value > 0.0f)
            {
                updateHz = value;
                updateDelta = 1.0f / updateHz;
            }
            else
            {
                updateHz = 1.0f;
                updateDelta = 1.0f;
                Debug.LogError("The Transform Update Hz must be greater than 0.");
            }
        }
    }

    [SerializeField] [HideInInspector] private float updateDelta;

    private Vector3 originPosition;
    private Vector3 originRotation;

    private float cumulativeTimeDelta = 0;

    private Vector3 workerOrigin = Vector3.zero;
    private EntityId entityId;

    void Start()
    {
        var linkedComp = GetComponent<LinkedEntityComponent>();
        entityId = linkedComp.EntityId;
        workerOrigin = linkedComp.Worker.Origin;

        if (updateHz > 0.0f)
        {
            updateDelta = 1.0f / updateHz;
        }
        else
        {
            updateDelta = 1.0f;
            Debug.LogError("The Transform Update Hz must be greater than 0.");
        }
    }

    void Update()
    {
        var delta = cumulativeTimeDelta + Time.deltaTime;
        if (delta >= updateDelta)
        {
            delta -= updateDelta;
            if (transform.position - workerOrigin != originPosition 
                || transform.eulerAngles != originRotation)
            {
                SendTransformInfo(cumulativeTimeDelta);
            }
        }
        cumulativeTimeDelta = delta;
    }

    void SendTransformInfo(float timeDelta)
    {
        originPosition = transform.position - workerOrigin;
        originRotation = transform.eulerAngles;

        clientTransformWriter.SendUnitsTransformChangedEvent(new TransformInfo {
            Position = originPosition.ToIntAbsolute(),
            Rotation = originRotation.ToIntAbsolute(),
            DeltaTime = timeDelta.ToInt100k()
        });

        OnTransformSent?.Invoke();
    }
}
