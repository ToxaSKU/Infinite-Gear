using System.Collections;
using UnityEngine;

public class AIHandler : MonoBehaviour
{
    [SerializeField]
    CarHandler carHandler;

    [SerializeField]
    LayerMask otherCarsLayerMask;

    [SerializeField]
    MeshCollider meshCollider;

    RaycastHit[] raycastHits = new RaycastHit[1];
    bool isCarAhead = false;

    WaitForSeconds wait = new WaitForSeconds(0.2f);

    private void Awake()
    {
        if (CompareTag("Player"))
        {
            Destroy(this);
            return;
        }
    }

    void Start()
    {
        carHandler.SetMaxSpeed(Random.Range(4, 8));
        StartCoroutine(UpdateLessOftenCO());
    }

    IEnumerator UpdateLessOftenCO()
    {
        while (true)
        {
            yield return wait;
            isCarAhead = CheckIfOtherCarsIsAhead();
        }
    }

    bool CheckIfOtherCarsIsAhead()
    {
        meshCollider.enabled = false;

        int numberOfHits = Physics.BoxCastNonAlloc(
            transform.position,
            Vector3.one * 0.5f,
            transform.forward,
            raycastHits,
            Quaternion.identity,
            3f,
            otherCarsLayerMask
        );

        meshCollider.enabled = true;

        if (numberOfHits > 0)
            return true;

        return false;
    }

    void Update()
    {
        float accelerationInput = 1.0f;

        if (isCarAhead)
            accelerationInput = -0.5f;

        float steerInput = 0f;

        steerInput = Mathf.Clamp(steerInput, -1.0f, 1.0f);

        carHandler.SetInput(new Vector2(steerInput, accelerationInput));
    }

    private void OnEnable()
    {
        if (!CompareTag("Player"))
        {
            carHandler.SetMaxSpeed(Random.Range(8, 16));
        }
    }
}