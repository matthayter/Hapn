using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using UniRx.Async;
using Hapn.UniRx;
using Hapn;

namespace Hapn
{
    // Relies on UniRx.Async for now
    public class FluentBuilder {

        private Graph m_graph;
        private IStateConstruction m_stateInContext;
        private Dictionary<string, IStateConstruction> m_states = new Dictionary<string, IStateConstruction>();
        private Dictionary<string, StateGroup> m_stateGroups = new Dictionary<string, StateGroup>();
        // Transitions leading to a state that has not yet been declared.
        private Dictionary<string, List<IBaseTransitionBuilder>> m_waitingTransitions = new Dictionary<string, List<IBaseTransitionBuilder>>();
        private Dictionary<string, List<StateGroup>> m_waitingStateGroups = new Dictionary<string, List<StateGroup>>();
        private List<IBaseTransitionBuilder> m_transitionsToNextState = new List<IBaseTransitionBuilder>(5);

        public FluentBuilder(Graph g) {
            m_graph = g;
            m_stateInContext = null;
        }

        // Declare a new StateGroup, independent of the state in context.
        public FluentBuilderStateGroup StateGroup(string name, params string[] states) {
            if (!m_stateGroups.ContainsKey(name)) {
                m_stateGroups[name] = m_graph.NewStateGroup(name);
            }
            StateGroup sg = m_stateGroups[name];
            foreach (string stateName in states) {
                if (m_states.ContainsKey(stateName)) {
                    m_states[stateName].AddGroup(sg);
                } else {
                    if (m_waitingStateGroups.ContainsKey(stateName)) {
                        m_waitingStateGroups[stateName].Add(sg);
                    } else {
                        m_waitingStateGroups[stateName] = new List<StateGroup>(5) { sg };
                    }
                }
            }
            var builder = new FluentBuilderStateGroup(sg, this);
            return builder;
        }

        // Add a stateGroup to the state in context.
        public FluentBuilder InStateGroups(params string[] stateGroupNames) {
            foreach (string sgName in stateGroupNames) {
                if (!m_stateGroups.ContainsKey(sgName)) {
                    m_stateGroups[sgName] = m_graph.NewStateGroup(sgName);
                }
                m_stateInContext.AddGroup(m_stateGroups[sgName]);
            }
            return this;
        }

        public FluentBuilder State(string name) {
            NoTokenStateConstruction newState = m_graph.NewState(name);
            m_stateInContext = newState;
            if (m_states.ContainsKey(name)) {
                Debug.LogError("Hapn FluentBuilder: multiple states declared with same name.");
            }
            m_states[name] = newState;
            if (m_waitingTransitions.ContainsKey(name)) {
                foreach (IBaseTransitionBuilder t in m_waitingTransitions[name]) {
                    // Transitions with- and without-tokens can transition to no-token states.
                    t.To(newState);
                }
                m_waitingTransitions.Remove(name);
            }

            LinkWaitingThingsToNewState(name, newState);
            AssignTransitionsAndClear(newState);

            return this;
        }

        public FluentBuilder State<T>(string name) {
            WithTokenStateConstruction<T> newState = m_graph.NewState<T>(name);
            m_stateInContext = newState;
            if (m_states.ContainsKey(name)) {
                Debug.LogError("Hapn Fluent Builder: multiple states declared with same name.");
            }
            m_states[name] = newState;
            if (m_waitingTransitions.ContainsKey(name)) {
                foreach (IBaseTransitionBuilder t in m_waitingTransitions[name]) {
                    if (t is ITransitionBuilder<T> typedT) {
                        typedT.To(newState);
                    } else {
                        Debug.LogErrorFormat("FluentBuilder: State '{0}' that requires token created, but transitions-without-tokens were awaiting a state with this name.", name);
                    }
                }
                m_waitingTransitions.Remove(name);
            }

            LinkWaitingThingsToNewState(name, newState);

            foreach (var t in m_transitionsToNextState) {
                var castTransition = t as ITransitionBuilder<T>;
                if (t == null) throw new InvalidOperationException("A transition was waiting to be connected to a state with a different token type (or no token) compared to the new state");
                castTransition.To(newState);
            }
            m_transitionsToNextState.Clear();

            return this;
        }

        private void LinkWaitingThingsToNewState(string name, IStateConstruction state) {
            if (m_waitingStateGroups.ContainsKey(name)) {
                foreach (var sg in m_waitingStateGroups[name]) {
                    // Transitions with- and without-tokens can transition to no-token states.
                    state.AddGroup(sg);
                }
                m_waitingTransitions.Remove(name);
            }

        }

        private void AssignTransitionsAndClear(NoTokenStateConstruction newState) {
            foreach (var t in m_transitionsToNextState) {
                t.To(newState);
            }
            m_transitionsToNextState.Clear();
        }

