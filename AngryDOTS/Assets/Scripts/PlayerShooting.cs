/* PLAYER SHOOTING
 * This script manages the process of shooting bullets. Most of the code is general
 * or used for GameObject workflows. The DOTS items to be aware of in this script are:
	* - The entity member: manager
	* - The initialization in the Start() method
	* - The entity instantiation in the SpawnBulletECS() and SpawnBulletSpreadECS() methods
 *
 * Note: This code is for presentation and learning purposes. We're handing off the spawning
 *	of entities to the Systems, but based on MonoBehaviour input.
 * 
 *  You can have all of the input detection code be systems-based, please refer to our ECS samples:
 *	https://github.com/Unity-Technologies/EntityComponentSystemSamples
 */

using Unity.Entities;
using UnityEngine;

public class PlayerShooting : MonoBehaviour
{
	public bool useECS = false;
	public bool spreadShot = false;

	[Header("General")]
	public Transform gunBarrel;
	public ParticleSystem shotVFX;
	public AudioSource shotAudio;
	public float fireRate = .1f;
	public int spreadAmount = 20;

	[Header("Bullets")]
	public GameObject bulletPrefab;

	float timer;

	EntityManager manager;		// Member to hold an EntityManager reference


	void Start()
	{
		// If not using ECS, no need to do anything here
		if (!useECS) return;
		
		// Get a reference to an EntityManager which is how we will create and access entities
		manager = World.DefaultGameObjectInjectionWorld.EntityManager;
	}
	
	void Update()
	{
		if(Settings.IsPlayerDead())
			return;
		
		timer += Time.deltaTime;

		if (Input.GetButton("Fire1") && timer >= fireRate)
		{
			Vector3 rotation = gunBarrel.rotation.eulerAngles;
			rotation.x = 0f;

			if (useECS)
			{
				if (spreadShot)
					SpawnBulletSpreadECS(rotation);
				else
					SpawnBulletECS(rotation);
			}
			else
			{
				if (spreadShot)
					SpawnBulletSpread(rotation);
				else
					SpawnBullet(rotation);
			}

			timer = 0f;

			if (shotVFX)
				shotVFX.Play();

			if (shotAudio)
				shotAudio.Play();
		}
	}

	void SpawnBullet(Vector3 rotation)
	{
		Instantiate(bulletPrefab, gunBarrel.position, Quaternion.Euler(rotation));
	}

	void SpawnBulletSpread(Vector3 rotation)
	{
		int max = spreadAmount / 2;
		int min = -max;

		Vector3 tempRot = rotation;
		for (int x = min; x < max; x++)
		{
			tempRot.x = (rotation.x + 3 * x) % 360;

			for (int y = min; y < max; y++)
			{
				tempRot.y = (rotation.y + 3 * y) % 360;

				Instantiate(bulletPrefab, gunBarrel.position, Quaternion.Euler(tempRot));
			}
		}
	}

	// This method creates a request to spawn bullets as entities, instead of GameObjects
	void SpawnBulletECS(Vector3 rotation)
	{
		// Use our EntityManager to instantiate a copy of the bullet request
		var bulletRequestEntity = manager.CreateEntity();
		var spawnBulletRequest = new SpawnBulletRequest
		{
			gunBarrelPosition = gunBarrel.position,
			playerRotation = Quaternion.Euler(rotation)
		};
		
		// Set the component data we just created for the entity we just created
		manager.AddComponent<SpawnBulletRequest>(bulletRequestEntity);
		manager.SetComponentData(bulletRequestEntity, spawnBulletRequest);
	}

	// This method creates a request to spawn many bullets at a time
	// as entities, instead of GameObjects
	void SpawnBulletSpreadECS(Vector3 rotation)
	{
		// Use our EntityManager to instantiate a copy of the bullet spread request
		var bulletRequestEntity = manager.CreateEntity();
		var spawnBulletSpreadRequest = new SpawnBulletSpreadRequest
		{
			gunBarrelPosition = gunBarrel.position,
			playerRotation = rotation,
			spreadAmount = spreadAmount
		};
		
		// Set the component data we just created for the entity we just created
		manager.AddComponent<SpawnBulletSpreadRequest>(bulletRequestEntity);
		manager.SetComponentData(bulletRequestEntity, spawnBulletSpreadRequest);
	}
}

