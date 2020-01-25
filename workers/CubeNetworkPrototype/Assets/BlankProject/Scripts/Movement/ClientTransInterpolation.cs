using BlankProject;
using Improbable.Gdk.Subscriptions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClientTransInterpolation : MonoBehaviour
{
    public Vector3 nextPosition { get; set; }
    public Quaternion nextRotation { get; set; }
    public float deltaTime { get; set; }

    private Vector3 workerOrigin = Vector3.zero;

    private void Start()
    {
        workerOrigin = GetComponent<LinkedEntityComponent>().Worker.Origin;
    }

    private void Update()
    {
        if (Time.deltaTime < deltaTime)
        {
            // update position
            // deltaTime here must > 0, cause Time.deltaTime >= 0
            var percentage = Time.deltaTime / deltaTime;
            transform.position = Vector3.Lerp(transform.position,
                nextPosition + workerOrigin,
                percentage);

            // update rotation
            transform.rotation = Quaternion.Lerp(transform.rotation, nextRotation, percentage);

            deltaTime -= Time.deltaTime;
        }
        else
        {
            transform.position = nextPosition + workerOrigin;
            transform.rotation = nextRotation;
        }
    }
}
