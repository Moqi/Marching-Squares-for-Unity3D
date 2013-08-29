using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class Bullet : MonoBehaviour
{
	
	MarchingSquaresTerrain terrain;
	
	float death;
	
	void Awake()
	{
		terrain = FindObjectOfType(typeof(MarchingSquaresTerrain)) as MarchingSquaresTerrain;
		death = Time.time + 15f;
	}
	
	void LateUpdate()
	{
		if (death < Time.time)
			Destroy(gameObject);
	}
	
	public float effect = 0.1f, effectRadius = 1f;

	void OnCollisionEnter (Collision c)
	{
		if (c.collider.GetComponent<MarchingSquaresChunk>())
		{
			terrain.Paint(c.contacts[0].point, effectRadius, effect);
			Destroy(gameObject);
		}
	}
}