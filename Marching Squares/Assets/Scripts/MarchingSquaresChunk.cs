using UnityEngine;
using System.Collections.Generic;

public class MarchingSquaresChunk : MonoBehaviour
{
	MarchingSquaresTerrain terrain;
	int resolution;
	float scale, depth = 1f;
	float[,] field;
	const float min = 0f, max = 1f, mid = 0.5f;
	bool changed = false, optimized = true, initialized;
	byte consecutiveUpdates;
	
	public float this [int x, int y] {
		get {
			return field [x, y];
		}
		set {
			field [x, y] = Mathf.Max (min, Mathf.Min (max, value));
			changed = true;
		}
	}
	
	public event System.EventHandler PointChanged;
	
	Mesh mesh;
	List<Vector3> vertices;
	List<int> triangles;
	
	void Awake ()
	{
		mesh = new Mesh ();
		GetComponent<MeshFilter> ().mesh = mesh;
		vertices = new List<Vector3> ();
		triangles = new List<int> ();
	}
	
	void Start ()
	{
		if (!initialized)
			Initialize ();
	}
	
	void Initialize ()
	{
		field = new float[resolution, resolution];
		if (terrain) {
			if (terrain.generateGround && transform.position.y < 0f) {
				for (int y = 0; y < resolution; y++) {
					for (int x = 0; x < resolution; x++) {
						field [x, y] = 1f;
						changed = true;
					}
				}
			}
		}
		initialized = true;
	}
	
	public void Paint (Vector3 position, bool relativeToSelf, float radius, float effect)
	{
		if (!initialized)
			return;
		if (!relativeToSelf)
			position -= transform.position;
		if (position.x - radius > -1f || position.x + radius < resolution || position.y - radius > -1f || position.y + radius < resolution) {		
			for (int y = 0; y < resolution; y++)
				for (int x = 0; x < resolution; x++)
					if (((new Vector3 (x * scale, y * scale)) - position).sqrMagnitude < radius * radius)
						this [x, y] += effect;
		}
		changed = true;
	}
	
	public void Regenerate ()
	{
		changed = true;	
	}
	
	public void SetTerrain (MarchingSquaresTerrain t)
	{
		terrain = t;
		if (terrain) {
			transform.parent = terrain.transform;
			resolution = terrain.resolution;
			scale = terrain.scale;
			depth = terrain.depth;
			Initialize ();
		}
	}
	
	public void SetDimensions (int resolution, float scale)
	{
		if (!terrain) {
			this.resolution = resolution;
			this.scale = scale;
		} else {
			this.resolution = terrain.resolution;
			this.scale = terrain.scale;
		}
		field = new float[resolution, resolution];
	}
	
	void OnDestroy ()
	{
		if (terrain)
			terrain.RemoveMSChunk (this);	
	}
	
	void Update ()
	{
		if (changed) {
			if (consecutiveUpdates < 255)
				consecutiveUpdates++;
			GenerateMesh ();
			optimized = false;
			if (PointChanged != null) {
				PointChanged (this, System.EventArgs.Empty);
			}
			changed = false;
		} else if (consecutiveUpdates > 0)
			consecutiveUpdates--;
		else if (mesh.vertexCount > 0 && !optimized) {
			//mesh.Optimize();
			optimized = true;			
		}
	}
	
