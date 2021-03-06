﻿using System;
using System.Collections.Generic;
using System.Linq;
using Entitas;
using DG.Tweening;
using UnityEngine;

namespace Assets.Render
{
    public abstract class AnimationSystem : IEnsureComponents
    {
        public virtual IMatcher ensureComponents { get { return GameMatcher.Animator; } }
    }

    public class RotationAnimationSystem : IReactiveSystem
    {
        public TriggerOnEvent trigger { get { return Matcher.AllOf(GameMatcher.View, GameMatcher.Rotation).OnEntityAdded(); } }

        public void Execute(List<Entity> entities)
        {
            foreach (var entity in entities)
            {
                var transform = entity.view.Value.transform;
                transform.rotation = Quaternion.LookRotation(LocalDirections.ToDirection(entity.rotation.Value).ToV3(), Vector3.up);
            }
        }
    }

    public class BumpIntoObjectAnimationSystem : AnimationSystem, IReactiveSystem
    {
        public TriggerOnEvent trigger { get { return GameMatcher.BumpedIntoObject.OnEntityAdded(); } }

        public void Execute(List<Entity> entities)
        {
            foreach (var entity in entities)
            {
                var animator = entity.animator.Value;
                entity.AddActingSequence(0.75f, () => animator.SetTrigger("BumpedIntoObject"));
            }
        }
    }

    public class EdgeRecoverAnimationSystem : AnimationSystem, IReactiveSystem
    {
        public TriggerOnEvent trigger { get { return GameMatcher.RecoveredAtEdge.OnEntityAdded(); } }

        public void Execute(List<Entity> entities)
        {
            foreach (var entity in entities)
            {
                var animator = entity.animator.Value;
                entity.AddActingSequence(0.75f, () => animator.SetTrigger("StoppedAtEdge"));
            }
        }
    }

    public class MoveAnimationSystem : IReactiveSystem, IEnsureComponents
    {
        public const float MoveTime = 0.5f;

        public TriggerOnEvent trigger { get { return Matcher.AllOf(GameMatcher.View, GameMatcher.Position).OnEntityAdded(); } }
        public IMatcher ensureComponents { get { return Matcher.AnyOf(GameMatcher.Hero, GameMatcher.Boss); } }

        public void Execute(List<Entity> entities)
        {
            foreach (var entity in entities)
            {
                PositionChanged(entity);
            }
        }

        private void PositionChanged(Entity entity)
        {
            var transform = entity.view.Value.transform;
            var newPosition = entity.position.Value.ToV3() + entity.viewOffset.Value;

            var animationAction = "IsMoving";
            var time = MoveTime;
            if (entity.isPushing)
            {
                animationAction = "IsPushing";
                time = 0.7f;
            }
            else if (entity.isPulling)
            {
                animationAction = "IsPulling";
                time = 0.9f;
            }

            var animator = entity.animator.Value;
            var sequence = DOTween.Sequence()
                .Pause()
                .OnStart(() => animator.SetBool(animationAction, true))
                .AppendInterval(time)
                .OnComplete(() =>
                {
                    transform.position = newPosition;
                    animator.SetBool(animationAction, false);
                });

            entity.AddActingSequence(time, sequence);
        }
    }

    public class ItemCarryAnimationSystem : AnimationSystem, IReactiveSystem
    {
        public TriggerOnEvent trigger { get { return GameMatcher.SpikesCarried.OnEntityAddedOrRemoved(); } }

        public void Execute(List<Entity> entities)
        {
            foreach (var entity in entities)
            {
                var animator = entity.animator.Value;
                var isSpikesCarried = entity.isSpikesCarried;
                entity.AddActingSequence(0.75f, () =>
                    {
                        animator.SetBool("Pickup", isSpikesCarried);
                        animator.SetTrigger("ItemInteract");
                    });
            }
        }
    }

    public class TrapLoadedAnimationSystem : AnimationSystem, IReactiveSystem
    {
        public TriggerOnEvent trigger { get { return GameMatcher.Loaded.OnEntityAddedOrRemoved(); } }

        public void Execute(List<Entity> entities)
        {
            foreach (var entity in entities)
            {
                entity.animator.Value.SetBool("Loaded", entity.hasLoaded);
            }
        }
    }

