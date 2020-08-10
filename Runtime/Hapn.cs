using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;


namespace Hapn
{
    // To declare a state may be entered without any tokens/data
    public interface EntranceTypes : IState { }

    public interface EntranceTypes<T> : IState {
        T token { get; }
        void Enter(T var1);
    }

    // Inherit from this for custom state classes.
    public abstract class BaseState<T> : EntranceTypes<T>, WithTokenStateConstruction<T> {
        public List<ITransition> transitions { get; } = new List<ITransition>();
        public List<Action> entryActions { get; } = new List<Action>();
        public List<Action> everyFrame { get; } = new List<Action>();
        public List<Action> exitActions { get; } = new List<Action>();
        public List<Action> negativeEntryActions { get; } = new List<Action>();
        public List<Action> negativeEveryFrameActions { get; } = new List<Action>();
        public List<Action> negativeExitActions { get; } = new List<Action>();
        public List<StateGroup> groups { get; set; }  = new List<StateGroup>();
        public float entryTime { get; set; } = 0f;
        public float exitTime { get; set; } = 0f;
        public Graph Graph { get; set; }
        public string Name { get; }

        public T token { get; set; }

        public BaseState (Graph graph, string name) {
            this.Graph = graph;
            this.Name = name;
        }

        public void AddTransition(ITransition t) {
            transitions.Add(t);
        }
        public void AddGroup(StateGroup sg) {
            groups.Add(sg);
        }

        public void AddEntryAction(Action action) {
            entryActions.Add(action);
        }
        public void AddExitAction(Action action) {
            exitActions.Add(action);
        }
        public void AddEveryFrameAction(Action action) {
            everyFrame.Add(action);
        }
        public EntranceTypes<T> ToEntranceType() {
            return this;
        }

        // This is only a setter method. Entrance actions will be invoked by the Graph, possibly by calling a different method on this.
        public void Enter(T token) {
            this.token = token;
        }
        public void Exit() {
            token = default(T);
        }

        public void RunEveryFrameActions()
        {
            foreach (var a in everyFrame)
            {
                a();
            }
        }
        public void RunEntryActions() {
            foreach (var a in entryActions) {
                a();
            }
        }
        public void RunExitActions() {
            foreach (var a in exitActions) {
                a();
            }
        }

        public void AddNegativeEntryAction(Action action) {
            negativeEntryActions.Add(action);
        }
        public void AddNegativeExitAction(Action action) {
            negativeExitActions.Add(action);
        }
        public void AddNegativeEveryFrameAction(Action action) {
            negativeEveryFrameActions.Add(action);
        }

        public void RunNegativeEntryActions() {
            foreach (var a in negativeEntryActions) {
                a();
            }
        }

        public void RunNegativeEveryFrameActions() {
            foreach (var a in negativeEveryFrameActions) {
                a();
            }
        }

        public void RunNegativeExitActions() {
            foreach (var a in negativeExitActions) {
                a();
            }
        }

        public IState ToRuntimeState() {
            return this;
        }
    }

    // Inherit from this for custom state classes.
    public abstract class BaseState : EntranceTypes, NoTokenStateConstruction {
        public List<ITransition> transitions { get; } = new List<ITransition>();
        public List<Action> entryActions { get; } = new List<Action>();
        public List<Action> everyFrame { get; } = new List<Action>();
        public List<Action> exitActions { get; } = new List<Action>();
        public List<Action> negativeEntryActions { get; } = new List<Action>();
        public List<Action> negativeEveryFrameActions { get; } = new List<Action>();
        public List<Action> negativeExitActions { get; } = new List<Action>();
        public List<StateGroup> groups { get; set; }  = new List<StateGroup>();
        public Graph Graph { get; set; }
        public float entryTime { get; set; } = 0f;
        public float exitTime { get; set; } = 0f;
        public string Name { get; }

        public BaseState(string name) {
            this.Name = name;
        }

        // This is only a setter method. Entrance actions will be invoked by the Graph, possibly by calling a different method on this.
        public void Enter() {
        }
        // public void Exit() {
        // }

        public void AddTransition(ITransition t) {
            transitions.Add(t);
        }
        public void AddGroup(StateGroup sg) {
            groups.Add(sg);
        }

        public void RunEveryFrameActions() {
            this.EveryFrame();
            foreach (var a in everyFrame)
            {
                a();
            }
        }
        public void RunEntryActions() {
            this.OnEntry();
            foreach (var a in entryActions) {
                a();
            }
        }
        public void RunExitActions() {
            this.OnExit();
            foreach (var a in exitActions) {
                a();
            }
        }

        public void AddEveryFrameAction(Action action) {
            everyFrame.Add(action);
        }

        public void AddEntryAction(Action action) {
            entryActions.Add(action);
        }
        public void AddExitAction(Action action) {
            exitActions.Add(action);
        }

