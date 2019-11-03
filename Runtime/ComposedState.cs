
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;


namespace Hapn {

    // Not meant to be a base class; build up the state with composition.
    public class ComposedState : EntranceTypes, NoTokenStateConstruction {
        public List<ITransition> transitions { get; } = new List<ITransition>();
        public List<Action> entryActions { get; } = new List<Action>();
        public List<Action> everyFrame { get; } = new List<Action>();
        public List<Action> exitActions { get; } = new List<Action>();
        public List<Action> negativeEntryActions { get; } = new List<Action>();
        public List<Action> negativeEveryFrameActions { get; } = new List<Action>();
        public List<Action> negativeExitActions { get; } = new List<Action>();
        public List<StateGroup> groups { get; set; }  = new List<StateGroup>();
        public float entryTime { get; set; } = 0f;
        public Graph Graph { get; set; }
        public string Name { get; }

        //public List<StateGroup> groups { get; set; }

        public ComposedState (Graph graph, string name) {
            this.Graph = graph;
            this.Name = name;
        }

        public void RunEveryFrameActions() {
            foreach (var a in everyFrame) {
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

        public void AddTransition(ITransition t) {
            transitions.Add(t);
        }
        public void AddGroup(StateGroup sg) {
            groups.Add(sg);
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
        public EntranceTypes ToEntranceType() {
            return this;
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

    public class ComposedState<T> : EntranceTypes<T>, WithTokenStateConstruction<T> {
        public List<ITransition> transitions { get; } = new List<ITransition>();
        public List<Action> entryActions { get; } = new List<Action>();
        public List<Action> everyFrame { get; } = new List<Action>();
        public List<Action> exitActions { get; } = new List<Action>();
        public List<Action> negativeEntryActions { get; } = new List<Action>();
        public List<Action> negativeEveryFrameActions { get; } = new List<Action>();
        public List<Action> negativeExitActions { get; } = new List<Action>();
        public List<StateGroup> groups { get; set; }  = new List<StateGroup>();
        public float entryTime { get; set; } = 0f;
        public Graph Graph { get; set; }
        public string Name { get; }

        public T token { get; set; }
        
        public ComposedState (Graph graph, string name) {
            this.Graph = graph;
            this.Name = name;
        }

        public void RunEveryFrameActions() {
            foreach (var a in everyFrame) {
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

        public void Enter(T token1) {
            this.token = token1;
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

    public static class ComposedStateExtensions {

        public static void BindBool(this IStateConstruction state, Action<bool> action)
        {
            state.AddEntryAction(() => action(true));
            state.AddNegativeEntryAction(() => action(false));
        }


        public static void BindEaseAnimation(this IStateConstruction state, float duration, Func<(Vector3, Vector3)> initValues, Action<Vector3> output) {
            var curve = AnimationCurve.EaseInOut(0f, 0f, duration, 1f);
            Vector3 start = default(Vector3);
            Vector3 end = default(Vector3);
            state.AddEntryAction(() => {
                var values = initValues();
                start = values.Item1;
                end = values.Item2;
            });
            state.AddEveryFrameAction(() => {
                float sinceEntry = Time.time - state.entryTime; 
                if (sinceEntry < duration) {
                    output(Vector3.Lerp(start, end, curve.Evaluate(sinceEntry)));
                } else {
                    output(end);
                }
            });
        }

        public static void BindEaseAnimation(this IStateConstruction state, float duration, Func<(float, float)> initValues, Action<float> output) {
            var curve = AnimationCurve.EaseInOut(0f, 0f, duration, 1f);
            state.BindAnimation(duration, curve, initValues, output);
        }

        public static void BindAnimation(this IStateConstruction state, float duration, AnimationCurve curve, Func<(float, float)> initValues, Action<float> output) {
            state.BindAnimation(duration, AdditionalHelpers.LerpFloat, initValues, output, curve);
        }

        public static void BindHapnTween(this IStateConstruction state, HapnTweenAdapter tween) {
            state.BindAnimation(tween.duration, AdditionalHelpers.LerpVec2, () => (tween.startPos, tween.endPos), (val) => tween.toChange?.Invoke(val), tween.curve);
        }

        public static void BindHapnTween(this IStateConstruction state, HapnAnchorTweenAdapter tween) {
            state.BindAnimation(tween.duration, AdditionalHelpers.LerpRect, () => (tween.startOrActiveAnchors, tween.endOrInactiveAnchors), (val) => tween.SetAnchors(val), tween.curve);
        }

        public static void BindHapnTweenBothWays(this IStateConstruction state, HapnTweenAdapter tween) {
            state.BindAnimationBothWays(() => tween.duration, AdditionalHelpers.LerpVec2, tween.startPos, tween.endPos, (val) => tween.toChange?.Invoke(val), tween.curve);
        }

        public static void BindHapnTweenBothWays(this IStateConstruction state, HapnAnchorTweenAdapter tween) {
            state.BindAnimationBothWays(() => tween.duration, AdditionalHelpers.LerpRect, tween.startOrActiveAnchors, tween.endOrInactiveAnchors, (val) => tween.SetAnchors(val), tween.curve);
        }

        public static void BindAnimationBothWays<T>(this IStateConstruction state, Func<float> getDuration, Func<T, T, float, T> lerp, T inState, T outState, Action<T> output, AnimationCurve curve) {
            float timeIntoActivePosition = 0f;
            bool started = false;
            float duration = 0f;

            state.AddEntryAction(() => {
                duration = getDuration();
                timeIntoActivePosition = Mathf.Clamp(timeIntoActivePosition, 0f, duration);
                if (!started) {
                    started = true;
                    // Starting in active position
                    timeIntoActivePosition = duration;
                    output(inState);
                }
            });
            state.AddNegativeEntryAction(() => {
                duration = getDuration();
                timeIntoActivePosition = Mathf.Clamp(timeIntoActivePosition, 0f, duration);
                if (!started) {
                    started = true;
                    // Starting in inactive position
                    timeIntoActivePosition = 0f;
                    output(outState);
                }
            });
            state.AddEveryFrameAction(() => {
                if (timeIntoActivePosition == duration) return;

                timeIntoActivePosition += Time.deltaTime;

                if (timeIntoActivePosition < duration) {
                    output(lerp(outState, inState, AdditionalHelpers.EvalCurveNormalized(curve, duration, timeIntoActivePosition)));
                } else {
                    timeIntoActivePosition = duration;
                    output(inState);
                }
            });
            state.AddNegativeEveryFrameAction(() => {
                if (timeIntoActivePosition == 0f) return;

                timeIntoActivePosition -= Time.deltaTime;

                if (timeIntoActivePosition > 0f) {
                    output(lerp(outState, inState, AdditionalHelpers.EvalCurveNormalized(curve, duration, timeIntoActivePosition)));
                } else {
                    timeIntoActivePosition = 0f;
                    output(outState);
                }
            });
        }

        // Returns: A function to get whether the animation is finished.
        public static Func<bool> BindAnimation<T>(this IStateConstruction state, float duration, Func<T, T, float, T> lerp, Func<(T, T)> initValues, Action<T> output, AnimationCurve curve) {
            T start = default(T);
            T end = default(T);
            bool done = false;
            state.AddEntryAction(() => {
                (start, end) = initValues();
                done = false;
            });
            state.AddEveryFrameAction(() => {
                if (done) return;
                float sinceEntry = Time.time - state.entryTime;
                if (sinceEntry < duration) {
                    output(lerp(start, end, AdditionalHelpers.EvalCurveNormalized(curve, duration, sinceEntry)));
                } else {
                    output(lerp(start, end, AdditionalHelpers.EvalCurveNormalized(curve, duration, duration)));
                    done = true;
                }
            });
            return () => done;
        }

        public static void StateFromHapnTween(this IStateConstruction state, HapnTweenAdapter m_tween) {
            state.BindAnimation(m_tween.duration, AdditionalHelpers.LerpVec2, () => (m_tween.startPos, m_tween.endPos), (val) => m_tween.toChange?.Invoke(val), m_tween.curve);
        }

        // Delivers a periodic float every frame in the range [-1,1] along with a periodCount. timeOffset starts the first later into the first period, and should be in the range [0f, periodSecs)
        public static void BindPeriodic(this IStateConstruction state, float periodSecs, float timeOffset, Action<float, int> output) {
            state.AddEveryFrameAction(() => {
                float nowTime = (Time.time) - state.entryTime + timeOffset;
                int cycles = Mathf.FloorToInt(nowTime / periodSecs);
                output(Mathf.Sin(Mathf.PI * 2f * nowTime / periodSecs), cycles);
            });
        }

        public static void BindCanvasGroupFadeInOut(this IStateConstruction state, IStateConstruction fadeOutState, CanvasGroup cg) {
            state.BindBool(isOn => {
                cg.blocksRaycasts = isOn;
                cg.interactable = isOn;
            });

            state.BindEaseAnimation(
                0.5f,
                () => (cg.alpha, 1f),
                (float v) => cg.alpha = v);
            fadeOutState.BindEaseAnimation(
                0.5f,
                () => (cg.alpha, 0f),
                (float v) => cg.alpha = v);
        }


    }

}