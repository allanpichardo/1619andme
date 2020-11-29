using System.Collections.Generic;
using UnityEngine;

public class SkyGenerator : MonoBehaviour, Constellation.CompletionListener, DataService.IPathListener
{
    private Dictionary<int, Star> _starScape;
    private LineRenderer _lineRenderer;
    private List<Constellation> _constellations;

    public Starfield starfieldSource;
    public AudioSource musicSource;
    public GameObject constellationPrefab;
    public GameObject starPrefab;
    public int spacing = 100;
    public int pathLength = 10;
    // Start is called before the first frame update
    void Start()
    {
        starfieldSource.Initialize();
        
        _constellations = new List<Constellation>();
        _lineRenderer = GetComponent<LineRenderer>();
        _starScape = new Dictionary<int, Star>();
        
        foreach (AudioPoint audioPoint in starfieldSource.GetAudioPoints())
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
        Constellation constellation = GetActiveConstellation(star);
        if (constellation != null)
        {
            constellation.ContinueSequence();
        }
        else
        {
            if (!star.GetAudioPoint().IsInAfrica())
            {
                GeneratePath(star);
            }
        }
    }

    public void GeneratePath(Star start)
    {
        StartCoroutine(DataService.GetPath(start.GetAudioPoint(), this, pathLength));
    }
    
    private void AddToSkyscape(AudioPoint audioPoint, bool skipDictionary = false)
    {
        GameObject thisStar =
            Instantiate(starPrefab, transform.TransformPoint(audioPoint.GetPosition(spacing)), Quaternion.identity, this.transform);

        Star star = thisStar.GetComponent<Star>();
        star.SetAudioPoint(audioPoint);
        star.SetSkyGenerator(this);
        star.audioSource = musicSource;

        if (star.GetAudioPoint().IsInAfrica())
        {
            star.SetColor(Color.black);
        }

        if (!skipDictionary)
        {
            _starScape.Add(audioPoint.id, star);
        }
    }

    public void OnFinished(Constellation constellation)
    {
        _constellations.Remove(constellation);
    }

    public void OnPath(Queue<AudioPoint> path)
    {
        Queue<Star> starPath = new Queue<Star>();
        foreach (AudioPoint audioPoint in path)
        {
            starPath.Enqueue(_starScape[audioPoint.id]);
        }

        Constellation constellation = Instantiate(constellationPrefab, this.transform).GetComponent<Constellation>();
        constellation.SetPath(starPath);
        constellation.SetCompletionListener(this);
        _constellations.Add(constellation);
    }
}