        // Convenient Methods for subclasses to override in simple cases. For more control, subclasses
        // may override the Run*Actions() methods
        protected virtual void EveryFrame() {
        }
        protected virtual void OnEntry() {
        }
        protected virtual void OnExit() {
        }

        public void AddNegativeEntryAction(Action action) {
            negativeEntryActions.Add(action);
        }
        public void AddNegativeExitAction(Action action) {
            negativeExitActions.Add(action);
        }
        public void AddNegativeEveryFrameAction(Action action) {
            negativeEveryFrameActions.Add(action);
        }

        public void RunNegativeEntryActions() {
            foreach (var a in negativeEntryActions) {
                a();
            }
        }

        public void RunNegativeEveryFrameActions() {
            foreach (var a in negativeEveryFrameActions) {
                a();
            }
        }

        public void RunNegativeExitActions() {
            foreach (var a in negativeExitActions) {
                a();
            }
        }

        public EntranceTypes ToEntranceType() {
            return this;
        }

        public IState ToRuntimeState() {
            return this;
        }
    }

    // This is for use by the Graph at runtime
    public interface IState {
        List<ITransition> transitions { get; }
        // Perhaps these should also be functions, e.g. void RunGroupEntryActions().
        // May need to change later
        List<StateGroup> groups { get; }
        float entryTime { get; set; }
        float exitTime { get; set; }
        string Name { get; }

        // These are functions instead of getters to allow the Actions to be typed to the tokens
        // of the state (not currently the case)
        void RunEntryActions();
        void RunEveryFrameActions();
        void RunExitActions();

        // The Negative of a state is a meta-state that is active if and only if the state is inactive. Represents a State Group of 'every other state'
        void RunNegativeEntryActions();
        void RunNegativeEveryFrameActions();
        void RunNegativeExitActions();
    }

    // Common concepts between states and state-groups. Might need a better name.
    public interface IEnterable {
        // Entry Actions occur before outgoing transitions are enabled and checked.
        void AddEntryAction(Action action);
        // Exit actions occur after outgoing transitions have been disabled. EveryFrameActions are guarenteed not to occur afterwards, until state is entered again.
        void AddExitAction(Action action);
        void AddEveryFrameAction(Action action);
        void AddNegativeEntryAction(Action action);
        // Exit actions occur after outgoing transitions have been disabled. EveryFrameActions are guarenteed not to occur afterwards, until state is entered again.
        void AddNegativeExitAction(Action action);
        void AddNegativeEveryFrameAction(Action action);
        float entryTime { get; }
        float exitTime { get; }
    }

    // Used by all the building mechanisms. Abstracts over states that can be built on/around
    public interface IStateConstruction : IEnterable {
        void AddTransition(ITransition t);
        void AddGroup(StateGroup sg);
        Graph Graph { get; }

        IState ToRuntimeState();
    }
    public interface NoTokenStateConstruction : IStateConstruction {
        EntranceTypes ToEntranceType();
    }
    // Construction regarding a state with a token of type T
    public interface WithTokenStateConstruction<T> : IStateConstruction {
        EntranceTypes<T> ToEntranceType();
    }

    public class InvertedStateConstruction : IEnterable {
        private IEnterable m_baseState;
        public InvertedStateConstruction(IEnterable baseState) {
            m_baseState = baseState;
        }

        public float entryTime => m_baseState.exitTime;
        public float exitTime => m_baseState.entryTime;

        public void AddEntryAction(Action action) {
            m_baseState.AddNegativeEntryAction(action);
        }

        public void AddEveryFrameAction(Action action) {
            m_baseState.AddNegativeEveryFrameAction(action);
        }

        public void AddExitAction(Action action) {
            m_baseState.AddNegativeExitAction(action);
        }

        public void AddNegativeEntryAction(Action action) {
            m_baseState.AddEntryAction(action);
        }

        public void AddNegativeEveryFrameAction(Action action) {
            m_baseState.AddEveryFrameAction(action);
        }

        public void AddNegativeExitAction(Action action) {
            m_baseState.AddExitAction(action);
        }
    }

    // UTILITIES
    [Serializable]
    public class ColourChangeEvent : UnityEvent<Color>{ }

    [Serializable]
    public class ColourSlider {
        public AnimationCurve m_colourShift;
        public Color m_baseColour;
        public Color m_highlightColour;
        [NonSerialized]
        float m_colourPos = 0f;
        public ColourChangeEvent target;

        // dir = -1/1
        public void AdjustColour (float dir) {
            if (m_colourShift == null || m_colourShift.length < 2) {
                Debug.LogError("Colour Slider not initialized with Animation Curve");
                return;
            }
            m_colourPos = Mathf.Clamp(m_colourPos + Time.deltaTime*dir, 0f, m_colourShift.keys[1].time);
            target.Invoke(Color.Lerp(m_baseColour, m_highlightColour, m_colourShift.Evaluate(m_colourPos)));
        }
    }

    public static class ConvenienceExtensions {
        public static float GetTimeSinceEntry(this IState s) {
            return Time.time - s.entryTime;
        }
    }

