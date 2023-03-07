/* Created by U.K.L. on 8/16/2022
 * 
 * Finds the nearest egg and points to that location using an indiactor.
 * 
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectLocator : MonoBehaviour
{
    GameObject[] _eggs;
    GameObject _nearestEgg;
    Quaternion _nextRotation;
    public GameObject prefab_indicator;
    private GameObject _indicator;
    public float rotationalSpeed = 4f;
    void Start()
    {

        _indicator = Instantiate(prefab_indicator, transform);
    }

    void Update()
    {
        UpdateIndicator(Time.deltaTime * rotationalSpeed);

    }

    private void FixedUpdate()
    {
        _eggs = GameObject.FindGameObjectsWithTag("Egg");
        _nearestEgg = nearestEgg();
    }

    GameObject nearestEgg()
    {
        GameObject _returnedObj = null;
        float min = Mathf.Infinity;
        foreach (GameObject egg in _eggs)
        {
            Vector3 eggPos = egg.transform.position;
            Vector3 playerPos = transform.position;

            float distance = Mathf.Sqrt( Mathf.Pow(eggPos.x - playerPos.x, 2) + Mathf.Pow(eggPos.y - playerPos.y,2) + Mathf.Pow(eggPos.z - playerPos.z,2));
            if (min > distance)
            {
                min = distance;
                _returnedObj = egg;
            }

        }

        return _returnedObj;
    }

    void UpdateIndicator(float t)
    {
        if (_nearestEgg == null)
            return;
        Vector3 origin = transform.position;
        Vector3 destination = _nearestEgg.transform.position;

        Vector3 direction = (destination - origin).normalized;
        //The amount it is outwards from the player.
        float dist = Vector3.Distance(origin, destination) * 0.25f;
        float outwards = Mathf.Clamp(transform.localScale.magnitude * 0.55f * dist, 1f, 3);
        _indicator.transform.position = Vector3.Lerp(_indicator.transform.position, origin + direction * outwards, t);

        //Rotate correctly
        //_indicator.transform.rotation = Quaternion.LookRotation(direction);
        _nextRotation = Quaternion.LookRotation(direction);
        _indicator.transform.rotation = Quaternion.Slerp(_indicator.transform.rotation, _nextRotation, t);

        ColorChangeIndicator colorIndic = _indicator.GetComponentInChildren<ColorChangeIndicator>();
        colorIndic.closenessToEgg = 2 - Vector3.Distance(origin, destination)/4.25f;
    }


    private void OnDrawGizmos()
    {
        if (_nearestEgg == null)
            return;
        Ray r = new Ray(transform.position, (_nearestEgg.transform.position- transform.position).normalized);
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(r);
    }
}
