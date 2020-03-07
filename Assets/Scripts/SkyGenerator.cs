using System.Collections.Generic;
using UnityEngine;

public class SkyGenerator : MonoBehaviour, Constellation.CompletionListener
{
    private DataService _dataService;
    private Dictionary<int, Star> _starScape;
    private LineRenderer _lineRenderer;
    private List<Constellation> _constellations;

    public GameObject constellationPrefab;
    public GameObject starPrefab;
    public int spacing = 100;
    public int pathLength = 10;
    // Start is called before the first frame update
    void Start()
    {
        _constellations = new List<Constellation>();
        _lineRenderer = GetComponent<LineRenderer>();
        _dataService = new DataService("1619.db");
        _starScape = new Dictionary<int, Star>();
        
        foreach (AudioPoint audioPoint in _dataService.GetAudioPoints())
        {
            AddToSkyscape(audioPoint);
        }
        
    }

    private Constellation GetActiveConstellation(Star star)
    {
        foreach (Constellation constellation in _constellations)
        {
            if (constellation.IsNext(star))
            {
                return constellation;
            }
        }

        return null;
    }

    public void OnLookAtStar(Star star)
    {
        Debug.Log($"Activated: {star.GetAudioPoint().ToString()}");
        Constellation constellation = GetActiveConstellation(star);
        if (constellation != null)
        {
            Debug.Log("Active Constellation");
            constellation.ContinueSequence();
        }
        else
        {
            if (!star.GetAudioPoint().IsInAfrica())
            {
                Debug.Log("New Constellation");
                constellation = Instantiate(constellationPrefab, this.transform).GetComponent<Constellation>();
                constellation.SetPath(GeneratePath(star));
                constellation.SetCompletionListener(this);
                _constellations.Add(constellation);
            }
        }
    }

    public Queue<Star> GeneratePath(Star start)
    {
        Queue<AudioPoint> path = _dataService.GetPath(start.GetAudioPoint(), pathLength);
        Queue<Star> starPath = new Queue<Star>();
        foreach (AudioPoint audioPoint in path)
        {
            starPath.Enqueue(_starScape[audioPoint.id]);
        }

        return starPath;
    }
    
    private void AddToSkyscape(AudioPoint audioPoint, bool skipDictionary = false)
    {
        GameObject thisStar =
            Instantiate(starPrefab, transform.TransformPoint(audioPoint.GetPosition(spacing)), Quaternion.identity, this.transform);

        Star star = thisStar.GetComponent<Star>();
        star.SetAudioPoint(audioPoint);
        star.SetSkyGenerator(this);

        if (!skipDictionary)
        {
            _starScape.Add(audioPoint.id, star);
        }
    }

    public void OnFinished(Constellation constellation)
    {
        Debug.Log("Removing Used Constellation");
        _constellations.Remove(constellation);
    }
}
