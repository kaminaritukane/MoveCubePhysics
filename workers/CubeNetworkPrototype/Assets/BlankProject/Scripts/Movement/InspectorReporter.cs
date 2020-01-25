using Improbable;
using Improbable.Gdk.Subscriptions;
using UnityEngine;

public class InspectorReporter : MonoBehaviour
{
    [Require] private PositionWriter positionWriter = null;

    [SerializeField] private float updateFrequence = 1.0f;
    private float updateInterval = 0.0f;
    private float updateCD = 0.0f;

    void Start()
    {
        if (Mathf.Approximately(updateFrequence, 0.0f))
        {
            updateFrequence = 1.0f;
        }
        updateInterval = 1.0f / updateFrequence;
        updateCD = updateInterval;
    }

    void Update()
    {
        var delta = updateCD - Time.deltaTime;
        if (delta < 0.0f)
        {
            delta += updateInterval;

            // send the update so that the SpatialOS inspector will receive update
            positionWriter.SendUpdate(new Position.Update
            {
                Coords = Coordinates.FromUnityVector( transform.position )
            });
        }
        updateCD = delta;
    }
}
