using UnityEngine;


public class TransformUpdateMetrics : MonoBehaviour
{
    private uint sentUpdateCount = 0;
    private uint receivedUpdateCount = 0;
    private float timeCD = 1.0f;

    private uint resultSentUpdate = 0;
    private uint resultReceivedUpdate = 0;
    private float resultUpdateRatio = 0.0f;

    public void CountUpdateSent()
    {
        ++sentUpdateCount;
    }
    public void CountUpdateReceived()
    {
        ++receivedUpdateCount;
    }

    public void GetResult(out uint sent, out uint received, out float ratio)
    {
        sent = resultSentUpdate;
        received = resultReceivedUpdate;
        ratio = resultUpdateRatio;
    }

    protected virtual void Update()
    {
        var cd = timeCD - Time.deltaTime;
        if (cd < 0.0f)
        {
            cd += 1.0f;

            resultSentUpdate = sentUpdateCount;
            resultReceivedUpdate = receivedUpdateCount;

            resultUpdateRatio = 0.0f;
            if (sentUpdateCount > 0)
            {
                resultUpdateRatio = receivedUpdateCount / (float)sentUpdateCount;
            }

            sentUpdateCount = 0;
            receivedUpdateCount = 0;
        }
        timeCD = cd;
    }
}