        public FluentBuilder BindLinearAnimationAndTransitionOnDone(float duration, Func<(float, float)> initValues, Action<float> output) {
            var t = m_stateInContext.BindAnimationAndTransitionOnDone(duration, AdditionalHelpers.LerpFloat, initValues, output, AnimationCurve.Linear(0f, 0f, 1f, 1f));
            m_transitionsToNextState.Add(t);
            return this;
        }
        public FluentBuilder BindLinearAnimationAndTransitionOnDone(float duration, Func<(Vector3, Vector3)> initValues, Action<Vector3> output) {
            var t = m_stateInContext.BindAnimationAndTransitionOnDone(duration, Vector3.LerpUnclamped, initValues, output, AnimationCurve.Linear(0f, 0f, 1f, 1f));
            m_transitionsToNextState.Add(t);
            return this;
        }

        public FluentBuilder BindLinearAnimation(float duration, Func<(float, float)> initValues, Action<float> output) {
            m_stateInContext.BindAnimation(duration, AdditionalHelpers.LerpFloat, initValues, output, AnimationCurve.Linear(0f, 0f, 1f, 1f));
            return this;
        }
        public FluentBuilder BindLinearAnimation(float duration, Func<(Vector3, Vector3)> initValues, Action<Vector3> output) {
            m_stateInContext.BindAnimation(duration, Vector3.LerpUnclamped, initValues, output, AnimationCurve.Linear(0f, 0f, 1f, 1f));
            return this;
        }

        public FluentBuilder StateFromAnimationCurve(string name, float duration, Func<(float, float)> initValues, Action<float> output, AnimationCurve curve) {
            var res = m_graph.StateFromAnimationCurve(name, duration, initValues, output, curve);
            m_stateInContext = res.state;

            LinkWaitingThingsToNewState(name, res.state);
            AssignTransitionsAndClear(res.state);

            m_transitionsToNextState.Add(res.transition);

            return this;
        }

        public FluentBuilder Init() {
            m_graph.SetInitState(m_stateInContext.ToRuntimeState());
            return this;
        }

        public FluentBuilder GetState(Action<IStateConstruction> handler) {
            handler(m_stateInContext);
            return this;
        }

        public FluentBuilder CheckedTransition(Func<bool> when) {
            var t = m_stateInContext.MakeDanglingTransition(when);
            m_transitionsToNextState.Add(t);
            return this;
        }

        public FluentBuilder ManualTransition(Action<Action> assignTrigger) {
            if (m_stateInContext == null) {
                throw new InvalidOperationException("Create a state before making a transition.");
            }
            var (transition, trigger) = m_stateInContext.MakeManualDanglingTransition();
            assignTrigger(trigger);
            m_transitionsToNextState.Add(transition);

            return this;
        }
        public FluentBuilder ManualTransition<T>(Action<Action<T>> assignTrigger) {
            if (m_stateInContext == null) {
                throw new InvalidOperationException("Create a state before making a transition.");
            }
            var (transition, trigger) = m_stateInContext.MakeManualDanglingTransition<T>();
            assignTrigger(trigger);
            m_transitionsToNextState.Add(transition);

            return this;
        }

        // f returns a promise for a state-name, which must be one of the names given in `destinations`.
        // One of the design goals of Hapn is for the state-graph to be immutable at 'run-time', which is why the destinations
        // must be specified at 'build-time'
        //public FluentBuilder TransitionBasedOnResult(Func<UniTask<string>> f, params string[] destinations) {
        //    return this;
        //}

        public FluentBuilder TransitionByResult(Func<UniTask> f, string errorDest, string successDest = null) {
            var (tPass, tFail) = m_stateInContext.BindAsyncLamdaAndTransitionByResult(f);
            LinkTransition(tFail, errorDest);
            if (successDest != null) {
                TryToLinkTransition(tPass, successDest);
            } else {
                m_transitionsToNextState.Add(tPass);
            }
            return this;
        }

        public FluentBuilder TransitionByResult(Func<UniTask<bool>> f, string errorDest, string successDest = null) {
            var (tPass, tFail) = m_stateInContext.BindAsyncLamdaAndTransitionByResult(f);
            LinkTransition(tFail, errorDest);
            if (successDest != null) {
                TryToLinkTransition(tPass, successDest);
            } else {
                m_transitionsToNextState.Add(tPass);
            }
            return this;
        }


        public FluentBuilder TransitionAfter(Func<UniTask> f, string dest = null) {
            var t = m_stateInContext.BindAsyncLamdaAndTransitionOnDone(f);
            if (dest != null) {
                TryToLinkTransition(t, dest);
            } else {
                m_transitionsToNextState.Add(t);
            }
            return this;
        }

        public FluentBuilder TransitionWhen(Func<bool> test, string dest = null) {
            var t = m_stateInContext.MakeDanglingTransition(test);
            if (dest != null) {
                TryToLinkTransition(t, dest);
            } else {
                m_transitionsToNextState.Add(t);
            }
            return this;
        }

        public FluentBuilder TransitionImmediately(string dest = null) {
            return this.TransitionWhen(() => true, dest);
        }

