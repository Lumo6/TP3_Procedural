using System.Collections;
using UnityEngine;

public class FlyCamera : MonoBehaviour
{
    [Header("Camera Settings")]
    [Tooltip("Multiplier for camera sensitivity.")]
    [Range(0f, 300f)] public float sensitivity = 90f;
    [Tooltip("Multiplier for camera movement upwards.")]
    [Range(0f, 10f)] public float climbSpeed = 4f;
    [Tooltip("Multiplier for normal camera movement.")]
    [Range(0f, 20f)] public float normalMoveSpeed = 20f;
    [Tooltip("Multiplier for slower camera movement.")]
    [Range(0f, 5f)] public float slowMoveSpeed = 0.5f;
    [Tooltip("Multiplier for faster camera movement.")]
    [Range(0f, 40f)] public float fastMoveSpeed = 2f;

    [Header("Third Person Settings")]
    private float distanceFromObject = 15f;
    private float height = 2f;
    private float currentAngle = 0f;

    private Vector2 cameraRotation;
    public TP3_Terrain Terrain;

    private GameObject moveObject;

    private Coroutine currentCoroutine;

    private bool isFirstPersonView = false;
    private bool objectCreated = false;
    private bool freeWillMode = false;

    private Vector3 eulerAngles;
    private Vector3 destination = Vector3.zero;

    public LayerMask layerM;

    public Texture2D cursorTexture;

    private void Start()
    {
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        eulerAngles = transform.localEulerAngles;
    }

    private IEnumerator MoveObjectToDestination(Transform objectTransform, Vector3 destination, float duration)
    {
        Vector3 startPosition = objectTransform.position;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            Vector3 currentPosition = Vector3.Lerp(startPosition, destination, elapsedTime / duration);
            if (Physics.Raycast(new Vector3(currentPosition.x, currentPosition.y + 1f, currentPosition.z), Vector3.down, out RaycastHit hitInfo, Mathf.Infinity, layerM))
            {
                currentPosition.y = hitInfo.point.y+1;
            }


            objectTransform.position = currentPosition;

            if (isFirstPersonView)
            {
                Camera.main.transform.position = objectTransform.position;
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }
        if (Physics.Raycast(new Vector3(destination.x, destination.y + 50f, destination.z), Vector3.down, out RaycastHit finalHit, Mathf.Infinity, layerM))
        {
            destination.y = finalHit.point.y+1;
        }
        objectTransform.position = destination;
    }