	void GenerateMesh ()
	{		
		vertices.Clear ();
		triangles.Clear ();
		
		Cell cell;
		float xs, ys;
		int hi = -1;
		
		for (int y = 0; y < resolution - 1; y++) {
			ys = y * scale;
			for (int x = 0; x < resolution - 1; x++) {
				xs = x * scale;
				cell = GetCell (x, y);
				hi = GenerateCellMesh (cell, xs, ys, hi);
			}
		}
		
		if (terrain) {
			MarchingSquaresChunk upperNeighbor = terrain.GetChunk (transform.position + Vector3.up * resolution * scale, false);
			if (upperNeighbor) {
				ys = (resolution - 1) * scale;
				for (int x = 0; x < resolution - 1; x++) {
					xs = x * scale;
					cell = GetUpperBoundaryCell (upperNeighbor, x);
					hi = GenerateCellMesh (cell, xs, ys, hi);
				}
			}
			
			MarchingSquaresChunk rightNeighbor = terrain.GetChunk (transform.position + Vector3.right * resolution * scale, false);
			if (rightNeighbor) {
				xs = (resolution - 1) * scale;
				for (int y = 0; y < resolution - 1; y++) {
					ys = y * scale;
					cell = GetRightBoundaryCell (rightNeighbor, y);
					hi = GenerateCellMesh (cell, xs, ys, hi);
				}
			}
			
			MarchingSquaresChunk upperRightNeighbor = terrain.GetChunk (transform.position + new Vector3 (resolution * scale, resolution * scale, 0f), false);
			if (upperRightNeighbor) {
				cell = new Cell (field [resolution - 1, resolution - 1], rightNeighbor ? rightNeighbor [0, resolution - 1] : 0f, upperNeighbor ? upperNeighbor [resolution - 1, 0] : 0f, upperRightNeighbor [0, 0]);
				cell.cellCase = GetCellCase (mid, cell.a, cell.b, cell.c, cell.d);
				hi = GenerateCellMesh (cell, (resolution - 1) * scale, (resolution - 1) * scale, hi);
			}
		}
		
		if (consecutiveUpdates > 2)
			mesh.MarkDynamic ();
		mesh.Clear ();
		mesh.vertices = vertices.ToArray ();
		mesh.triangles = triangles.ToArray ();
		
		vertices.Clear ();
		triangles.Clear ();
		
		mesh.RecalculateNormals ();
		mesh.RecalculateBounds ();
		
		GetComponent<MeshCollider> ().sharedMesh = null;
		GetComponent<MeshCollider> ().sharedMesh = mesh;
	}
	