    public class TrapActivatedAnimationSystem : AnimationSystem, IReactiveSystem
    {
        private const float TrapActivateTime = 0.7f;

        public TriggerOnEvent trigger { get { return GameMatcher.TrapActivated.OnEntityAdded(); } }
        public override IMatcher ensureComponents { get { return Matcher.AllOf(GameMatcher.SpikeTrap, GameMatcher.Loaded, base.ensureComponents); } }

        public void Execute(List<Entity> entities)
        {
            foreach (var entity in entities)
            {
                var animator = entity.animator.Value;
                var animationSequence = DOTween.Sequence()
                    .AppendInterval(MoveAnimationSystem.MoveTime + TrapActivateTime / 2)
                    .OnComplete(() => animator.SetTrigger("Activated"));

                entity.AddActingSequence(TrapActivateTime, animationSequence);
            }
        }
    }

    public class ExitGateAnimationSystem : AnimationSystem, IReactiveSystem
    {
        public TriggerOnEvent trigger { get { return GameMatcher.ExitGate.OnEntityAdded(); } }

        public void Execute(List<Entity> entities)
        {
            foreach (var exitGate in entities)
            {
                var animator = exitGate.animator.Value;
                var isLocked = exitGate.exitGate.Locked;
                exitGate.AddActingSequence(0.75f, () => animator.SetBool("IsLocked", isLocked));
            }
        }
    }

    public class AttackAnimationSystem : AnimationSystem, IReactiveSystem, ISetPool
    {
        private Pool _pool;
        public TriggerOnEvent trigger { get { return Matcher.AllOf(GameMatcher.Boss, GameMatcher.Attacking).OnEntityAdded(); } }

        public void SetPool(Pool pool)
        {
            _pool = pool;
        }

        public void Execute(List<Entity> entities)
        {
            var camera = _pool.GetCamera();
            foreach (var character in entities)
            {
                var animator = character.animator.Value;
                character.AddActingSequence(1, DOTween.Sequence()
                    .OnStart(() => animator.SetTrigger("Attack"))
                    .AppendInterval(0.3f)
                    .AppendCallback(() => camera.transform.DOShakeRotation(0.6f, 3f, 20, 7)));
            }
        }
    }

    public class HealthChangedAnimationSystem : IReactiveSystem, ISetPool
    {
        private Pool _pool;
        public TriggerOnEvent trigger { get { return GameMatcher.Health.OnEntityAdded(); } }

        public void SetPool(Pool pool)
        {
            _pool = pool;
        }

        public void Execute(List<Entity> entities)
        {
            foreach (var entity in entities)
            {
                if (entity.hasHealthVisual)
                {
                    entity.healthVisual.Text.text = entity.health.Value.ToString();
                }

                if (entity.hasAnimator && !_pool.isLevelLoaded && !entity.isDead)
                {
                    var animator = entity.animator.Value;
                    if (!animator.GetBool("IsCursed"))
                    {
                        entity.AddActingSequence(1, () => animator.SetTrigger("Damage"));
                    }
                }
            }
        }
    }

    public class BoxMovedAnimationSystem : IInitializeSystem, ISetPool
    {
        // TODO: Why do I need to adjust height?
        private const float WeirdOffset = 0.15f;
        private readonly float _startHeight = 1 - Mathf.Sin(45*Mathf.Deg2Rad) - WeirdOffset;
        private Group _movedBoxGroup;
        private Group _rockedBoxGroup;
        private Pool _pool;

        public void SetPool(Pool pool)
        {
            _pool = pool;
            _movedBoxGroup = pool.GetGroup(Matcher.AllOf(GameMatcher.Box, GameMatcher.View, GameMatcher.Position));
            _rockedBoxGroup = pool.GetGroup(Matcher.AllOf(GameMatcher.Box, GameMatcher.View, GameMatcher.Knocked, GameMatcher.Rocked));
        }

        public void Initialize()
        {
            _movedBoxGroup.OnEntityUpdated +=
                (group, entity, index, oldComponent, newComponent) =>
                    DoRollAnimation(entity, oldComponent as PositionComponent, newComponent as PositionComponent);
            _rockedBoxGroup.OnEntityAdded += (group, entity, index, newComponent) => DoRockAnimation(entity);
        }

