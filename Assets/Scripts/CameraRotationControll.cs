using UnityEngine;
using UnityEngine.EventSystems;

public class CameraRotationControll : MonoBehaviour
{
    public bool autoRotate;
    public float autoRotateSpeed = 2f;

    public bool controlRotation;
    public float controlRotationSpeed = 5f;

    public float smoothing = 5f;

    private int rotateDir = 1;
    private float rotationTarget;
    private Vector3 lastMousePosition;
    private Vector3 firstMousePosition;

    void Start()
    {
        rotationTarget = transform.rotation.eulerAngles.y;
        
    }

    void Update()
    {
        if(controlRotation && !EventSystem.current.IsPointerOverGameObject() && EventSystem.current.currentSelectedGameObject == null)
        {
            if(Input.GetMouseButtonDown(0))
            {
                lastMousePosition = Input.mousePosition;
                firstMousePosition = Input.mousePosition;
            }

            if(Input.GetMouseButton(0))
            {
                var dir = Input.mousePosition - lastMousePosition;
                lastMousePosition = Input.mousePosition;

                rotationTarget += dir.normalized.x * controlRotationSpeed;
            }

            if(Input.GetMouseButtonUp(0))
            {
                var dir = lastMousePosition - firstMousePosition;
                rotateDir = dir.normalized.x < 0 ? -1 : 1;
            }
        }

        if(autoRotate)
        {
            rotationTarget += rotateDir * autoRotateSpeed * Time.deltaTime;
        }

        Quaternion targetQuaternion = Quaternion.Euler(transform.rotation.eulerAngles.x, rotationTarget, transform.rotation.eulerAngles.z);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetQuaternion, smoothing * Time.deltaTime);
    }

    public void SetAutoRotate(bool x)
    {
        this.autoRotate = x;
    }

    public void SetControllRotation(bool x)
    {
        this.controlRotation = x;
    }

    public void UpdatePositionHeight(float y)
    {
        var p = transform.position;
        p.y = y / 2f;

        transform.position = p;
    }
}
