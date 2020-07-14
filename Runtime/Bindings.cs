using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace Hapn {
    public static class ComposedStateExtensions {

        public static void BindBool(this IEnterable state, Action<bool> action)
        {
            state.AddEntryAction(() => action(true));
            state.AddNegativeEntryAction(() => action(false));
        }

        public static void BindFloat(this IEnterable state, float duration, Action<float> action) {
            state.BindFloat(duration, action, AnimationCurve.EaseInOut(0f, 0f, 1f, 1f));
        }

        public static void BindFloat(this IEnterable state, float duration, Action<float> action, AnimationCurve curve) {
            state.BindAnimationBothWays(() => duration, Mathf.Lerp, 0f, 1f, action, curve);
        }

        public static void BindFloat(this IEnterable state, float duration, float outVal, float inVal, Action<float> action) {
            state.BindAnimationBothWays(() => duration, Mathf.Lerp, outVal, inVal, action, AnimationCurve.EaseInOut(0f, 0f, 1f, 1f));
        }

        public static void BindVector3(this IEnterable state, float duration, Vector3 outVal, Vector3 inVal, Action<Vector3> action) {
            state.BindAnimationBothWays(() => duration, Vector3.Lerp, outVal, inVal, action, AnimationCurve.EaseInOut(0f, 0f, 1f, 1f));
        }

        public static void BindColor(this IEnterable state, float duration, Color outVal, Color inVal, Action<Color> action) {
            state.BindAnimationBothWays(() => duration, Color.Lerp, outVal, inVal, action, AnimationCurve.EaseInOut(0f, 0f, 1f, 1f));
        }

        public static void BindQuaternion(this IEnterable state, float duration, Quaternion outVal, Quaternion inVal, Action<Quaternion> action) {
            state.BindAnimationBothWays(() => duration, Quaternion.Lerp, outVal, inVal, action, AnimationCurve.EaseInOut(0f, 0f, 1f, 1f));
        }

        public static void BindEaseAnimation(this IStateConstruction state, float duration, Func<(Vector3, Vector3)> initValues, Action<Vector3> output) {
            var curve = AnimationCurve.EaseInOut(0f, 0f, duration, 1f);
            state.EntryAnimation(duration, Vector3.LerpUnclamped, initValues, output, curve);
        }

        public static void BindEaseAnimation(this IStateConstruction state, float duration, Func<(Quaternion, Quaternion)> initValues, Action<Quaternion> output) {
            var curve = AnimationCurve.EaseInOut(0f, 0f, duration, 1f);
            state.EntryAnimation(duration, Quaternion.LerpUnclamped, initValues, output, curve);
        }

        public static void BindEaseAnimation(this IEnterable state, float duration, Func<(float, float)> initValues, Action<float> output) {
            var curve = AnimationCurve.EaseInOut(0f, 0f, duration, 1f);
            state.BindAnimation(duration, curve, initValues, output);
        }

        public static void BindAnimation(this IEnterable state, float duration, AnimationCurve curve, Func<(float, float)> initValues, Action<float> output) {
            state.EntryAnimation(duration, AdditionalHelpers.LerpFloat, initValues, output, curve);
        }

        public static void BindHapnTween(this IEnterable state, HapnTweenAdapter tween) {
            state.EntryAnimation(tween.duration, AdditionalHelpers.LerpVec2, () => (tween.startPos, tween.endPos), (val) => tween.toChange?.Invoke(val), tween.curve);
        }

        public static void BindHapnTween(this IEnterable state, HapnAnchorTweenAdapter tween) {
            state.EntryAnimation(tween.duration, AdditionalHelpers.LerpRect, () => (tween.startOrActiveAnchors, tween.endOrInactiveAnchors), (val) => tween.SetAnchors(val), tween.curve);
        }

        public static void BindHapnTweenBothWays(this IEnterable state, HapnTweenAdapter tween) {
            state.BindAnimationBothWays(() => tween.duration, AdditionalHelpers.LerpVec2, tween.endPos, tween.startPos, (val) => tween.toChange?.Invoke(val), tween.curve);
        }

        public static void BindHapnTweenBothWays(this IEnterable state, HapnAnchorTweenAdapter tween) {
            state.BindAnimationBothWays(() => tween.duration, AdditionalHelpers.LerpRect, tween.endOrInactiveAnchors, tween.startOrActiveAnchors, (val) => tween.SetAnchors(val), tween.curve);
        }

        public static void BindAnimationBothWays<T>(this IEnterable state, Func<float> getDuration, Func<T, T, float, T> lerp, T outState, T inState, Action<T> output, AnimationCurve curve) {
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
        public static Func<bool> EntryAnimation<T>(this IEnterable state, float duration, Func<T, T, float, T> lerp, Func<(T, T)> initValues, Action<T> output, AnimationCurve curve) {
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
            state.EntryAnimation(m_tween.duration, AdditionalHelpers.LerpVec2, () => (m_tween.startPos, m_tween.endPos), (val) => m_tween.toChange?.Invoke(val), m_tween.curve);
        }

        // Delivers a periodic float every frame in the range [-1,1] along with a periodCount. timeOffset starts the first later into the first period, and should be in the range [0f, periodSecs)
        public static void BindPeriodic(this IStateConstruction state, float periodSecs, float timeOffset, Action<float, int> output) {
            state.AddEveryFrameAction(() => {
                float nowTime = (Time.time) - state.entryTime + timeOffset;
                int cycles = Mathf.FloorToInt(nowTime / periodSecs);
                output(Mathf.Sin(Mathf.PI * 2f * nowTime / periodSecs), cycles);
            });
        }

        public static void BindCanvasGroupFadeInOut(this IEnterable state, CanvasGroup cg) {
            // Immediately prevent interaction
            state.BindBool(isOn => {
                cg.blocksRaycasts = isOn;
                cg.interactable = isOn;
            });

            state.BindAnimationBothWays(() => 0.4f, Mathf.Lerp, 0f, 1f, v => {
                cg.gameObject.SetActive(v > 0f);
                cg.alpha = v;
            }, AnimationCurve.EaseInOut(0f, 0f, 1f, 1f));
        }
    }

    public static class TransitionHelpers {

        /// <summary>
        /// Make a transition that the caller is responsible for triggering via Transition.TriggerManually().
        /// The caller can also make use of transition.ToEnable() and .ToDisable().
        /// </summary>
        public static Action MakeManualTransition(this IStateConstruction src, NoTokenStateConstruction dest) {
            (var t, var trigger) = MakeManualDanglingTransition(src);
            t.To(dest);
            return trigger;
        }

        public static Action<T> MakeManualTransition<T>(this IStateConstruction src, WithTokenStateConstruction<T> dest) {
            (var t, var trigger) = MakeManualDanglingTransition<T>(src);
            t.To(dest);
            return trigger;
        }

        public static (ITransitionBuilder transition, Action trigger) MakeManualDanglingTransition(this IStateConstruction src) {
            var t = new MultiTransition(src.Graph);
            bool isEnabled = false;

            t.ToEnable(() => isEnabled = true);
            t.ToDisable(() => isEnabled = false);
            src.AddTransition(t);
            return (t, () => {
                if (isEnabled) t.TriggerManually();
            });
        }

        public static (ITransitionBuilder<T> transition, Action<T> trigger) MakeManualDanglingTransition<T>(this IStateConstruction src) {
            var t = new MultiTransition<T>(src.Graph);
            bool isEnabled = false;

            t.ToEnable(() => isEnabled = true);
            t.ToDisable(() => isEnabled = false);
            src.AddTransition(t);
            return (t, (token) => {
                if (isEnabled) t.TriggerManually(token);
            });
        }

        public static void MakeTransition(this IStateConstruction src, NoTokenStateConstruction dest, Button button) {
            var t = new MultiTransition(src.Graph);
            t.To(dest);

            // Local function
            void trigger() => t.TriggerManually();

            t.ToEnable(() => button.onClick.AddListener(trigger));
            t.ToDisable(() => button.onClick.RemoveListener(trigger));
            src.AddTransition(t);
        }

        // May want to add generic version for token states.
        public static ITransitionBuilder MakeDanglingTransition(this IStateConstruction src, Button button) {
            var t = new MultiTransition(src.Graph);

            // Local function
            void trigger() => t.TriggerManually();

            t.ToEnable(() => button.onClick.AddListener(trigger));
            t.ToDisable(() => button.onClick.RemoveListener(trigger));
            src.AddTransition(t);
            return t;
        }

        // May want to add generic version for token states.
        public static ITransitionBuilder MakeDanglingTransition(this IStateConstruction src, Func<bool> when) {
            var t = new MultiTransition(src.Graph);
            t.When(when);

            src.AddTransition(t);
            return t;
        }

        public static ITransitionBuilder MakeDanglingTransition(this IStateConstruction src, UnityEvent when) {
            var t = new MultiTransition(src.Graph);
            t.ToEnable(() => when.AddListener(t.TriggerManually));
            t.ToDisable(() => when.RemoveListener(t.TriggerManually));

            src.AddTransition(t);
            return t;
        }


        public static (ITransitionBuilder onFalse, ITransitionBuilder onTrue) MakeBranchedTransitions(this IStateConstruction src, Button button, Func<bool> test) {
            var trueTransition = new MultiTransition(src.Graph);
            var falseTransition = new MultiTransition(src.Graph);

            // Local function
            void trigger() {
                bool result = test();
                if (result) {
                    trueTransition.TriggerManually();
                } else {
                    falseTransition.TriggerManually();
                }
            }

            trueTransition.ToEnable(() => button.onClick.AddListener(trigger));
            trueTransition.ToDisable(() => button.onClick.RemoveListener(trigger));

            src.AddTransition(trueTransition);
            src.AddTransition(falseTransition);
            return (falseTransition, trueTransition);
        }

        public static void MakeTransition(this IStateConstruction src, NoTokenStateConstruction dest, Func<bool> when) {
            var t = new MultiTransition(src.Graph);
            t.To(dest);
            t.When(when);

            src.AddTransition(t);
        }

        public static void MakeTransition<T>(this IStateConstruction src, WithTokenStateConstruction<T> dest, Func<T> when) {
            var t = new MultiTransition<T>(src.Graph);
            t.To(dest);
            t.When(when);

            src.AddTransition(t);
        }

        public static void TransitionAfterTime(this IStateConstruction src, NoTokenStateConstruction dest, float seconds) {
            var t = new MultiTransition(src.Graph);
            t.To(dest);
            t.When(() => {
                return Time.time - src.entryTime >= seconds;
            });

            src.AddTransition(t);
        }

    }

}