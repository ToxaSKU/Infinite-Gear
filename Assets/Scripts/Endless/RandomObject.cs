using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomObjact : MonoBehaviour
{
    [SerializeField]
    private Vector3 localRotationMin = Vector3.zero;
    [SerializeField]
    private Vector3 localRotationMax = Vector3.zero;

    [SerializeField]
    private float localScaleMultiplierMin = 0.8f;
    [SerializeField]
    private float localScaleMultiplierMax = 1.5f;

    void OnEnable()
    {
        transform.localRotation = Quaternion.Euler(
            Random.Range(localRotationMin.x, localRotationMax.x),
            Random.Range(localRotationMin.y, localRotationMax.y),
            Random.Range(localRotationMin.z, localRotationMax.z)
        );

        float randomScaleMultiplier = Random.Range(localScaleMultiplierMin, localScaleMultiplierMax);
        transform.localScale = transform.localScale * randomScaleMultiplier;
    }
}