        private void DoRockAnimation(Entity entity)
        {
            var moveDirection = entity.knocked.FromDirection.ToV3();
            var transform = entity.view.Value.transform;

            const float time = 0.5f;
            var camera = _pool.GetCamera();
            var rotationDirection = Vector3.Cross(moveDirection.normalized, Vector3.up);
            // TODO: This looks like shit
            var sequence = DOTween.Sequence()
                .AppendInterval(entity.knocked.Wait)
                .Append(transform.DORotate(-rotationDirection / 8 * 90, time / 4, RotateMode.WorldAxisAdd))
                .Join(transform.DOMove(moveDirection / 8, time / 4).SetRelative())
                .Join(camera.transform.DOShakeRotation(0.2f, 0.7f, 20, 3))
                .Append(transform.DORotate(rotationDirection / 8 * 90, time / 4, RotateMode.WorldAxisAdd))
                .Join(transform.DOMove(-moveDirection / 8, time / 4).SetRelative())
                .Append(transform.DORotate(rotationDirection / 16 * 90, time / 4, RotateMode.WorldAxisAdd))
                .Join(transform.DOMove(-moveDirection / 16, time / 4).SetRelative())
                .Append(transform.DORotate(-rotationDirection / 16 * 90, time / 4, RotateMode.WorldAxisAdd))
                .Join(transform.DOMove(moveDirection / 16, time / 4).SetRelative())
                .OnUpdate(() => UpdateVerticalMove(transform));

            if (!entity.knocked.Immediate)
            {
                entity.AddActingSequence(MoveAnimationSystem.MoveTime);
                entity.AddActingSequence(time, sequence);
            }
        }

        private void DoRollAnimation(Entity entity, PositionComponent oldPosition, PositionComponent newPosition)
        {
            var moveDirection = (newPosition.Value - oldPosition.Value).ToV3();
            var transform = entity.view.Value.transform;

            const float time = 0.5f;
            var camera = _pool.GetCamera();
            var rotationDirection = Vector3.Cross(moveDirection.normalized, Vector3.up);
            var sequence = DOTween.Sequence()
                .AppendInterval(entity.knocked.Wait)
                .Append(transform.DORotate(-rotationDirection * 90, time, RotateMode.WorldAxisAdd))
                .Join(transform.DOMove(moveDirection, time)
                    .SetRelative(true))
                .Join(camera.transform.DOShakeRotation(0.3f, 1, 20, 3))
                .OnUpdate(() => UpdateVerticalMove(transform));

            if (!entity.knocked.Immediate)
            {
                entity.AddActingSequence(MoveAnimationSystem.MoveTime);
                entity.AddActingSequence(time, sequence);
            }
        }

        private void UpdateVerticalMove(Transform transform)
        {
            var angles = transform.rotation.eulerAngles;
            var position = transform.position;
            transform.position = new Vector3(position.x, Mathf.Sin((angles.x % 90 + 45) * Mathf.Deg2Rad) + _startHeight, position.z);
        }
    }

    public class DeathAnimationSystem : AnimationSystem, IReactiveSystem
    {
        private const float DeathTime = 3;

        public TriggerOnEvent trigger { get { return GameMatcher.Dead.OnEntityAdded(); } }

        public void Execute(List<Entity> entities)
        {
            foreach (var dead in entities)
            {
                var animator = dead.animator.Value;
                dead.AddActingSequence(DeathTime, () => animator.SetTrigger("Killed"));
            }
        }
    }

    public class CurseAnimationSystem : AnimationSystem, IReactiveSystem
    {
        private const float CurseAnimationTime = 1.1f;

        public TriggerOnEvent trigger { get { return GameMatcher.Cursed.OnEntityAddedOrRemoved(); } }

        public void Execute(List<Entity> entities)
        {
            foreach (var cursed in entities.Where(x => x.health.Value > 0))
            {
                var animator = cursed.animator.Value;
                var isCursed = cursed.isCursed;
                cursed.AddActingSequence(CurseAnimationTime, () => animator.SetBool("IsCursed", isCursed));
            }
        }
    }
}