	int GenerateCellMesh (Cell cell, float xs, float ys, int hi)
	{
		switch (cell.cellCase) {
		case -1 :
			Debug.LogError ("Invalid cell case!");
			break;
		case 0 :
			vertices.AddRange (new Vector3[4] {
				new Vector3 (xs, ys, 0),
				new Vector3 (xs + scale, ys, 0),
				new Vector3 (xs + scale, ys + scale, 0),
				new Vector3 (xs, ys + scale, 0)
			});
			triangles.AddRange (new int[6] {
				hi + 1,hi + 3,hi + 2,
				hi + 1,hi + 4,hi + 3
			});
			hi += 4;
			break;
		case 1 :
			vertices.AddRange (new Vector3[5]{
				new Vector3 (xs, ys, 0),
				new Vector3 (xs + scale, ys, 0),
				new Vector3 (xs + scale, ys + scale, 0),
				new Vector3 (xs + HLI (cell.c, cell.d), ys + scale, 0),
				new Vector3 (xs, ys + HLI (cell.a, cell.c), 0)
			});
			triangles.AddRange (new int[9]{
				hi + 1,hi + 5,hi + 2,
				hi + 2,hi + 5,hi + 4,
				hi + 2,hi + 4,hi + 3
			});
			hi += 5;
			GenerateCellEdge (new Vector2 (xs, ys + HLI (cell.a, cell.c)), new Vector2 (HLI (cell.c, cell.d), HLI (cell.c, cell.a)), false, hi);
			hi += 4;
			break;
		case 2 :
			vertices.AddRange (new Vector3[5]{
				new Vector3 (xs, ys, 0),
				new Vector3 (xs + scale, ys, 0),
				new Vector3 (xs, ys + scale, 0),
				new Vector3 (xs + HLI (cell.c, cell.d), ys + scale, 0),
				new Vector3 (xs + scale, ys + HLI (cell.b, cell.d), 0)
			});
			triangles.AddRange (new int[9]{
				hi + 1,hi + 3,hi + 4,
				hi + 1,hi + 4,hi + 5,
				hi + 1,hi + 5,hi + 2
			});
			hi += 5;
			GenerateCellEdge (new Vector2 (xs + HLI (cell.c, cell.d), ys + scale), new Vector2 (HLI (cell.d, cell.c), -HLI (cell.d, cell.b)), false, hi);
			hi += 4;
			break;					
		case 3 :
			vertices.AddRange (new Vector3[4] {
				new Vector3 (xs, ys, 0),
				new Vector3 (xs + scale, ys, 0),
				new Vector3 (xs + scale, ys + HLI (cell.b, cell.d), 0),
				new Vector3 (xs, ys + HLI (cell.a, cell.c), 0)
			});
			triangles.AddRange (new int[6] {
				hi + 1,hi + 3,hi + 2,
				hi + 1,hi + 4,hi + 3
			});
			hi += 4;
			GenerateCellEdge (new Vector2 (xs, ys + HLI (cell.a, cell.c)), new Vector2 (scale, HLI (cell.b, cell.d) - HLI (cell.a, cell.c)), false, hi);
			hi += 4;
			break;
		case 4 :
			vertices.AddRange (new Vector3[5]{
				new Vector3 (xs, ys, 0),
				new Vector3 (xs, ys + scale, 0),
				new Vector3 (xs + scale, ys + scale, 0),
				new Vector3 (xs + HLI (cell.a, cell.b), ys, 0),
				new Vector3 (xs + scale, ys + HLI (cell.b, cell.d), 0)
			});
			triangles.AddRange (new int[9]{
				hi + 2,hi + 3,hi + 5,
				hi + 2,hi + 5,hi + 4,
				hi + 2,hi + 4,hi + 1
			});
			hi += 5;
			GenerateCellEdge (new Vector2 (xs + HLI (cell.a, cell.b), ys), new Vector2 (HLI (cell.b, cell.a), HLI (cell.b, cell.d)), true, hi);
			hi += 4;
			break;
		case 5 :
			vertices.AddRange (new Vector3[6]{
				new Vector3 (xs, ys, 0),
				new Vector3 (xs, ys + HLI (cell.a, cell.c), 0),
				new Vector3 (xs + HLI (cell.a, cell.b), ys, 0),
				new Vector3 (xs + scale, ys + scale, 0),
				new Vector3 (xs + scale, ys + HLI (cell.b, cell.d), 0),
				new Vector3 (xs + HLI (cell.c, cell.d), ys + scale, 0)
			});
			triangles.AddRange (new int[6]{
				hi + 1, hi + 2, hi + 3, hi + 4, hi + 5, hi + 6
			});
			hi += 6;
			GenerateCellEdge (new Vector2 (xs, ys + HLI (cell.a, cell.c)), new Vector2 (HLI (cell.a, cell.b), -HLI (cell.a, cell.c)), false, hi);
			hi += 4;
			GenerateCellEdge (new Vector2 (xs + HLI (cell.c, cell.d), ys + scale), new Vector2 (HLI (cell.d, cell.c), -HLI (cell.d, cell.b)), true, hi);
			hi += 4;
			break;
		case 6 :
			vertices.AddRange (new Vector3[4] {
				new Vector3 (xs, ys, 0),
				new Vector3 (xs + HLI (cell.a, cell.b), ys, 0),
				new Vector3 (xs + HLI (cell.c, cell.d), ys + scale, 0),
				new Vector3 (xs, ys + scale, 0)
			});
			triangles.AddRange (new int[6] {
				hi + 1,hi + 4,hi + 3,
				hi + 1,hi + 3,hi + 2
			});
			hi += 4;
			GenerateCellEdge (new Vector2 (xs + HLI (cell.a, cell.b), ys), new Vector2 (HLI (cell.c, cell.d) - HLI (cell.a, cell.b), scale), true, hi);
			hi += 4;
			break;
		case 7 :
			vertices.AddRange (new Vector3[3] {
				new Vector3 (xs, ys, 0),
				new Vector3 (xs, ys + HLI (cell.a, cell.c), 0),
				new Vector3 (xs + HLI (cell.a, cell.b), ys, 0)							
			});
			triangles.AddRange (new int[3] {
				hi + 1,hi + 2,hi + 3
			});
			hi += 3;
			GenerateCellEdge (new Vector2 (xs, ys + HLI (cell.a, cell.c)), new Vector2 (HLI (cell.a, cell.b), -HLI (cell.a, cell.c)), false, hi);
			hi += 4;
			break;
		case 8 :
			vertices.AddRange (new Vector3[5]{
				new Vector3 (xs, ys + scale, 0),
				new Vector3 (xs + scale, ys, 0),
				new Vector3 (xs + scale, ys + scale, 0),
				new Vector3 (xs + HLI (cell.a, cell.b), ys, 0),
				new Vector3 (xs, ys + HLI (cell.a, cell.c), 0)
			});
			triangles.AddRange (new int[9]{
				hi + 3,hi + 5,hi + 1,
				hi + 3,hi + 4,hi + 5,
				hi + 3,hi + 2,hi + 4
			});
			hi += 5;
			GenerateCellEdge (new Vector2 (xs, ys + HLI (cell.a, cell.c)), new Vector2 (HLI (cell.a, cell.b), -HLI (cell.a, cell.c)), true, hi);
			hi += 4;
			break;
		case 9 :
			vertices.AddRange (new Vector3[4] {
				new Vector3 (xs + scale, ys, 0),
				new Vector3 (xs + HLI (cell.a, cell.b), ys, 0),
				new Vector3 (xs + HLI (cell.c, cell.d), ys + scale, 0),
				new Vector3 (xs + scale, ys + scale, 0)
			});
			triangles.AddRange (new int[6] {
				hi + 2,hi + 3,hi + 4,
				hi + 2,hi + 4,hi + 1
			});
			hi += 4;
			GenerateCellEdge (new Vector2 (xs + HLI (cell.a, cell.b), ys), new Vector2 (HLI (cell.c, cell.d) - HLI (cell.a, cell.b), scale), false, hi);
			hi += 4;
			break;
		case 10 :
			vertices.AddRange (new Vector3[6]{
				new Vector3 (xs, ys + scale, 0),
				new Vector3 (xs + HLI (cell.c, cell.d), ys + scale, 0),
				new Vector3 (xs, ys + HLI (cell.a, cell.c), 0),
				new Vector3 (xs + scale, ys, 0),
				new Vector3 (xs + HLI (cell.a, cell.b), ys, 0),
				new Vector3 (xs + scale, ys + HLI (cell.b, cell.d), 0)
			});
			triangles.AddRange (new int[6]{
				hi + 1, hi + 2, hi + 3, hi + 4, hi + 5, hi + 6
			});
			hi += 6;
			GenerateCellEdge (new Vector2 (xs, ys + HLI (cell.a, cell.c)), new Vector2 (HLI (cell.c, cell.d), HLI (cell.c, cell.a)), true, hi);
			hi += 4;
			GenerateCellEdge (new Vector2 (xs + HLI (cell.a, cell.b), ys), new Vector2 (HLI (cell.b, cell.a), HLI (cell.b, cell.d)), false, hi);
			hi += 4;
			break;
		case 11 :
			vertices.AddRange (new Vector3[3] {
				new Vector3 (xs + scale, ys, 0),
				new Vector3 (xs + HLI (cell.a, cell.b), ys, 0),
				new Vector3 (xs + scale, ys + HLI (cell.b, cell.d), 0)							
			});
			triangles.AddRange (new int[3] {
				hi + 1,hi + 2,hi + 3
			});
			hi += 3;
			GenerateCellEdge (new Vector2 (xs + HLI (cell.a, cell.b), ys), new Vector2 (HLI (cell.b, cell.a), HLI (cell.b, cell.d)), false, hi);
			hi += 4;
			break;
		case 12 :
			vertices.AddRange (new Vector3[4] {
				new Vector3 (xs, ys + scale, 0),
				new Vector3 (xs + scale, ys + scale, 0),
				new Vector3 (xs + scale, ys + HLI (cell.b, cell.d), 0),
				new Vector3 (xs, ys + HLI (cell.a, cell.c), 0)
			});
			triangles.AddRange (new int[6] {
				hi + 4,hi + 1,hi + 2,
				hi + 4,hi + 2,hi + 3
			});
			hi += 4;
			GenerateCellEdge (new Vector2 (xs, ys + HLI (cell.a, cell.c)), new Vector2 (scale, HLI (cell.b, cell.d) - HLI (cell.a, cell.c)), true, hi);
			hi += 4;
			break;
		case 13 :
			vertices.AddRange (new Vector3[3] {
				new Vector3 (xs + HLI (cell.c, cell.d), ys + scale, 0),
				new Vector3 (xs + scale, ys + scale, 0),
				new Vector3 (xs + scale, ys + HLI (cell.b, cell.d), 0)							
			});
			triangles.AddRange (new int[3] {
				hi + 1,hi + 2,hi + 3
			});
			hi += 3;
			GenerateCellEdge (new Vector2 (xs + HLI (cell.c, cell.d), ys + scale), new Vector2 (HLI (cell.d, cell.c), -HLI (cell.d, cell.b)), true, hi);
			hi += 4;
			break;
		case 14 :
			vertices.AddRange (new Vector3[3] {
				new Vector3 (xs, ys + scale, 0),
				new Vector3 (xs + HLI (cell.c, cell.d), ys + scale, 0),
				new Vector3 (xs, ys + HLI (cell.a, cell.c), 0)							
			});
			triangles.AddRange (new int[3] {
				hi + 1,hi + 2,hi + 3
			});
			hi += 3;
			GenerateCellEdge (new Vector2 (xs, ys + HLI (cell.a, cell.c)), new Vector2 (HLI (cell.c, cell.d), HLI (cell.c, cell.a)), true, hi);
			hi += 4;
			break;
		case 15 :
			break;
		}
		return hi;
	}
	
