using UnityEngine;
using System.Collections.Generic;

public class MarchingSquaresTerrain : MonoBehaviour
{
	
	List<MarchingSquaresChunk> chunks;
	public int resolution;
	public float scale, depth;
	public bool generateGround;
	float resolutionTimesScale;
	public MarchingSquaresChunk MSChunkPrefab;
	
	public float this [Vector3 point] {
		get {
			MarchingSquaresChunk chunk = GetChunk (point, true);
			return this [chunk, point];
		}
		set {
			MarchingSquaresChunk chunk = GetChunk (point, true);
			this [chunk, point] = value;
		}
	}
	
	public float this [MarchingSquaresChunk chunk, Vector3 point] {
		get {
			point.z = 0;
			Vector3 p = (point - chunk.transform.position) / scale;
			p.x = Mathf.Round (Mathf.Min (resolution - 1, p.x));
			p.y = Mathf.Round (Mathf.Min (resolution - 1, p.y));
			return chunk [(int)p.x, (int)p.y];
		}
		set {
			point.z = 0f;
			Vector3 p = (point - chunk.transform.position) / scale;
			int x = Mathf.RoundToInt (Mathf.Min (resolution - 1, p.x));
			int y = Mathf.RoundToInt (Mathf.Min (resolution - 1, p.y));
			chunk [x, y] = value;
			if (x == 0)
				UpdateNeighbor (chunk, Vector3.left);
			if (x == resolution - 1)
				UpdateNeighbor (chunk, Vector3.right);
			if (y == 0)
				UpdateNeighbor (chunk, Vector3.down);
			if (y == resolution - 1)
				UpdateNeighbor (chunk, Vector3.up);
			if (x == 0 && y == 0)
				UpdateNeighbor (chunk, Vector3.down + Vector3.left);
			if (x == resolution - 1 && y == resolution - 1)
				UpdateNeighbor (chunk, Vector3.up + Vector3.right);
		}
	}
	
	public MarchingSquaresChunk this [int x, int y] {
		get {
			return GetChunk (new Vector3 (x * resolutionTimesScale, y * resolutionTimesScale, 0f), true);
		}
	}
	
	public void Paint (Vector3 position, float radius, float effect)
	{
		if (radius > resolutionTimesScale)
			print ("radius greater than res/scale");
		position.z = 0f;
		MarchingSquaresChunk chunk = GetChunk (position, true);
		chunk.Paint (position, false, radius, effect);
		
		Vector3 u = Vector3.up * radius,
				r = Vector3.right * radius,
				ur = (Vector3.up + Vector3.right).normalized * radius,
				ul = (Vector3.up + Vector3.left).normalized * radius,
				v = chunk.transform.position;
		
		float rmots = (resolution - 1) * scale;
		
		if ((position + r).x > v.x + rmots)
			GetChunk (position + r, true).Paint (position, false, radius, effect);
		if ((position + u).y > v.y + rmots)
			GetChunk (position + u, true).Paint (position, false, radius, effect);		
		if ((position - r).x < v.x)
			GetChunk (position - r, true).Paint (position, false, radius, effect);
		if ((position - u).y < v.y)
			GetChunk (position - u, true).Paint (position, false, radius, effect);
		
		if ((position + ur).x > v.x + rmots && (position + ur).y > v.y + rmots)
			GetChunk (position + ur, true).Paint (position, false, radius, effect);		
		if ((position + ul).x < v.x && (position + ul).y > v.y + rmots)
			GetChunk (position + ul, true).Paint (position, false, radius, effect);		
		if ((position - ul).x > v.x + rmots && (position - ul).y < v.y)
			GetChunk (position - ul, true).Paint (position, false, radius, effect);
		if ((position - ur).x < v.x && (position - ur).y < v.y)
			GetChunk (position - ur, true).Paint (position, false, radius, effect);
	}
	
	void Awake ()
	{
		chunks = new List<MarchingSquaresChunk> ();
		resolutionTimesScale = resolution * scale;
	}
	
	void Update ()
	{
		Vector3 cp = ValidatePosition (Camera.main.transform.position),
				up = Vector3.up * resolutionTimesScale,
				right = Vector3.right * resolutionTimesScale;
		
		GetChunk (cp, true);
		GetChunk (cp + right, true);
		GetChunk (cp + right + up, true);
		GetChunk (cp + up, true);
		GetChunk (cp + up - right, true);
		GetChunk (cp - right, true);
		GetChunk (cp - right - up, true);
		GetChunk (cp - up, true);
		GetChunk (cp - up + right, true);
	}
	
	public void RemoveChunk (MarchingSquaresChunk chunk)
	{
		chunks.Remove (chunk);	
	}
	
	void UpdateNeighbor (MarchingSquaresChunk chunk, Vector3 dir)
	{
		MarchingSquaresChunk n = GetChunk (chunk.transform.position + dir.normalized * resolutionTimesScale, false);
		if (n)
			n.Regenerate ();
	}
	
	void UpdateNeighbors (MarchingSquaresChunk chunk)
	{		
		UpdateNeighbor (chunk, Vector3.down * resolutionTimesScale);
		UpdateNeighbor (chunk, Vector3.left * resolutionTimesScale);
		UpdateNeighbor (chunk, (Vector3.down + Vector3.left) * resolutionTimesScale);
	}
	
	public MarchingSquaresChunk GetChunk (Vector3 position, bool addIfNotFound)
	{
		position = ValidatePosition (position);
		foreach (MarchingSquaresChunk c in chunks)
			if (c.transform.position.Equals (position))
				return c;
		if (addIfNotFound) {
			MarchingSquaresChunk chunk = Instantiate (MSChunkPrefab, position, Quaternion.identity) as MarchingSquaresChunk;
			chunk.SetTerrain (this);
			chunks.Add (chunk);
			UpdateNeighbors (chunk);
			return chunk;
		}
		return null;
	}
	
	Vector3 ValidatePosition (Vector3 position)
	{
		Vector3 p = position;
		p.z = 0f;
		p /= resolutionTimesScale;
		p.x = Mathf.Floor (p.x);
		p.y = Mathf.Floor (p.y);
		p *= resolutionTimesScale;
		
		return p;
	}
	
	float nScale = 1f;
	
	void OnGUI ()
	{
		GUILayout.BeginArea (new Rect (0f, 0f, 256f, 64f));
		GUILayout.BeginHorizontal ();
		GUILayout.Label ("Terrain Scale");
		nScale = GUILayout.HorizontalSlider (nScale, 0.25f, 2f);
		GUILayout.EndHorizontal ();
		GUILayout.BeginHorizontal ();
		if (GUILayout.Button ("Reset Terrain")) {
			GameObject.FindGameObjectWithTag ("Player").transform.position = new Vector3 (0f, 5f, 0.5f);
			scale = nScale;
			resolutionTimesScale = resolution * scale;
			foreach (MarchingSquaresChunk c in chunks)
				Destroy (c.gameObject);
		}
		GUILayout.EndHorizontal ();
		GUILayout.EndArea ();
	}
}