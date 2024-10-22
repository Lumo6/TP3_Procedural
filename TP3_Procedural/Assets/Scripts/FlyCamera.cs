using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class FlyCamera : MonoBehaviour
{
    [Header("Camera")]
    [Tooltip("Multiplier for camera sensitivity.")]
    [Range(0f, 300)]
    public float sensitivity = 90f;
    [Tooltip("Multiplier for camera movement upwards.")]
    [Range(0f, 10f)]
    public float climbSpeed = 4f;
    [Tooltip("Multiplier for normal camera movement.")]
    [Range(0f, 20f)]
    public float normalMoveSpeed = 20f;
    [Tooltip("Multiplier for slower camera movement.")]
    [Range(0f, 5f)]
    public float slowMoveSpeed = 0.5f;
    [Tooltip("Multiplier for faster camera movement.")]
    [Range(0f, 40f)]
    public float fastMoveSpeed = 2f;
    [Tooltip("Rotation limits for the X-axis in degrees. X represents the lowest and Y the highest value.")]
    public Vector2 rotationLimitsX;
    [Tooltip("Rotation limits for the X-axis in degrees. X represents the lowest and Y the highest value.")]
    public Vector2 rotationLimitsY;
    [Tooltip("Whether the rotation on the X-axis should be limited.")]
    public bool limitXRotation = false;
    [Tooltip("Whether the rotation on the Y-axis should be limited.")]
    public bool limitYRotation = false;

    private Vector2 cameraRotation;


    public GameObject RotateObject;
    private GameObject MoveObject;
    private bool stPov;
    private bool CreatedChar = false;
    private Vector3 euler;
    private Vector3 Destination =  Vector3.zero;

    // Use this for initialization
    private void Start()
    {
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        stPov = false;
        euler = transform.localEulerAngles;
    }
    /* https://discussions.unity.com/t/move-transform-to-target-in-x-seconds/48455/3 */
    private IEnumerator MoveObjectToDestination(Transform objectTransform, Vector3 destination, float duration)
    {

        Vector3 startPosition = objectTransform.position;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            objectTransform.position = Vector3.Lerp(startPosition, destination, elapsedTime / duration);
            if (stPov)
            {
                Camera.main.transform.position = objectTransform.position;
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }
        objectTransform.position = destination;
    }
    
    private IEnumerator WaitforClick()
    {
        while (true)
        {
            if (Input.GetMouseButtonDown(0))
            {
                yield break;
            }
            yield return null;
        }
    }

    private IEnumerator WaitForClickAndMove()
    {
        while (Destination == Vector3.zero)
        {
            yield return StartCoroutine(WaitforClick());
            //Destination = GetClickPosition();
            Vector3 clickPos = GetClickPosition();
            Destination = new Vector3(clickPos.x, 1, clickPos.z);
        }
        StartCoroutine(MoveObjectToDestination(MoveObject.transform, Destination, 10f));
    }

    private Vector3 GetClickPosition()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            return hit.point;
        }
        return Vector3.zero;
    }

    // LateUpdate is called every frame, if the Behaviour is enabled
    private void LateUpdate()
    {
        //Rotation de la caméra
        if (!Input.GetKey(KeyCode.RightControl))
        {
            cameraRotation.x += Input.GetAxis("Mouse X") * sensitivity * Time.deltaTime;
            cameraRotation.y += Input.GetAxis("Mouse Y") * sensitivity * Time.deltaTime;

            if (limitXRotation)
            {
                cameraRotation.x = Mathf.Clamp(cameraRotation.x, rotationLimitsX.x, rotationLimitsX.y);
            }
            if (limitYRotation)
            {
                cameraRotation.y = Mathf.Clamp(cameraRotation.y, rotationLimitsY.x, rotationLimitsY.y);
            }

            transform.localRotation = Quaternion.AngleAxis(cameraRotation.x, Vector3.up);
            transform.localRotation *= Quaternion.AngleAxis(cameraRotation.y, Vector3.left);
        }
        //Gestion du positionnement de la caméra
        if (!stPov)
        {
            // Gestion du mouvement
            float moveSpeed = normalMoveSpeed;
            if (Input.GetKey(KeyCode.LeftShift))
            {
                moveSpeed *= fastMoveSpeed;
            }
            else if (Input.GetKey(KeyCode.LeftControl))
            {
                moveSpeed *= slowMoveSpeed;
            }

            transform.position += transform.right * moveSpeed * Input.GetAxis("Horizontal") * Time.deltaTime;
            transform.position += transform.forward * moveSpeed * Input.GetAxis("Vertical") * Time.deltaTime;

            // Gestion du mouvement vertical
            if (Input.GetKey(KeyCode.E))
            {
                transform.position += transform.up * climbSpeed * Time.deltaTime;
            }

            if (Input.GetKey(KeyCode.Q))
            {
                transform.position -= transform.up * climbSpeed * Time.deltaTime;
            }
        }

        // Gestion de la rotation de l'objet avec CTRL Droit
        if (Input.GetKey(KeyCode.RightControl))
        {
            euler.y -= Input.GetAxis("Mouse X") * sensitivity;
            RotateObject.transform.localEulerAngles = euler;
        }

        // Création de l'objet avec F2
        if (!CreatedChar)
        {
            if (Input.GetKey(KeyCode.F2))
            {
                MoveObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                //MoveObject.AddComponent<Rigidbody>();
                MoveObject.transform.position = new Vector3(5, 1, 0);
                CreatedChar = true;
                StartCoroutine(WaitForClickAndMove());

            }
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.LeftControl))
            {
                if (!stPov)
                {
                    StartCoroutine(MoveObjectToDestination(Camera.main.transform, MoveObject.transform.position, 1f));
                    stPov = true;
                }
                else
                {
                    StartCoroutine(MoveObjectToDestination(Camera.main.transform, new Vector3(0.0f, 1.0f, -10.0f), 1f));
                    stPov = false;
                }
            }
        }
    }

}

