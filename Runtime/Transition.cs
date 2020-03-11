using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace Hapn {

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


    public static class TransitionExtensionsToStates {
        //public static ITransitionBuilder<S> Transition<S>(this S source, EntranceTypes dest) where S : IState {
        //    MultiTransition<S> multiTransition = new MultiTransition<S>();
        //    return multiTransition;
        //}
    }

    // This is the interface for use by the graph.
    // Should maybe use different interfaces for 'Check every frame' type vs 'Trigger' vs 'Subscribe' etc...
    public interface ITransition {
        bool CheckAndPassData();
        // Graph will call this to activate the next state; transition should already have filled that state with needed tokens first
        IState GetDestination();

        void Enable();
        void Disable();
    }

    #region Triggerable stuff, delete this when ready.
    public interface ITriggerable {
        void Trigger();
    }

    public interface ITriggerable<T> {
        void Trigger(T token);
    }

    public class TriggerableWrapper : ITriggerable {
        internal readonly Action triggerAction;

        public TriggerableWrapper(Action triggerAction) {
            this.triggerAction = triggerAction;
        }

        public void Trigger() {
            triggerAction();
        }
    }

    public class TriggerableWrapper<T> : ITriggerable<T> {
        internal readonly Action<T> triggerAction;

        public TriggerableWrapper(Action<T> triggerAction) {
            this.triggerAction = triggerAction;
        }

        public void Trigger(T token) {
            triggerAction(token);
        }
    }
    #endregion

    // Pull out the parts common to ITranitionBuilder that are unrelated to the destination token type.
    public interface IBaseTransitionBuilder {
        // Transitions with tokens may target no-token states by simply discarding the token.
        void To(NoTokenStateConstruction dest);
        void ToEnable(Action enable);
        void ToDisable(Action disable);
        // TODO: this should probably return ITransition. It's currently unused by anything.
        void Build();
    }

    // We'll probably need a proper builder later, but just make the Transition be it's own builder for now.
    public interface ITransitionBuilder : IBaseTransitionBuilder {
        //ITransitionBuilder To(EntranceTypes dest);
        void When(Func<bool> predicate);
    }
    public interface ITransitionBuilder<T> : IBaseTransitionBuilder {
        //ITransitionBuilder<T> To(EntranceTypes<T> dest);
        void To(WithTokenStateConstruction<T> dest);
        void When(Func<T> predicate);
        void When(Func<(bool, T)> predicate);
    }

    //public class TransitionBuilder<S> : ITransitionBuilder<S> where S : IState {
    //}

    // Reminder: this is a data class, and should have no behaviour except for trivial actions needed to statisfy the type system.
    public class MultiTransition : ITransition, ITransitionBuilder {
        private Graph m_graph;
        private IState m_dest;
        private List<Func<bool>> m_check = new List<Func<bool>>();
        // Actions that "enable/disable the transition occurring" - e.g. subscribing to a button's onClick event
        private Action m_enable = null;
        private Action m_disable = null;

        public MultiTransition(Graph graph) {
            m_graph = graph;
        }

        #region Builder methods to be moved out
        public void When(Func<bool> predicate) {
            m_check.Add(predicate);
        }

        public void To(NoTokenStateConstruction dest) {
            m_dest = dest.ToEntranceType();
        }

        public void ToEnable(Action enable) {
            if (m_enable != null) {
                var oldEnable = m_enable;
                m_enable = () => {
                    oldEnable();
                    enable();
                };
            } else {
                m_enable = enable;
            }
        }

        public void ToDisable(Action disable) {
            if (m_disable != null) {
                var oldDisable = m_disable;
                m_disable = () => {
                    oldDisable();
                    disable();
                };
            } else {
                m_disable = disable;
            }
        }

        public void Build() {
        }
        #endregion

        public bool CheckAndPassData() {
            foreach (var predicate in m_check) {
                if (predicate()) {
                    return true;
                }
            }
            return false;
        }

        public void TriggerManually() {
            m_graph.TriggerManualTransition(m_dest);
        }

        public void Disable() {
            m_disable?.Invoke();
        }

        public void Enable() {
            m_enable?.Invoke();
        }

        public IState GetDestination() {
            return m_dest;
        }
    }
    public class MultiTransition<S> : ITransition, ITransitionBuilder<S> {
        private Graph m_graph;
        private EntranceTypes m_noTokenDest;
        private EntranceTypes<S> m_dest;
        private List<Func<(bool, S)>> m_check = new List<Func<(bool, S)>>();
        // These return a non-null token on transition, null when do-not-transition.
        private List<Func<S>> m_notNullTokenChecks = new List<Func<S>>();
        // Actions that "enable/disable the transition occurring" - e.g. subscribing to a button's onClick event
        private Action m_enable = null;
        private Action m_disable = null;

        public MultiTransition(Graph graph) {
            m_graph = graph;
        }


        #region Builder methods
        public void When(Func<(bool, S)> predicate) {
            m_check.Add(predicate);
        }

        public void When(Func<S> predicate) {
            m_notNullTokenChecks.Add(predicate);
        }

        public void To(EntranceTypes<S> dest) {
            m_dest = dest;
        }
        public void To(NoTokenStateConstruction dest) {
            m_noTokenDest = dest.ToEntranceType();
        }
        public void To(WithTokenStateConstruction<S> dest) {
            m_dest = dest.ToEntranceType();
        }

        public void ToEnable(Action enable) {
            if (m_enable != null) {
                var oldEnable = m_enable;
                m_enable = () => {
                    oldEnable();
                    enable();
                };
            } else {
                m_enable = enable;
            }
        }

        public void ToDisable(Action disable) {
            if (m_disable != null) {
                var oldDisable = m_disable;
                m_disable = () => {
                    oldDisable();
                    disable();
                };
            } else {
                m_disable = disable;
            }
        }
        #endregion

        // Just to implement ITransitionBuilder for now.
        public void Build() {
        }

        public bool CheckAndPassData() {
            foreach (var predicate in m_check) {
                var result = predicate();
                if (result.Item1) {
                    if (m_dest != null) {
                        m_dest.Enter(result.Item2);
                        return true;
                    }
                    // m_noTokenDest.Enter()
                    return true;
                }
            }
            foreach (var predicate in m_notNullTokenChecks) {
                var result = predicate();
                if (result != null) {
                    if (m_dest != null) {
                        m_dest.Enter(result);
                        return true;
                    }
                    // m_noTokenDest.Enter()
                    return true;
                }
            }
            return false;
        }

        public void TriggerManually(S token) {
            // TODO: Consider making this safer.
            // Warning: this currently does not verify whether the graph is indeed in the src state. Only call this if you're sure that is the case.
            m_dest.Enter(token);
            m_graph.TriggerManualTransition(m_dest);
        }

        public void Disable() {
            m_disable?.Invoke();
        }

        public void Enable() {
            m_enable?.Invoke();
        }

        public IState GetDestination() {
            if (m_dest != null) return m_dest;
            return m_noTokenDest;
        }
    }


    // This transition is triggered by calling a method on it directly
    public class ManualTransition : ITransition {
        EntranceTypes m_destination;
        Graph m_graph;
        public ManualTransition(Graph graph) {
            m_graph = graph;
        }
        public bool CheckAndPassData() {
            return false;
        }

        public void To(EntranceTypes destination) {
            m_destination = destination;
        }

        public IState GetDestination() {
            return m_destination;
        }

        public void Trigger() {
            m_graph.TriggerManualTransition(m_destination);
        }

        public void Enable() {

        }
        public void Disable() {

        }
    }

    // This transition is triggered by calling a method on it directly
    public class ManualTransition<T> : ITransition {
        EntranceTypes<T> m_destination;
        Graph m_graph;
        public ManualTransition(Graph graph) {
            m_graph = graph;
        }
        public bool CheckAndPassData() {
            return false;
        }

        public void To(EntranceTypes<T> destination) {
            m_destination = destination;
        }

        public IState GetDestination() {
            throw new NotImplementedException();
        }

        public void Trigger(T token) {
            m_destination.Enter(token);
            m_graph.TriggerManualTransition(m_destination);
        }
        public void Enable() {

        }
        public void Disable() {

        }
    }

    public class CheckedTransition : ITransition {
        private Func<bool> m_check;
        private EntranceTypes m_destination;

        public bool CheckAndPassData() {
            return m_check();
        }
        public IState GetDestination() {
            return m_destination;
        }

        public void To(EntranceTypes destination) {
            m_destination = destination;
        }
        public void When(Func<bool> f) {
            m_check = f;
        }
        public void Enable() {

        }
        public void Disable() {

        }
    }


    public class CheckedTransition<T> : ITransition {
        private Func<TransitionCheckResult<T>> m_check;
        private Func<T> m_checkWithNotNull;
        private EntranceTypes<T> m_destination;

        public bool CheckAndPassData() {
            if (m_check != null) {
                var result = m_check();
                if (result.doTransition) {
                    m_destination.Enter(result.EntranceConditions);
                }
                return result.doTransition;
            } else {
                T result = m_checkWithNotNull();
                if (result != null) {
                    m_destination.Enter(result);
                    return true;
                }
                return false;
            }
        }
        public IState GetDestination() {
            return m_destination;
        }
        public void To(EntranceTypes<T> destination) {
            m_destination = destination;
        }
        public void When(Func<TransitionCheckResult<T>> f) {
            m_check = f;
        }
        public void WhenButNotNull(Func<T> f) {
            m_checkWithNotNull = f;
        }
        public void Enable() {

        }
        public void Disable() {

        }
    }

    // public class Transition<T, U> : Transition {
    //     public void CheckAndTransition() {
    //     }
    // }

    public struct TransitionCheckResult<T> {
        public bool doTransition;
        public T EntranceConditions;
    }

}