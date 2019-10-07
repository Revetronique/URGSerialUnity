using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Effect : MonoBehaviour
{
    /// <summary>
    /// gameobject of the effect
    /// </summary>
    [SerializeField]
    GameObject prefab;

    /// <summary>
    /// interval to scan
    /// </summary>
    [SerializeField, Range(0.01f, 5f)]
    float interval = 0.5f;

    /// <summary>
    /// span to be appeared
    /// </summary>
    [SerializeField, Range(0.1f, 10f)]
    float lifespan = 1.0f;

    /// <summary>
    /// LRF controller
    /// </summary>
    [SerializeField]
    LRFClick lrf;

    float previous = 0;

    // Start is called before the first frame update
    void Start()
    {
        previous = Time.time;
    }

    //// Update is called once per frame
    void Update()
    {
        if (Time.time - previous > interval)
        {
            var points = lrf.GetScanScreenPoint();

            foreach (var point in points)
            {
                var screen = new Vector3(point.x, point.y, -Camera.main.transform.position.z);
                var world = Camera.main.ScreenToWorldPoint(screen);

                var obj = Instantiate(prefab, world, prefab.transform.rotation);
                Destroy(obj, lifespan);
            }

            previous = Time.time;
        }
    }

    public void Generate(Vector2 point)
    {
        //convert screen point to coordination points of the world
        var screen = new Vector3(point.x, point.y, -Camera.main.transform.position.z);
        var world = Camera.main.ScreenToWorldPoint(screen);

        var obj = Instantiate(prefab, world, prefab.transform.rotation);
        Destroy(obj, lifespan);
    }
}
