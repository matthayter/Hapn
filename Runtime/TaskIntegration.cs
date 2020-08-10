using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Cysharp.Threading.Tasks;

namespace Hapn.UniRx {
    public static class TaskIntegration {

        public static (NoTokenStateConstruction state, ITransitionBuilder transition) StateFromAsyncLamda(this Graph graph, Func<UniTask> task) {
            var s = graph.NewState();

            return (s, BindAsyncLamdaAndTransitionOnDone(s, task));
        }

        // Note on transition timing: The outgoing transition will trigger on the update() after the task completes. It may
        // be desired that the transition is triggered 'manually' as soon as the task completes.
        public static ITransitionBuilder BindAsyncLamdaAndTransitionOnDone(this IStateConstruction state, Func<UniTask> task) {
            // If the state is entered then exited then entered again before the first UniTask completes,
            // we need a way to have the first task not cause a transition. Use a simple counter.
            uint execCounter = 0;
            bool transitionShouldTrigger = false;
            state.AddEntryAction(async () => {
                transitionShouldTrigger = false;
                execCounter++;
                uint thisTasksCounter = execCounter;

                await task();

                if (thisTasksCounter == execCounter) {
                    transitionShouldTrigger = true;
                }
            });

            var t = new MultiTransition(state.Graph, state.ToRuntimeState());
            state.AddTransition(t);

            t.When(() => transitionShouldTrigger);
            return t;
        }

        public static (ITransitionBuilder success, ITransitionBuilder<Exception> fail) BindAsyncLamdaAndTransitionByResult(this IStateConstruction state, Func<UniTask> task) {
            return state.BindAsyncLamdaAndTransitionByResult(() => task().ContinueWith(() => true));
        }

        public static (ITransitionBuilder success, ITransitionBuilder<Exception> fail) BindAsyncLamdaAndTransitionByResult(this IStateConstruction state, Func<UniTask<bool>> task) {
            // If the state is entered then exited then entered again before the first UniTask completes,
            // we need a way to have the first task not cause a transition. Avoid alloc by using a simple counter.
            uint execCounter = 0;
            bool successTransitionShouldTrigger = false;
            Exception e = null;
            state.AddEntryAction(async () => {
                successTransitionShouldTrigger = false;
                e = null;
                execCounter++;
                uint thisTasksCounter = execCounter;

                try {
                    bool result = await task();
                    if (thisTasksCounter == execCounter) {
                        if (result) {
                            successTransitionShouldTrigger = true;
                        } else {
                            e = new Exception("Test returned false");
                        }
                    }
                } catch (Exception eInstance) {
                    Debug.LogException(eInstance);
                    if (thisTasksCounter == execCounter) {
                        e = eInstance;
                    }
                }
            });

            var tFail = new MultiTransition<Exception>(state.Graph, state.ToRuntimeState());
            state.AddTransition(tFail);

            var tSuccess = new MultiTransition(state.Graph, state.ToRuntimeState());
            state.AddTransition(tSuccess);

            tSuccess.When(() => successTransitionShouldTrigger);
            tFail.When(() => e);
            return (tSuccess, tFail);
        }


        // Runtime helpers - these are useful at runtime, not during graph building.
        public static async UniTask RunHapnTween(HapnVec3Tween tween) {
            var firstKey = tween.curve.keys[0];
            var lastKey = tween.curve.keys[tween.curve.length - 1];
            float curveDuration = lastKey.time - firstKey.time;
            float startTime = Time.time;

            tween.toChange.Invoke(Vector3.LerpUnclamped(tween.startPos, tween.endPos, firstKey.value));

            await UniTask.Yield();

            
            for (float elapsedTime = Time.time - startTime; elapsedTime < tween.duration; elapsedTime = Time.time - startTime) {
                var lerpResult = Vector3.LerpUnclamped(tween.startPos, tween.endPos, tween.curve.Evaluate(firstKey.time + curveDuration * (elapsedTime / tween.duration)));
                tween.toChange.Invoke(lerpResult);
                await UniTask.Yield();
            }
            tween.toChange.Invoke(Vector3.LerpUnclamped(tween.startPos, tween.endPos, lastKey.value));
        }
    }
}
