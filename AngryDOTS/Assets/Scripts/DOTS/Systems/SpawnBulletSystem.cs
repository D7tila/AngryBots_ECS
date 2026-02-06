using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

partial struct SpawnBulletSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        // These 2 lines makes the system not update unless at least one entity in
        // the world exists that has the Directory component, and 1 with the SpawnBulletRequest
        state.RequireForUpdate<Directory>();
        state.RequireForUpdate<SpawnBulletRequest>();
    }
    
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var directoryQuery = SystemAPI.QueryBuilder().WithAll<Directory>().Build();
        var directory = directoryQuery.GetSingleton<Directory>();

        Entity bulletEntityPrefab = directory.bulletPrefab;
        EntityManager manager = state.EntityManager;

        // This code uses a CommandBuffer, which lets us queue up commands for playback later.
        // Note: CommandBuffers must be cleaned up to prevent memory leaks, so the "using" syntax
        // is used here to automatically manage that
        using (var commandBuffer = new EntityCommandBuffer(Allocator.TempJob))
        {
            foreach (var(spawnBulletRequest,entity) in
                     SystemAPI.Query<RefRO<SpawnBulletRequest>>()
                         .WithEntityAccess())
            {
                SpawnBullet(
                    ref manager,
                    bulletEntityPrefab,
                    spawnBulletRequest.ValueRO.gunBarrelPosition,
                    spawnBulletRequest.ValueRO.playerRotation);
                
                commandBuffer.DestroyEntity(entity);
            }
            
            // After the foreach, playback the buffer, destroying the entities
            commandBuffer.Playback(state.EntityManager);
        }
    }
    
    // This method spawns bullets as entities instead of GameObjects
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
}