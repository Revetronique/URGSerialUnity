using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
            // get the scanned points converted to orthogonal coordinate at all angles
            var scans = lrf.GetScanScreenPoint(false);
            // get chunks of satisfied elements
            var groups = grouping(scans, v => v != Vector2.zero);
            // get the average point in each chunk
            foreach (var group in groups)
            {
                var point = new Vector2(group.Average(v => v.x), group.Average(v => v.y));

                var screen = new Vector3(point.x, point.y, -Camera.main.transform.position.z);
                var world = Camera.main.ScreenToWorldPoint(screen);

                var obj = Instantiate(prefab, world, prefab.transform.rotation);
                Destroy(obj, lifespan);
            }

            //----- get all scanned points within the range -----
            //var points = lrf.GetScanScreenPoint();

            //foreach (var point in points)
            //{
            //    var screen = new Vector3(point.x, point.y, -Camera.main.transform.position.z);
            //    var world = Camera.main.ScreenToWorldPoint(screen);

            //    var obj = Instantiate(prefab, world, prefab.transform.rotation);
            //    Destroy(obj, lifespan);
            //}
            //---------------------------------------------------

            previous = Time.time;
        }
    }

    IEnumerable<IEnumerable<T>> grouping<T>(IEnumerable<T> source, System.Func<T, bool> predicate)
    {
        while (source.Any(predicate))
        {
            // find the first element satisfying the predicate
            var first = source.First(predicate);
            // get the group of the leading elements which satisfies the predicate
            source = source.SkipWhile(x => !x.Equals(first));
            yield return source.TakeWhile(predicate);
            // cutting chunk from the source elements
            source = source.SkipWhile(predicate);
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
