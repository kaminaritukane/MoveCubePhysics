using BlankProject;
using Improbable.Gdk.Subscriptions;
using UnityEngine;

namespace Scripts.Sphere
{
    public class CubeMovementBehaviour : MonoBehaviour
    {
        // to ensure we are authoritative
        //[Require] private SvUnitTransformWriter unitTransformWriter;

        private Vector3 workerOrigin;
        private float speed = 1.0f;

        private void Start()
        {
            workerOrigin = GetComponent<LinkedEntityComponent>().Worker.Origin;
            speed = 10.0f;// UnityEngine.Random.Range(1f, 10f);
        }

        private void Update()
        {
            var oriToPos = transform.position - workerOrigin;
            var dir = Vector3.Cross(oriToPos, Vector3.up).normalized;
            var step = dir * speed * Time.deltaTime;
            var oriToNewPos = (oriToPos + step).normalized;
            transform.position = oriToNewPos * oriToPos.magnitude + workerOrigin;

            //transform.position += dir * speed * Time.deltaTime;

            transform.rotation = Quaternion.LookRotation(dir);
        }
    }
}
