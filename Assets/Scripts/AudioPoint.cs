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

//    public AudioClip GetAudioClip()
//    {
//        return AssetDatabase.LoadAssetAtPath<AudioClip>(string.Format("Assets/StreamingAssets/sounds/{0}.wav", id));
//    }

    public Vector3 GetPosition(int spacing)
    {
        return new Vector3(x * spacing, y * spacing / 2.0f, z * spacing);
    }

    public override string ToString ()
    {
        return string.Format ("[Point: id={0}, x={1},  y={2}, z={3}, region={4}]", id, x, y, z, region);
    }

    public override bool Equals(object obj)
    {
        if ((obj == null) || this.GetType() != obj.GetType()) 
        {
            return false;
        }

        AudioPoint p = (AudioPoint) obj; 
        return id == p.id;
    }

    public override int GetHashCode()
    {
        return this.id;
    }

    public bool IsInAfrica()
    {
        string[] africa =
        {
            "Akan", "Benin", "Fulani", "Hausa", "Igbo", "Kanem", "Kangaba", "Kongo", "Mali", "Mande", "Wolof", "Yoruba"
        };
        foreach (string nation in africa)
        {
            if (region == nation)
            {
                return true;
            }
        }

        return false;
    }
}