    public static class AdditionalHelpers {
        public static float LerpFloat(float a, float b, float x) {
            return Mathf.LerpUnclamped(a, b, x);
        }

        public static Vector2 LerpVec2(Vector2 a, Vector2 b, float x) {
            return Vector2.LerpUnclamped(a, b, x);
        }

        public static Rect LerpRect(Rect a, Rect b, float x) {
            var minLerp = Vector2.LerpUnclamped(a.min, b.min, x);
            var maxLerp = Vector2.LerpUnclamped(a.max, b.max, x);
            return Rect.MinMaxRect(minLerp.x, minLerp.y, maxLerp.x, maxLerp.y);
        }

        // Adjust the evaluate value so that the curve key times are remapped to span (0f, duration). evalAt is not clamped to duration.
        public static float EvalCurveNormalized(AnimationCurve curve, float duration, float evalAt) {
            float curveStart = curve.keys[0].time;
            float curveEnd = curve.keys[curve.length-1].time;
            return curve.Evaluate(curveStart + (evalAt / duration * (curveEnd - curveStart)));

        }
    }

    // Premade graph patterns
    public static class GraphBuilders {
        private static int anonInt = 0;
        public static NoTokenStateConstruction NewState(this Graph graph, string name = null) {
            ComposedState state = new ComposedState(graph, name ?? anonInt++.ToString());
            graph.AddState(state);
            return state;
        }

        public static WithTokenStateConstruction<T> NewState<T>(this Graph graph, string name = null) {
            ComposedState<T> state = new ComposedState<T>(graph, name ?? anonInt++.ToString());
            graph.AddState(state);
            return state;
        }

        public static StateGroup NewStateGroup(this Graph graph, string name) {
            var sg = new StateGroup(name);
            graph.AddGroup(sg);
            return sg;
        }


        public static (NoTokenStateConstruction state, ITransitionBuilder transition) StateFromEaseIn(this Graph graph, float duration, Func<(float, float)> initValues, Action<float> output) {
            var curve = AnimationCurve.EaseInOut(0f, 0f, duration, 1f);
            return graph.StateFromAnimationCurve(duration, AdditionalHelpers.LerpFloat, initValues, output, curve);
        }

        public static (NoTokenStateConstruction state, ITransitionBuilder transition) StateFromAnimationCurve(this Graph graph, string name, float duration, Func<(float, float)> initValues, Action<float> output, AnimationCurve curve) {
            return graph.StateFromAnimationCurve(duration, AdditionalHelpers.LerpFloat, initValues, output, curve, name);
        }

        public static (NoTokenStateConstruction state, ITransitionBuilder transition) StateFromHapnTween(this Graph graph, HapnTweenAdapter m_tween) {
            return graph.StateFromAnimationCurve(m_tween.duration, AdditionalHelpers.LerpVec2, () => (m_tween.startPos, m_tween.endPos), (val) => m_tween.toChange?.Invoke(val), m_tween.curve);
        }

        public static (NoTokenStateConstruction state, ITransitionBuilder transition) StateFromAnimationCurve<T>(this Graph graph, float duration, Func<T, T, float, T> lerp, Func<(T, T)> initValues, Action<T> output, AnimationCurve curve, string name = null) {
            var s = graph.NewState(name);
            var t = s.EntryAnimationAndTransitionOnDone(duration, lerp, initValues, output, curve);
            return (s, t);
        }

        public static ITransitionBuilder EntryAnimationAndTransitionOnDone<T>(this IStateConstruction s, float duration, Func<T, T, float, T> lerp, Func<(T, T)> initValues, Action<T> output, AnimationCurve curve) {
            var t = new MultiTransition(s.Graph, s.ToRuntimeState());
            var isAnimationDone = s.EntryAnimation(duration, lerp, initValues, output, curve);
            t.When(isAnimationDone);
            s.AddTransition(t);
            return t;
        }

        public static (NoTokenStateConstruction off, NoTokenStateConstruction on) BooleanStates(this Graph graph, Button show, Button hide) {
            var off = graph.NewState();
            var on = graph.NewState();
            // Transitions
            off.MakeTransition(on, show);
            on.MakeTransition(off, hide);

            return (off, on);
        }
    }

    public static class UnityEngineExtensions {
        public static Vector2 Overwrite(this Vector2 v, float? x = null, float? y = null) {
            return new Vector2(x ?? v.x, y ?? v.y);
        }

        public static Rect AnchorsAsRect(this RectTransform rt) {
            return Rect.MinMaxRect(rt.anchorMin.x, rt.anchorMin.y, rt.anchorMax.x, rt.anchorMax.y);
        }

        public static void SetAnchoredPosX(this RectTransform rt, float x) {
            rt.anchoredPosition = rt.anchoredPosition.Overwrite(x: x);
        }

        public static void SetAnchoredPosY(this RectTransform rt, float y) {
            rt.anchoredPosition = rt.anchoredPosition.Overwrite(y: y);
        }
    }


}