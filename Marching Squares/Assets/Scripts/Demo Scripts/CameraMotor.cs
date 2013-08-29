using UnityEngine;
using System.Collections;

public class CameraMotor : MonoBehaviour {
	
	Transform tr;
	
	public float speed;
	
	void Start ()
	{
		tr = transform;
	}
	
	void Update ()
	{
		Vector3 dir = new Vector3(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"), 0f);
		tr.Translate(dir.normalized * (speed * Time.deltaTime), Space.World);
	}
}