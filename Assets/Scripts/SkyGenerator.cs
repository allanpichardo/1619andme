using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkyGenerator : MonoBehaviour
{
    private DataService _dataService;
    private Dictionary<int, Star> _starScape;
    private LineRenderer _lineRenderer;
    
    public Camera mainCamera;
    public GameObject starPrefab;
    public int spacing = 100;
    public int pathLength = 10;
    // Start is called before the first frame update
    void Start()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        _dataService = new DataService("1619.db");
        _starScape = new Dictionary<int, Star>();
        
        AudioPoint start = _dataService.GetStartingPoint("Dominican Republic");

        foreach (AudioPoint audioPoint in _dataService.GetAllAudioPointsNotInAfrica())
        {
            AddToSkyscape(audioPoint);
        }
        
        GeneratePath(start);
    }

    public void GeneratePath(AudioPoint start)
    {
        Queue<AudioPoint> path = _dataService.GetPath(start, pathLength);
        foreach (AudioPoint audioPoint in path)
        {
            Debug.LogError(audioPoint.ToString());
        }

        DrawConstellation(path);
    }

    private void DrawConstellation(Queue<AudioPoint> path)
    {
        _lineRenderer.positionCount = path.Count;
        int position = 0;
        Star start = _starScape[path.Dequeue().id];
        _lineRenderer.SetPosition(position, start.transform.position);
        position++;

        foreach (AudioPoint point in path)
        {
            if (_starScape.ContainsKey(point.id))
            {
                Star star = _starScape[point.id];
                _lineRenderer.SetPosition(position, star.transform.position);
            }
            else
            {
                AddToSkyscape(point, true);
                _lineRenderer.SetPosition(position, transform.TransformPoint(point.GetPosition(spacing)));
            }
            position++;
        }
    }

    private void AddToSkyscape(AudioPoint audioPoint, bool skipDictionary = false)
    {
        GameObject thisStar =
            Instantiate(starPrefab, transform.TransformPoint(audioPoint.GetPosition(spacing)), Quaternion.identity, this.transform);

        Star star = thisStar.GetComponent<Star>();
        star.SetAudioPoint(audioPoint);

        if (!skipDictionary)
        {
            _starScape.Add(audioPoint.id, star);
        }
    }

    // Update is called once per frame
    void Update()
    {
        Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5F, 0.5F, 0));
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, float.MaxValue, 8))
        {
            Star star = hit.transform.gameObject.GetComponent<Star>();
            print(star.GetAudioPoint().region);
        }
    }
}
