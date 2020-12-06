using System.Collections.Generic;

public class PathResponse
{
    public bool success = false;
    public string query;
    public string query_url;
    public List<Starfield.Response.Point> constellation;
}