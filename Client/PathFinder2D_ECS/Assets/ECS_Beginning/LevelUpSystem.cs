using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class LevelUpSystem : ComponentSystem
{
    protected override void OnUpdate()
    {
        Entities.ForEach((ref LevelConmponent levelComponent) =>
        {
            levelComponent.level += 1f * Time.DeltaTime;
        });
    }
}
