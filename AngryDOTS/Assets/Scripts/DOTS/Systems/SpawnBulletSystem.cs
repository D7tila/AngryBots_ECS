using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

// Timing: Update this system before the group of systems that renders the geometry.
//  This helps us to allocate and set transform data of the entity before it's rendered,
//  to avoid having a 1-frame delay where you can see the entity at the origin
[UpdateBefore(typeof(TransformSystemGroup))]
partial struct SpawnBulletSystem : ISystem
{
    float timer;
    
    EntityQuery directoryQuery;
    
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        // These 2 lines makes the system not update unless at least 1 entity in the world
        // exists that has the Directory component, and 1 with the SpawnBulletRequest component
        state.RequireForUpdate<Directory>();
        
        directoryQuery = SystemAPI.QueryBuilder().WithAll<Directory>().Build();

        timer = 0f;
    }
    
    public void OnUpdate(ref SystemState state)
    {
        if(Settings.IsPlayerDead())
            return;
		
        timer += Time.deltaTime;
        
        var directory = directoryQuery.GetSingleton<Directory>();

        Entity bulletEntityPrefab = directory.bulletPrefab;
        EntityManager manager = state.EntityManager;
        
        if (Settings.Instance.useECSforBullets && 
            Input.GetButton("Fire1") && 
            timer >= Settings.Instance.fireRate)
        {
            Vector3 rotation = Settings.PlayerGunBarrelRotationEuler;
            rotation.x = 0f;

            if (Settings.Instance.spreadShot)
            {
                SpawnBulletSpread(ref manager, 
                    bulletEntityPrefab,
                    Settings.Instance.spreadAmount, 
                    Settings.PlayerGunBarrelPosition, 
                    rotation);
            }
            else
            {
                SpawnBullet(
                    ref manager,
                    bulletEntityPrefab,
                    Settings.PlayerGunBarrelPosition,
                    Quaternion.Euler(rotation));   
            }
            
            timer = 0f;
        }
    }
    
    // This method spawns bullets as entities instead of GameObjects
    [BurstCompile]
    private void SpawnBullet(
        ref EntityManager manager,
        Entity bulletEntityPrefab, 
        float3 position,
        Quaternion rotation)
    {
        // Use our EntityManager to instantiate a copy of the bullet entity
        Entity bullet = manager.Instantiate(bulletEntityPrefab);

        // Create a new LocalTransform component and give it the values needed to
        // be positioned at the barrel of the gun
        LocalTransform t = new LocalTransform()
        {
            Position = position,
            Rotation = rotation,
            Scale = 1f
        };

        // Set the component data we just created for the entity we just created
        manager.SetComponentData(bullet, t);
    }
    
    [BurstCompile]
    private void SpawnBulletSpread(
        ref EntityManager manager, 
        Entity bulletEntityPrefab,
        int spreadAmount,
        float3 position,
        float3 rotation)
    {
        // Most of this code is just boilerplate math to create a grid of rotations. Only the
        // relevant DOTS code is commented
        if (spreadAmount % 2 != 0) //No odd numbers to keep the spread even
            spreadAmount += 1;
        
        int max = spreadAmount / 2;
        int min = -max;
        int totalAmount = spreadAmount * spreadAmount;

        Vector3 tempRot = rotation;
        int index = 0;
        
        // NativeArrays are thread-safe data containers. In DOTS, they are a great way to work
        // with a lot of entities at once. They must be cleaned up though. This code creates
        // a temporary NativeArray with a size equal to the number of bullets we want to spawn
        NativeArray<Entity> bullets = new NativeArray<Entity>(totalAmount, Allocator.Temp);
        
        // By passing a NativeArray into the Instantiate() method of the EntityManager, many entities
        // are created at once and put into this NativeArray
        manager.Instantiate(bulletEntityPrefab, bullets);
        
        for (int x = min; x < max; x++)
        {
            tempRot.x = (rotation.x + 3 * x) % 360;

            for (int y = min; y < max; y++)
            {
                tempRot.y = (rotation.y + 3 * y) % 360;


                // Create a new LocalTransform component and give it the values needed to
                // be positioned at the barrel of the gun
                LocalTransform t = new LocalTransform
                {
                    Position = position,
                    Rotation = Quaternion.Euler(tempRot), // Use the temp rotation value needed to make the bullets spread out
                    Scale = 1f
                };

                // Set the component data we just created for the entity we just created
                manager.SetComponentData(bullets[index], t);

                index++;
            }
        }
        // Be sure to Dispose of the NativeArray or else you'll have a memory leak
        bullets.Dispose();
    }
}