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

public class DataService  {

	private SQLiteConnection _connection;

	public DataService(string databaseName){

#if UNITY_EDITOR
            var dbPath = string.Format(@"Assets/StreamingAssets/db/{0}", databaseName);
#else
        // check if file exists in Application.persistentDataPath
        var filepath = string.Format("{0}/{1}", Application.persistentDataPath, databaseName);

        if (!File.Exists(filepath))
        {
            Debug.Log("Database not in Persistent path");
            // if it doesn't ->
            // open StreamingAssets directory and load the db ->

#if UNITY_ANDROID 
            var loadDb = new WWW("jar:file://" + Application.dataPath + "!/assets/" + databaseName);  // this is the path to your StreamingAssets in android
            while (!loadDb.isDone) { }  // CAREFUL here, for safety reasons you shouldn't let this while loop unattended, place a timer and error check
            // then save to Application.persistentDataPath
            File.WriteAllBytes(filepath, loadDb.bytes);
#elif UNITY_IOS
                 var loadDb = Application.dataPath + "/Raw/" + databaseName;  // this is the path to your StreamingAssets in iOS
                // then save to Application.persistentDataPath
                File.Copy(loadDb, filepath);
#elif UNITY_WP8
                var loadDb = Application.dataPath + "/StreamingAssets/db/" + databaseName;  // this is the path to your StreamingAssets in iOS
                // then save to Application.persistentDataPath
                File.Copy(loadDb, filepath);

#elif UNITY_WINRT
		var loadDb = Application.dataPath + "/StreamingAssets/db/" + databaseName;  // this is the path to your StreamingAssets in iOS
		// then save to Application.persistentDataPath
		File.Copy(loadDb, filepath);
		
#elif UNITY_STANDALONE_OSX
		var loadDb = Application.dataPath + "/Resources/Data/StreamingAssets/db/" + databaseName;  // this is the path to your StreamingAssets in iOS
		// then save to Application.persistentDataPath
		File.Copy(loadDb, filepath);
#else
	var loadDb = Application.dataPath + "/StreamingAssets/db/" + databaseName;  // this is the path to your StreamingAssets in iOS
	// then save to Application.persistentDataPath
	File.Copy(loadDb, filepath);

#endif

            Debug.Log("Database written");
        }

        var dbPath = filepath;
#endif
            _connection = new SQLiteConnection(dbPath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create);
        Debug.Log("Final PATH: " + dbPath);     

	}
	
	public IEnumerable<AudioPoint> GetAllAudioPointsGrouped(){
		return _connection.Query<AudioPoint>("select * from clips_small group by region");
	}

	public IEnumerable<AudioPoint> GetTestAudioPoint()
	{
		return _connection.Query<AudioPoint>("select * from clips_small where id = 1");
	}

	public IEnumerable<AudioPoint> GetAudioPoints()
	{
		string query =
			$"SELECT * FROM clips_small";
		return _connection.Query<AudioPoint>(query);
	}
	
	public IEnumerable<AudioPoint> GetAllAudioPointsNotInAfrica(){
		return _connection.Query<AudioPoint>("select * from clips_small where region not in ('Akan', 'Benin', 'Fulani', 'Hausa', 'Igbo', 'Kanem', 'Kangaba', 'Kongo', 'Mali', 'Mande', 'Wolof', 'Yoruba')");
	}

	public AudioPoint GetStartingPoint(string region)
	{
		string[] p = {region};
		return _connection.Query<AudioPoint>("select * from clips_small where region = ?", p).FirstOrDefault();
	}

	public AudioPoint GetNextNearest(AudioPoint start, bool includeAfrica = false)
	{
		string query = includeAfrica
			? "select c.*, ((? - c.x) * (? - c.x)) + ((? - c.y) * (? - c.y)) + ((? - c.z) * (? - c.z)) as distance from clips_small c where c.region != ? order by distance asc limit 1;"
			: "select c.*, ((? - c.x) * (? - c.x)) + ((? - c.y) * (? - c.y)) + ((? - c.z) * (? - c.z)) as distance from clips_small c where c.region != ? and region not in ('Akan', 'Benin', 'Fulani', 'Hausa', 'Igbo', 'Kanem', 'Kangaba', 'Kongo', 'Mali', 'Mande', 'Wolof', 'Yoruba') order by distance limit 1;";
		
		object[] p = {start.x, start.x, start.y, start.y, start.z, start.z, start.region};
		return  _connection.Query<AudioPoint>(query, p).FirstOrDefault();
	}

//	public Stack<AudioPoint> GetPath(AudioPoint start, int length = 5)
//	{
//		Stack<AudioPoint> stack = new Stack<AudioPoint>();
//		stack.Push(start);
//		for (int i = 0; i < length; i++)
//		{
//			stack.Push(GetNextNearest(stack.Peek()));
//		}
//
//		while (!IsInAfrica(stack.Peek().region))
//		{
//			stack.Push(GetNextNearest(stack.Peek(), true));
//		}
//
//		Stack<AudioPoint> ordered = new Stack<AudioPoint>();
//		foreach (AudioPoint audioPoint in stack)
//		{
//			ordered.Push(audioPoint);
//		}
//
//		return ordered;
//	}

	private bool IsInAfrica(string region)
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

	public Queue<AudioPoint> GetPath(AudioPoint start, int length = 5)
	{
		string query =
			"select c.*, ((? - c.x) * (? - c.x)) + ((? - c.y) * (? - c.y)) + ((? - c.z) * (? - c.z)) as distance from clips_small c order by distance;";
		object[] p = {start.x, start.x, start.y, start.y, start.z, start.z, length};
		IEnumerable<AudioPoint> results =  _connection.Query<AudioPoint>(query, p);

		OrderedDictionary points = new OrderedDictionary();
		int step = 0;
		foreach (AudioPoint audioPoint in results)
		{
			if (!points.Contains(audioPoint.region))
			{
				if (step < length)
				{
					if (!IsInAfrica(audioPoint.region))
					{
						points.Add(audioPoint.region, audioPoint);
						step++;
					}
				}
				else
				{
					if (IsInAfrica(audioPoint.region))
					{
						points.Add(audioPoint.region, audioPoint);
						break;
					}
				}
			}
		}
		
		Queue<AudioPoint> path = new Queue<AudioPoint>();
		foreach (DictionaryEntry dictionaryEntry in points)
		{
			path.Enqueue((AudioPoint) dictionaryEntry.Value);
		}
		
		return path;
	}
	
}