	void GenerateCellEdge (Vector2 origin, Vector2 edge, bool ccw, int hi)
	{
		vertices.AddRange (new Vector3[4]{
			new Vector3 (origin.x, origin.y, 0f),
			new Vector3 (origin.x + edge.x, origin.y + edge.y, 0f),
			new Vector3 (origin.x, origin.y, depth),
			new Vector3 (origin.x + edge.x, origin.y + edge.y, depth)
		});
		if (ccw)
			triangles.AddRange (new int[6]{
				hi + 1, hi + 4, hi + 3,
				hi + 1, hi + 2, hi + 4
			});
		else
			triangles.AddRange (new int[6]{
				hi + 1, hi + 3, hi + 4,
				hi + 1, hi + 4, hi + 2
			});
	}
	
	float HLI (float a, float b)
	{
		return scale * (mid - a) / (b - a);	
	}
	
	Cell GetCell (int x, int y)
	{
		Cell cell = new Cell (field [x, y], field [x + 1, y], field [x, y + 1], field [x + 1, y + 1]);
		cell.cellCase = GetCellCase (mid, cell.a, cell.b, cell.c, cell.d);
		return cell;
	}
	
	Cell GetUpperBoundaryCell (MarchingSquaresChunk neighbor, int x)
	{
		Cell cell = new Cell (field [x, resolution - 1], field [x + 1, resolution - 1], neighbor [x, 0], neighbor [x + 1, 0]);
		cell.cellCase = GetCellCase (mid, cell.a, cell.b, cell.c, cell.d);
		return cell;
	}
	
