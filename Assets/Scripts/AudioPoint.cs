using SQLite4Unity3d;
using UnityEditor;
using UnityEngine;

public class AudioPoint  {

    [PrimaryKey, AutoIncrement]
    public int id { get; set; }
    public float x { get; set; }
    public float y { get; set; }
    public float z { get; set; }
    public string region { get; set; }
    public double distance { get; set; }

//    public AudioClip GetAudioClip()
//    {
//        return AssetDatabase.LoadAssetAtPath<AudioClip>(string.Format("Assets/StreamingAssets/sounds/{0}.wav", id));
//    }

    public Vector3 GetPosition(int spacing)
    {
        return new Vector3(x, y, z) * spacing;
    }

    public override string ToString ()
    {
        return string.Format ("[Point: id={0}, x={1},  y={2}, z={3}, region={4}, distance={5}]", id, x, y, z, region, distance);
    }
}