using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Networking;

public class SkyGenerator : MonoBehaviour, Constellation.CompletionListener, DataService.IPathListener, Constellation.IAudioCallback, Starfield.IStarfieldListener
{
    private Dictionary<int, Star> _starScape;
    private LineRenderer _lineRenderer;
    private List<Constellation> _constellations;
    private Dictionary<int, AudioSource> _audioSources;

    public Starfield starfieldSource;
    public GameObject constellationPrefab;
    public GameObject starPrefab;
    public int spacing = 100;
    public int pathLength = 10;
    
    public AudioMixer audioMixer;
    public float crossfadeDuration = 4.0f;
    
    void Start()
    {
        _constellations = new List<Constellation>();
        _lineRenderer = GetComponent<LineRenderer>();
        _starScape = new Dictionary<int, Star>();
        _audioSources = new Dictionary<int, AudioSource>();
        
        starfieldSource.Initialize(this);
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
        audioMixer.FindSnapshot("Start").TransitionTo(crossfadeDuration);
    }

    public void OnPath(Queue<AudioPoint> path)
    {
        Queue<Star> starPath = new Queue<Star>();
        foreach (AudioPoint audioPoint in path)
        {
            if (_starScape.ContainsKey(audioPoint.id))
            {
                starPath.Enqueue(_starScape[audioPoint.id]);
            }
            else
            {
                AddToSkyscape(audioPoint);
                starPath.Enqueue(_starScape[audioPoint.id]);
            }
        }

        Constellation constellation = Instantiate(constellationPrefab, this.transform).GetComponent<Constellation>();
        constellation.SetCompletionListener(this);
        constellation.SetPath(starPath, this);
        _constellations.Add(constellation);
    }
    
    IEnumerator PlayAudio(AudioSource source, string url, string snapshot)
    {
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.MPEG))
        {
            yield return www.Send();

            if (www.isNetworkError)
            {
                Debug.LogError(www.error);
            }
            else
            {
                AudioClip audioClip = DownloadHandlerAudioClip.GetContent(www);
                source.PlayOneShot(audioClip, 0.5f);
                audioMixer.FindSnapshot(snapshot).TransitionTo(crossfadeDuration);
            }
        }
    }

    public void EnqueueAudioTrack(string url, int track)
    {
        Debug.Log($"SkyGenerator: enqueue {url} for track {track}");
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.outputAudioMixerGroup = audioMixer.FindMatchingGroups($"Step {track}")[0];
        _audioSources[track] = source;
        StartCoroutine(PlayAudio(source, url, $"{track}"));
    }

    public void OnStarfield()
    {
        foreach (AudioPoint audioPoint in starfieldSource.GetAudioPoints())
        {
            AddToSkyscape(audioPoint);
        }
    }
}
