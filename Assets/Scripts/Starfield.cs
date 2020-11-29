using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "FTDG/Starfield")]
public class Starfield : ScriptableObject, DataService.IStarfieldListener
{
    [Serializable]
    public class Response
    {
        [Serializable]
        public class Point
        {
            public int id;
            public float x;
            public float y;
            public float z;
            public string origin;
            public string url;
        }
        public bool success;
        public Point[] starfield;
    }

    public bool useRemote = false;
    public int size = 100;
    public TextAsset jsonFile;

    private List<AudioPoint> audioPoints;

    public void Initialize()
    {
        if (useRemote)
        {
            DataService.GetStarfield(this, size);
        }
        else if (jsonFile)
        {
            Response res = JsonUtility.FromJson<Response>(jsonFile.text);
            PopulateAudioPoints(res);
        }
    }

    public List<AudioPoint> GetAudioPoints()
    {
        return audioPoints;
    }
    
    private void PopulateAudioPoints(Response res)
    {
        audioPoints = new List<AudioPoint>();
        foreach (Response.Point point in res.starfield)
        {
            audioPoints.Add(new AudioPoint(point));
        }
    }

    public void OnStarfield(Response response)
    {
        if (response.success)
        {
            PopulateAudioPoints(response);
        }
        else
        {
            Debug.LogError("Unable to get starfield");
        }
    }
}
