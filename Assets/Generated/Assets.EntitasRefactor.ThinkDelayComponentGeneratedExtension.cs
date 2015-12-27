using System.Collections.Generic;
using Assets;

namespace Entitas {
    public partial class Entity {
        public ThinkDelayComponent thinkDelay { get { return (ThinkDelayComponent)GetComponent(ComponentIds.ThinkDelay); } }

        public bool hasThinkDelay { get { return HasComponent(ComponentIds.ThinkDelay); } }

        static readonly Stack<ThinkDelayComponent> _thinkDelayComponentPool = new Stack<ThinkDelayComponent>();

        public static void ClearThinkDelayComponentPool() {
            _thinkDelayComponentPool.Clear();
        }

        public Entity AddThinkDelay(float newTimeLeft) {
            var component = _thinkDelayComponentPool.Count > 0 ? _thinkDelayComponentPool.Pop() : new ThinkDelayComponent();
            component.TimeLeft = newTimeLeft;
            return AddComponent(ComponentIds.ThinkDelay, component);
        }

        public Entity ReplaceThinkDelay(float newTimeLeft) {
            var previousComponent = hasThinkDelay ? thinkDelay : null;
            var component = _thinkDelayComponentPool.Count > 0 ? _thinkDelayComponentPool.Pop() : new ThinkDelayComponent();
            component.TimeLeft = newTimeLeft;
            ReplaceComponent(ComponentIds.ThinkDelay, component);
            if (previousComponent != null) {
                _thinkDelayComponentPool.Push(previousComponent);
            }
            return this;
        }

        public Entity RemoveThinkDelay() {
            var component = thinkDelay;
            RemoveComponent(ComponentIds.ThinkDelay);
            _thinkDelayComponentPool.Push(component);
            return this;
        }
    }

    public partial class Matcher {
        static IMatcher _matcherThinkDelay;

        public static IMatcher ThinkDelay {
            get {
                if (_matcherThinkDelay == null) {
                    var matcher = (Matcher)Matcher.AllOf(ComponentIds.ThinkDelay);
                    matcher.componentNames = ComponentIds.componentNames;
                    _matcherThinkDelay = matcher;
                }

                return _matcherThinkDelay;
            }
        }
    }
}
