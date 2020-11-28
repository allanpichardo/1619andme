using System.Collections;
using SQLite4Unity3d;
using UnityEngine;
#if !UNITY_EDITOR
using System.Collections;
using System.IO;
#endif
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Security.Policy;
using UnityEngine.Networking;

public class DataService: ScriptableObject  {
	
	public interface IPathListener
	{
		void OnPath(Queue<AudioPoint> path);
	}

	public interface IStarfieldListener
	{
		void OnStarfield(Starfield.Response response);
	}
	
	public const string BASE_URL = "https://ftdg-ai.allanpichardo.com";
	public const string ENDPOINT_PATH_FROM_ID = "search?id";
	public const string ENDPOINT_PATH_FROM_QUERY = "search?query";
	public const string ENDPOINT_STARFIELD = "starfield?limit";

	public static bool IsInAfrica(string region)
	{
		string[] africa = {"Senegal", "Ghana", "Angola", "Benin", "Nigeria"};
		foreach (string nation in africa)
		{
			if (region == nation)
			{
				return true;
			}
		}

		return false;
	}

	public static IEnumerator GetPath(AudioPoint start, IPathListener onPathListener, int length = 5)
	{
		string url = GetUrl(ENDPOINT_PATH_FROM_ID, start.id.ToString());
		return GetRequest(url, start, onPathListener, length);
	}

	public static void GetStarfield(IStarfieldListener listener, int limit = 100)
	{
		string url = GetUrl(ENDPOINT_STARFIELD, limit.ToString());
		GetRequest(url, listener, limit);
	}

	private static string GetUrl(string path, string param)
	{
		return string.Format("{0}/{1}={2}", BASE_URL, path, param);
	}
	
	private static IEnumerator GetRequest(string uri, AudioPoint start, IPathListener listener, int length = 5)
	{
		using (UnityWebRequest webRequest = UnityWebRequest.Get(uri))
		{
			yield return webRequest.SendWebRequest();

			if (webRequest.isNetworkError)
			{
				Debug.LogError("Path Error: " + webRequest.error);
			}
			else
			{
				PathResponse response = JsonUtility.FromJson<PathResponse>(webRequest.downloadHandler.text);
				if (response.success && listener != null)
				{
					Queue<AudioPoint>  path = new Queue<AudioPoint>();
					path.Enqueue(start);
					for (int i = 0; i < length; i++)
					{
						path.Enqueue(response.constellation[i]);
					}
					path.Enqueue(response.constellation.Last());
				}
				else
				{
					Debug.LogError("Couldn't get path");
				}
			}
		}
	}
	
	private static void GetRequest(string uri, IStarfieldListener listener, int length = 5)
	{
		using (UnityWebRequest webRequest = UnityWebRequest.Get(uri))
		{
			
			webRequest.SendWebRequest();
			while (!webRequest.isDone)
			{
				//block
			}

			if (webRequest.isNetworkError)
			{
				Debug.LogError("Starfield Error: " + webRequest.error);
			}
			else
			{
				Starfield.Response response = JsonUtility.FromJson<Starfield.Response>(webRequest.downloadHandler.text);
				listener.OnStarfield(response);
			}
		}
	}
	
}