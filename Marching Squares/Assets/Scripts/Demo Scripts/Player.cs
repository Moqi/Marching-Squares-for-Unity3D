using UnityEngine;
using System.Collections;

public class Player : MonoBehaviour
{
	Transform tr;
	CharacterController cc;
	
	Vector3 move = Vector3.zero;
	
	public float speed, jump, bulletEffect, bulletEffectRadius, bulletSpeed, fireRate;
	float nextFire;
	
	Camera cam;
	
	public Transform gun;
	public Bullet bullet;
	
	Plane plane;
	
	void Awake ()
	{
		tr = transform;
		cc = GetComponent<CharacterController>();
		cam = Camera.main;
		plane = new Plane(Vector3.back, Vector3.forward*gun.position.z);
	}
	
	void Update ()
	{		
		if (cc.isGrounded){ 
			move = Vector3.zero;
			if (Input.GetKey(KeyCode.Space))
				move.y = jump;
		}
		move.x = (Input.GetAxis("Horizontal")*speed);
		
		move += Physics.gravity*Time.deltaTime;
		cc.Move(move*Time.deltaTime);
		
		float d = 0f;
		Ray ray = cam.ScreenPointToRay(Input.mousePosition);
		if (plane.Raycast(ray, out d)){
			gun.LookAt(ray.origin+ray.direction*d, tr.up);
		}
		
		if ((Input.GetMouseButton(0) || Input.GetMouseButton(1)) && Time.time > nextFire){
			Bullet b = Instantiate(bullet, gun.position+gun.forward, Quaternion.identity) as Bullet;
			b.rigidbody.velocity = move + gun.forward * bulletSpeed;
			b.effectRadius = bulletEffectRadius;
			nextFire = Time.time + 1f/fireRate;
			if (Input.GetMouseButton(0))
				b.effect = bulletEffect;
			else
				b.effect = -bulletEffect;
		}
	}
	
	void OnGUI ()
	{
		GUILayout.BeginArea(new Rect(0f,64f, 256f, 256f));
		GUILayout.BeginHorizontal();
		GUILayout.Label("Speed");
		speed = GUILayout.HorizontalSlider(speed, 1f, 25f);
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();
		GUILayout.Label("Jump");
		jump = GUILayout.HorizontalSlider(jump, 1f, 25f);
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();
		GUILayout.Label("Bullet Effect");
		bulletEffect = GUILayout.HorizontalSlider(bulletEffect, 0f, 1f);
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();
		GUILayout.Label("Bullet Effect Radius");
		bulletEffectRadius = GUILayout.HorizontalSlider(bulletEffectRadius, 0.25f, 8f);
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();
		GUILayout.Label("Fire Rate");
		fireRate = GUILayout.HorizontalSlider(fireRate, 1f, 25f);
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();
		GUILayout.Label("Bullet Speed");
		bulletSpeed = GUILayout.HorizontalSlider(bulletSpeed, 1f, 25f);
		GUILayout.EndHorizontal();
		GUILayout.EndArea();
	}
}