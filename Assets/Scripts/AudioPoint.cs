using UnityEngine;

public class AudioPoint
{

    public int id;
    public float x;
    public float y;
    public float z;
    public string origin;
    public string url;

    public AudioPoint(Starfield.Response.Point point)
    {
        id = point.id;
        x = point.x;
        y = point.y;
        z = point.z;
        origin = point.origin;
        url = point.url;
    }
    
    public Vector3 GetPosition(int spacing)
    {
        return (new Vector3(x / 2.0f, Mathf.Abs(y) / 3.0f, z / 2.0f) * spacing) + (Vector3.up * 2.0f);
    }

    public override string ToString ()
    {
        return string.Format ("[Point: id={0}, x={1},  y={2}, z={3}, origin={4}]", id, x, y, z, origin);
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
        string[] africa = {"Senegal", "Ghana", "Angola", "Benin", "Nigeria"};
        foreach (string nation in africa)
        {
            if (origin == nation)
            {
                return true;
            }
        }

        return false;
    }
}