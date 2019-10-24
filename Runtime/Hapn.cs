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
    public abstract class BaseState<S, T> : EntranceTypes<T>, WithTokenStateConstruction<T> where S : BaseState<S, T> {
        public List<ITransition> transitions { get; } = new List<ITransition>();
        public List<Action> entryActions { get; } = new List<Action>();
        public List<Action> everyFrame { get; } = new List<Action>();
        public List<Action> exitActions { get; } = new List<Action>();
        public List<Action> negativeEntryActions { get; } = new List<Action>();
        public List<Action> negativeEveryFrameActions { get; } = new List<Action>();
        public List<Action> negativeExitActions { get; } = new List<Action>();
        public float entryTime { get; set; } = 0f;
        public Graph Graph { get; set; }
        public string Name { get; }

        public T token { get; set; }

        public BaseState (Graph graph, string name) {
            this.Graph = graph;
            this.Name = name;
        }

        protected abstract S GetMe();

        public void AddTransition(ITransition t) {
            transitions.Add(t);
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
    public abstract class BaseState<S> : EntranceTypes where S : BaseState<S> {
        public List<ITransition> transitions { get; } = new List<ITransition>();
        public List<Action> entryActions { get; } = new List<Action>();
        public List<Action> everyFrame { get; } = new List<Action>();
        public List<Action> exitActions { get; } = new List<Action>();
        public List<Action> negativeEntryActions { get; } = new List<Action>();
        public List<Action> negativeEveryFrameActions { get; } = new List<Action>();
        public List<Action> negativeExitActions { get; } = new List<Action>();
        public float entryTime { get; set; } = 0f;
        public string Name { get; }

        public BaseState(string name) {
            this.Name = name;
        }

        protected abstract S GetMe();

        // This is only a setter method. Entrance actions will be invoked by the Graph, possibly by calling a different method on this.
        public void Enter() {
        }
        // public void Exit() {
        // }

        public CheckedTransition BuildTransition() {
            CheckedTransition t = new CheckedTransition();
            transitions.Add(t);
            return t;
        }

        public void AddTransition(ITransition t) {
            transitions.Add(t);
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

    }

    // This is for use by the Graph at runtime
    public interface IState {
        List<ITransition> transitions { get; }
        float entryTime { get; set; }
        string Name { get; }

        void RunEntryActions();
        void RunEveryFrameActions();
        void RunExitActions();

        // The Negative of a state is a meta-state that is active if and only if the state is inactive. Represents a State Group of 'every other state'
        void RunNegativeEntryActions();
        void RunNegativeEveryFrameActions();
        void RunNegativeExitActions();
    }

    // Used by all the building mechanisms. Abstracts over states that can be built on/around
    public interface IStateConstruction {
        // Entry Actions occur before outgoing transitions are enabled and checked.
        void AddEntryAction(Action action);
        // Exit actions occur after outgoing transitions have been disabled. EveryFrameActions are guarenteed not to occur afterwards, until state is entered again.
        void AddExitAction(Action action);
        void AddEveryFrameAction(Action action);
        void AddNegativeEntryAction(Action action);
        // Exit actions occur after outgoing transitions have been disabled. EveryFrameActions are guarenteed not to occur afterwards, until state is entered again.
        void AddNegativeExitAction(Action action);
        void AddNegativeEveryFrameAction(Action action);
        void AddTransition(ITransition t);
        Graph Graph { get; }

        float entryTime { get; }
        IState ToRuntimeState();
    }
    public interface NoTokenStateConstruction : IStateConstruction {
        EntranceTypes ToEntranceType();
    }
    // Construction regarding a state with a token of type T
    public interface WithTokenStateConstruction<T> : IStateConstruction {
        EntranceTypes<T> ToEntranceType();
    }

    public struct InvertedStateConstruction : IStateConstruction {
        private IStateConstruction m_baseState;
        public InvertedStateConstruction(IStateConstruction baseState) {
            m_baseState = baseState;
        }

        public Graph Graph => m_baseState.Graph;

        public float entryTime => m_baseState.entryTime;

        public void AddEntryAction(Action action) => m_baseState.AddNegativeEntryAction(action);

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

        public void AddTransition(ITransition t) {
            m_baseState.AddTransition(t);
        }

        public IState ToRuntimeState() {
            return m_baseState.ToRuntimeState();
        }
    }

    public class Graph {
        private static int anonCounter = 0;
        public IState m_currentState = null;
        private bool m_hasStartedInitState = false;
        private HashSet<IState> m_stateSet = new HashSet<IState>();
        private string m_name;
        private bool m_logDebug;

        public Graph(string name = null, bool logDebug = false) {
            m_name = name ?? anonCounter++.ToString();
            m_logDebug = logDebug;
        }
        public void SetInitState(IState s) {
            m_currentState = s;
        }

        public void AddState(IState s) {
            m_stateSet.Add(s);
        }

        public void Update()
        {
            if (m_currentState == null) {
                Debug.LogWarning("Graph: Update called, but no initial state has been set.");
                return;
            }
            if (!m_hasStartedInitState) {
                m_hasStartedInitState = true;
                if (m_logDebug) Debug.LogFormat("Graph [{0}] starting.", m_name);
                foreach (IState i in m_stateSet) {
                    if (i == m_currentState) continue;
                    i.RunNegativeEntryActions();
                }
                m_currentState.RunEntryActions();
                foreach (ITransition t in m_currentState.transitions) {
                    t.Enable();
                }
            }
            bool m_keepCheckingTransitions = true;
            while (m_keepCheckingTransitions) {
                m_keepCheckingTransitions = false;
                foreach (ITransition t in m_currentState.transitions) {
                    if (t.CheckAndPassData()) {
                        DoTransitionWork(t.GetDestination());
                        m_keepCheckingTransitions = true;
                        break;
                    }
                }
            }
            // Run EveryFrameActions for the current state and the NegativeStates of every other state
            m_currentState.RunEveryFrameActions();
            foreach (IState i in m_stateSet) {
                if (i == m_currentState) continue;
                i.RunNegativeEveryFrameActions();
            }
            
        }

        // Transition should already have inserted any tokens into the
        // destination state before calling this.
        public void TriggerManualTransition(IState destination) {
            DoTransitionWork(destination);
        }
        private void DoTransitionWork(IState destination) {
            if (m_logDebug) Debug.LogFormat("Graph [{0}]: {1} -> {2}", m_name, m_currentState.Name, destination.Name);
            var outgoingState = m_currentState;
            foreach (ITransition u in m_currentState.transitions) {
                u.Disable();
            }
            m_currentState.RunExitActions();
            // Run exit actions for the negativeState of the incoming state: we're exiting out of the meta-state represented by not(destination)
            destination.RunNegativeExitActions();

            //////
            m_currentState = destination;
            if (m_currentState == null) throw new Exception("Somebody messed up the transitions!");
            //////

            m_currentState.entryTime = Time.time;
            // Run entry actions of the negativeState for the outgoing state
            outgoingState.RunNegativeEntryActions();
            m_currentState.RunEntryActions();
            foreach (ITransition u in m_currentState.transitions) {
                u.Enable();
            }

        }
    }

    public class StateGroup {
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

        public static (NoTokenStateConstruction state, ITransitionBuilder transition) StateFromEaseIn(this Graph graph, float duration, Func<(float, float)> initValues, Action<float> output) {
            var curve = AnimationCurve.EaseInOut(0f, 0f, duration, 1f);
            return graph.StateFromAnimationCurve(duration, AdditionalHelpers.LerpFloat, initValues, output, curve);
        }

        public static (NoTokenStateConstruction state, ITransitionBuilder transition) StateFromHapnTween(this Graph graph, HapnTweenAdapter m_tween) {
            return graph.StateFromAnimationCurve(m_tween.duration, AdditionalHelpers.LerpVec2, () => (m_tween.startPos, m_tween.endPos), (val) => m_tween.toChange?.Invoke(val), m_tween.curve);
        }

        public static (NoTokenStateConstruction state, ITransitionBuilder transition) StateFromAnimationCurve<T>(this Graph graph, float duration, Func<T, T, float, T> lerp, Func<(T, T)> initValues, Action<T> output, AnimationCurve curve) {
            var s = graph.NewState();
            var t = new MultiTransition(graph);
            var isAnimationDone = s.BindAnimation(duration, lerp, initValues, output, curve);
            t.When(isAnimationDone);
            s.AddTransition(t);
            return (s, t);
        }

        public static (NoTokenStateConstruction off, NoTokenStateConstruction on) BooleanStates(this Graph graph, Button show, Button hide) {
            var off = graph.NewState();
            var on = graph.NewState();
            // Transitions
            TransitionHelpers.MakeTransition(off, on, show);
            TransitionHelpers.MakeTransition(on, off, hide);

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