    private IEnumerator WaitForClick()
    {
        while (!Input.GetMouseButtonDown(0))
        {
            Cursor.SetCursor(cursorTexture, Vector2.zero, CursorMode.Auto);

            yield return null;
        }
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);

    }

    private IEnumerator WaitForClickAndMove()
    {
        destination = Vector3.zero;
        yield return WaitForClick();
        destination = GetClickPosition();
        yield return StartCoroutine(MoveObjectToDestination(moveObject.transform, destination, 10f));
    }

    private Vector3 GetClickPosition()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Debug.Log(hit.point.ToString());
            return hit.point;

        }
        return Vector3.zero;
    }

    private void LateUpdate()
    {
        HandleCameraRotation();
        HandleCameraMovement();
        HandleObjectCreation();
        HandleObjectRotation();
        if (objectCreated)
        {
            HandleObjectMovement();
            HandleObjectHeight();

        }
    }

    private void HandleCameraRotation()
    {
        if (!Input.GetKey(KeyCode.RightControl))
        {
            currentAngle += Input.GetAxis("Mouse X") * sensitivity * Time.deltaTime;

            cameraRotation.y -= Input.GetAxis("Mouse Y") * sensitivity * Time.deltaTime;
            cameraRotation.y = Mathf.Clamp(cameraRotation.y, -80f, 80f);

            transform.localRotation = Quaternion.Euler(cameraRotation.y, currentAngle, 0f);
        }
    }

    private void HandleCameraMovement()
    {
        if (freeWillMode && isFirstPersonView)
        {
            StartCoroutine(MoveObjectToDestination(Camera.main.transform, moveObject.transform.position, 1f));
            return;
        }

        if (freeWillMode)
        {
            UpdateCameraPosition();
        }
        else if (!isFirstPersonView)
        {
            float moveSpeed = normalMoveSpeed;

            if (Input.GetKey(KeyCode.LeftControl)) moveSpeed *= fastMoveSpeed;
            if (Input.GetKey(KeyCode.LeftShift)) moveSpeed *= slowMoveSpeed;

            transform.position += transform.right * moveSpeed * Input.GetAxis("Horizontal") * Time.deltaTime;
            transform.position += transform.forward * moveSpeed * Input.GetAxis("Vertical") * Time.deltaTime;

            if (Input.GetKey(KeyCode.E)) transform.position += transform.up * climbSpeed * Time.deltaTime;
            if (Input.GetKey(KeyCode.Q)) transform.position -= transform.up * climbSpeed * Time.deltaTime;
        }
    }

    private void UpdateCameraPosition()
    {
        Vector3 offset = new Vector3(Mathf.Sin(currentAngle), height, Mathf.Cos(currentAngle)) * distanceFromObject;
        StartCoroutine(MoveObjectToDestination(Camera.main.transform, moveObject.transform.position + offset, 1f));
        transform.LookAt(moveObject.transform.position);
    }

    private void HandleObjectRotation()
    {
        if (Input.GetKey(KeyCode.RightControl))
        {
            eulerAngles.y -= Input.GetAxis("Mouse X") * sensitivity;
            Terrain.transform.localEulerAngles = eulerAngles;
        }
    }
    private void HandleObjectCreation()
    {
        if (!objectCreated && Input.GetKey(KeyCode.F2))
        {
            moveObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            MeshCollider meshCollider = moveObject.AddComponent<MeshCollider>();
            meshCollider.convex = true;
            Rigidbody rb = moveObject.AddComponent<Rigidbody>();
            moveObject.transform.position = new Vector3(5, 1, 0);
            objectCreated = true;
            currentCoroutine = StartCoroutine(WaitForClickAndMove());
            moveObject.layer = 2;
        }

        if (objectCreated && Input.GetKey(KeyCode.F2))
        {
            if (currentCoroutine != null)
            {
                StopCoroutine(currentCoroutine);
            }
            currentCoroutine = StartCoroutine(WaitForClickAndMove());
        }

        if (objectCreated && Input.GetKeyDown(KeyCode.LeftControl))
        {
            if (!isFirstPersonView)
            {
                StartCoroutine(MoveObjectToDestination(Camera.main.transform, moveObject.transform.position, 1f));
                isFirstPersonView = true;
            }
            else
            {
                Vector3 newPos = freeWillMode ? new Vector3(0f, 15f, -10f) : new Vector3(0f, 1f, -10f);
                StartCoroutine(MoveObjectToDestination(Camera.main.transform, newPos, 1f));
                isFirstPersonView = false;
            }
        }
    }
    private void HandleObjectMovement()
    {
        if (freeWillMode && objectCreated)
        {
            float moveSpeed = normalMoveSpeed;
            moveObject.transform.position += moveObject.transform.right * moveSpeed * Input.GetAxis("Horizontal") * Time.deltaTime;
            moveObject.transform.position += moveObject.transform.forward * moveSpeed * Input.GetAxis("Vertical") * Time.deltaTime;
            if (Input.GetKeyDown(KeyCode.Escape)) freeWillMode = false;
        }
        else if (!freeWillMode && objectCreated && Input.GetKeyDown(KeyCode.F3))
        {
            freeWillMode = true;
            StartCoroutine(MoveObjectToDestination(Camera.main.transform, new Vector3(0f, 15f, -10f), 1f));
        }
    }
    private void HandleObjectHeight()
    {
        if (Physics.Raycast(new Vector3(moveObject.transform.position.x, moveObject.transform.position.y + 10f, moveObject.transform.position.z), Vector3.down, out RaycastHit hitInfo, Mathf.Infinity, layerM))
        {
            Vector3 adjustedPosition = moveObject.transform.position;
            adjustedPosition.y = hitInfo.point.y + 1f; 
            moveObject.transform.position = adjustedPosition;
        }
    }
}
