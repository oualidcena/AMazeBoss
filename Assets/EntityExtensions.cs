﻿using System.Linq;
using Entitas;

namespace Assets
{
    public static class EntityExtensions
    {
        public static Entity AddId(this Entity entity)
        {
            var identifiables = Pools.game.GetEntities(GameMatcher.Id);
            var currentId = identifiables.Any() ? identifiables.Max(x => x.id.Value) : 0;
            entity.AddId(currentId+1);
            return entity;
        }

        public static Entity SetParent(this Entity child, Entity parent)
        {
            if(!parent.hasId)
            {
                parent.AddId();
            }
            child.AddChild(parent.id.Value);
            return child;
        }

        public static bool IsMoving(this Entity entity)
        {
            var target = entity.position.Value.ToV3();
            var current = entity.view.Value.transform.position;
            return entity.hasPosition && entity.hasView && target != current;
        }
    }
}