	Cell GetRightBoundaryCell (MarchingSquaresChunk neighbor, int y)
	{
		Cell cell = new Cell (field [resolution - 1, y], neighbor [0, y], field [resolution - 1, y + 1], neighbor [0, y + 1]);
		cell.cellCase = GetCellCase (mid, cell.a, cell.b, cell.c, cell.d);
		return cell;
	}
	
	struct Cell
	{
		public float a, b, c, d;
		public int cellCase;
		
		public Cell (float v1, float v2, float v3, float v4)
		{
			a = v1;
			b = v2;
			c = v3;
			d = v4;
			cellCase = -1;
		}
	}
		
	public static int GetCellCase (float mid, float a, float b, float c, float d)
	{
		if (a > mid) {
			if (b > mid) {
				if (c > mid) {
					if (d > mid)
						return 0;
					else
						return 2;
				} else {
					if (d > mid)
						return 1;
					else
						return 3;							
				}
			} else {
				if (c > mid) {
					if (d > mid)
						return 4;
					else
						return 6;
				} else {
					if (d > mid)
						return 5;
					else
						return 7;							
				}
			}
		} else {
			if (b > mid) {
				if (c > mid) {
					if (d > mid)
						return 8;
					else
						return 10;
				} else {
					if (d > mid)
						return 9;
					else
						return 11;							
				}
			} else {
				if (c > mid) {
					if (d > mid)
						return 12;
					else
						return 14;
				} else {
					if (d > mid)
						return 13;
					else
						return 15;							
				}
			}
		}
	}
}