        public FluentBuilder TransitionAfter(float seconds, string dest = null) {
            var state = m_stateInContext.ToRuntimeState();
            var t = m_stateInContext.MakeDanglingTransition(() => state.GetTimeSinceEntry() > seconds);
            if (dest != null) {
                TryToLinkTransition(t, dest);
            } else {
                m_transitionsToNextState.Add(t);
            }
            return this;
        }

        private void TryToLinkTransition(ITransitionBuilder t, string dest) {
            if (m_states.ContainsKey(dest)) {
                if (m_states[dest] is NoTokenStateConstruction typedDest) {
                    t.To(typedDest);
                } else {
                    Debug.LogError("Fluent Builder: transitions to states that require tokens must provide the token");
                }
            } else {
                if (m_waitingTransitions.ContainsKey(dest)) {
                    m_waitingTransitions[dest].Add(t);
                } else {
                    m_waitingTransitions[dest] = new List<IBaseTransitionBuilder>() { t };
                }
            }
        }

        private void LinkTransition<T>(ITransitionBuilder<T> t, string dest) {
            if (m_states.ContainsKey(dest)) {
                if (m_states[dest] is WithTokenStateConstruction<T> typedDest) {
                    t.To(typedDest);
                } else if (m_states[dest] is NoTokenStateConstruction noTokenDest) {
                    t.To(noTokenDest);
                } else {
                    Debug.LogErrorFormat("Fluent Builder: Transition provided token of type {0} but destination state required type {1}", typeof(T).ToString(), m_states[dest].GetType().ToString());
                }
            } else {
                if (m_waitingTransitions.ContainsKey(dest)) {
                    m_waitingTransitions[dest].Add(t);
                } else {
                    m_waitingTransitions[dest] = new List<IBaseTransitionBuilder>() { t };
                }
            }
        }

        public FluentBuilder TransitionOn(Button button, string dest = null) {
            ITransitionBuilder t = m_stateInContext.MakeDanglingTransition(button);
            if (dest != null) {
                TryToLinkTransition(t, dest);
            } else {
                m_transitionsToNextState.Add(t);
            }
            return this;
        }

        public FluentBuilder TransitionOnButtonByResult(Button button, Func<bool> test, string trueDest, string falseDest) {
            var (onFalse, onTrue) = m_stateInContext.MakeBranchedTransitions(button, test);

            TryToLinkTransition(onFalse, falseDest);
            TryToLinkTransition(onTrue, trueDest);

            return this;
        }

        public FluentBuilder BindGameObjectActiveState(GameObject go) {
            m_stateInContext.BindBool((isOn) => {
                go.SetActive(isOn);
            });
            return this;
        }

        public FluentBuilder BindCanvasFadeInOut(CanvasGroup cg) {
            m_stateInContext.BindCanvasGroupFadeInOut(cg);
            return this;
        }

        public FluentBuilder BindGameObjectInvertActiveState(GameObject go) {
            m_stateInContext.BindBool((isOn) => {
                go.SetActive(!isOn);
            });
            return this;
        }

        public FluentBuilder OnEntry(Action action) {
            m_stateInContext.AddEntryAction(action);
            return this;
        }

        public FluentBuilder OnExit(Action action) {
            m_stateInContext.AddExitAction(action);
            return this;
        }

        public FluentBuilder OnUpdate(Action action) {
            m_stateInContext.AddEveryFrameAction(action);
            return this;
        }

        public FluentBuilder OnNegativeEntry(Action action) {
            m_stateInContext.AddNegativeEntryAction(action);
            return this;
        }

        public FluentBuilder OnNegativeExit(Action action) {
            m_stateInContext.AddNegativeExitAction(action);
            return this;
        }

        public FluentBuilder OnNegativeUpdate(Action action) {
            m_stateInContext.AddNegativeEveryFrameAction(action);
            return this;
        }

        public FluentBuilder OnUpdate(Action<IState> action) {
            // Save a reference to the current state for use in the closure.
            IState currentState = m_stateInContext.ToRuntimeState();
            m_stateInContext.AddEveryFrameAction(() => {
                action(currentState);
            });
            return this;
        }
    }

    public class FluentBuilderStateGroup {
        private StateGroup m_stateGroup;
        private FluentBuilder m_parent;

        public FluentBuilderStateGroup(StateGroup sg, FluentBuilder parent) {
            this.m_stateGroup = sg;
            this.m_parent = parent;
        }

        public FluentBuilderStateGroup OnEntry(Action a) {
            m_stateGroup.AddEntryAction(a);
            return this;
        }

        public FluentBuilderStateGroup BindGameObjectActiveState(GameObject go) {
            m_stateGroup.BindBool((isOn) => {
                go.SetActive(isOn);
            });
            return this;
        }

        public FluentBuilderStateGroup BindCanvasFadeInOut(CanvasGroup canvasGroup) {
            m_stateGroup.BindCanvasGroupFadeInOut(canvasGroup);
            return this;
        }

        public FluentBuilder CloseStateGroup() {
            return m_parent;
        }
    }
}
