using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private float smoothTime;
    [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f);

    private Transform target;
    private Vector3 velocity;

    private void LateUpdate()
    {
        if (target == null)
            return;

        Vector3 desiredPosition = target.position + offset;
        transform.position = Vector3.Lerp(
            transform.position,
            desiredPosition,

            1-Mathf.Pow(smoothTime, Time.deltaTime)
        );
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
}
