using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class Constellation: MonoBehaviour
{
    public interface IAudioCallback
    {
        void EnqueueAudioTrack(string url, int track);
    }
    
    private static readonly int Color45Edb685 = Shader.PropertyToID("Color_45EDB685");
    public float speed = 1.0f;
    public float fadeTime = 20.0f;
    
    private Queue<Star> _path;
    private List<string> _journeyParts;
    private int _step;
    private int _max;
    private LineRenderer _lineRenderer;
    private CompletionListener _completionListener;
    private Star _current;
    private Star _next;
    private UIController _uiController;
    private IAudioCallback _audioCallback;

    private string GetJourneyText()
    {
        string text = "";
        for (int i = 0; i < _journeyParts.Count; i++)
        {
            if (i == 0)
            {
                text = _journeyParts[i];
            }
            else
            {
                text = $"{text} • {_journeyParts[i]}";
            }
        }

        return text;
    }
    
    public void SetPath(Queue<Star> stars, IAudioCallback callback)
    {
        _path = stars;
        _step = 0;
        _max = stars.Count;
        
        _journeyParts = new List<string>();
        _uiController = FindObjectOfType<UIController>();
        _lineRenderer = GetComponent<LineRenderer>();
        _step++;
        _lineRenderer.positionCount = _step;
        _current = stars.Dequeue();
        _lineRenderer.SetPosition(_step - 1, _current.gameObject.transform.position);
        _journeyParts.Add(_current.GetAudioPoint().origin);
        _audioCallback = callback;
        
        if (_audioCallback != null)
        {
            _audioCallback.EnqueueAudioTrack(_current.GetAudioPoint().url, _step);
        }
        
        ContinueSequence();
    }

    public void ContinueSequence()
    {
        _next = GetNext();
        _step++;
        StartCoroutine(DrawLineSegment());
    }

    private IEnumerator DrawLineSegment()
    {
        float colorInterp = ((_step - 1.0f) / _max);
        Color lineColor = Color.Lerp(Color.white, Color.yellow, colorInterp);
        SetLineColor(lineColor);
        
        Vector3 start = _current.gameObject.transform.position;
        Vector3 end = _next.gameObject.transform.position;
        float step = 0.01f;
        float t = 0.0f;

        AudioPoint a = _current.GetAudioPoint();
        AudioPoint b = _next.GetAudioPoint();
        
        _current = _next;
        _journeyParts.Add(_current.GetAudioPoint().origin);
        // _uiController.ShowJourneyText(GetJourneyText(), lineColor); //todo: reconsider UI

        _lineRenderer.positionCount = _step;
        _lineRenderer.SetPosition(_step - 1, start);

        while (t < 1.0f)
        {
            _lineRenderer.SetPosition(_step - 1, Vector3.Lerp(start, end, t));
            t += Time.deltaTime * speed;
            yield return new WaitForEndOfFrame();
        }
        
        _current.OnPointerEnter();

        if (_audioCallback != null)
        {
            _audioCallback.EnqueueAudioTrack(_current.GetAudioPoint().url, _step);
        }
        
        yield return DataService.DrawLineToMap(a, b);
        
        if(IsFinished())
        {
            RevealStar(_current);
            yield return new WaitForSeconds(10.0f);
            _completionListener.OnFinished(this, _step);
            BeginFadeSequence();
        }
        else
        {
            ContinueSequence();
        }
    }

    private void BeginFadeSequence()
    {
        Color startColor = Color.yellow;
        Color endColor = Color.clear;
        StartCoroutine(FadeAnimation(startColor, endColor));
    }

    private IEnumerator FadeAnimation(Color startColor, Color endColor)
    {
        float elapsed = 0.0f;
        while (elapsed < fadeTime)
        {
            SetLineColor(Color.Lerp(startColor, endColor, elapsed / fadeTime));
            elapsed += Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }
    }

    private void RevealStar(Star current)
    {
        current.SetColor(Color.red);
        SetLineColor(Color.yellow);
    }
    
    private void SetLineColor(Color color)
    {
        _lineRenderer.material.SetColor(Color45Edb685, color);
    }

    private Star GetNext()
    {
        return _path.Dequeue();
    }

    public bool IsNext(Star star)
    {
        return star.GetAudioPoint().Equals(_next.GetAudioPoint());
    }

    public bool IsFinished()
    {
        return _path.Count == 0;
    }
    
    public void SetCompletionListener(CompletionListener listener)
    {
        _completionListener = listener;
    }
    
    public interface CompletionListener
    {
        void OnFinished(Constellation constellation, int step);
    }
}
