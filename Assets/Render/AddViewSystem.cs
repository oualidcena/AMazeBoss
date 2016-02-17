﻿using System.Collections.Generic;
using Entitas;
using UnityEngine;

namespace Assets.Render
{
    public class AddViewSystem : IReactiveSystem
    {
        private readonly Transform _viewsContainer = new GameObject("Views").transform;

        public TriggerOnEvent trigger { get { return GameMatcher.Resource.OnEntityAdded(); } }

        public void Execute(List<Entity> entities)
        {
            foreach (var entity in entities)
            {
                AddView(entity);
            }
        }

        private void AddView(Entity entity)
        {
            var resourceObject = Resources.Load<GameObject>(entity.resource.Path);
            if (resourceObject == null)
            {
                throw new MissingReferenceException("Resource " + entity.resource.Path + " not found.");
            }

            var view = GameObject.Instantiate(resourceObject);
            view.transform.SetParent(_viewsContainer);

            entity.AddView(view);
            GameObjectConfigurer.AttachEntity(view, entity);
        }
    }
}
