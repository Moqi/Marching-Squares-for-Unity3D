using UnityEngine;
using System.Collections;

public class Editor : MonoBehaviour
{
	
	public MarchingSquaresTerrain terrain;
	Camera cam;
	public float effect;
	
	void Awake ()
	{
		cam = camera;
	}
	
	void Update ()
	{
		Ray ray = cam.ScreenPointToRay (Input.mousePosition);
		Plane plane = new Plane(Vector3.back, Vector3.zero);		
		float d = 0f;		
		
		if (plane.Raycast(ray, out d)){
			Vector3 intrsct = ray.origin+ray.direction*d;
			if (Input.GetMouseButton (0))
				terrain.Paint(new Vector2(intrsct.x, intrsct.y), 1f, effect * Time.deltaTime);
			else if (Input.GetMouseButton (1))
				terrain.Paint(new Vector2(intrsct.x, intrsct.y), 1f, -effect * Time.deltaTime);
		}
	}
}