using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace Hapn {
    // Reminder: this is a data class, and should have no behaviour except for trivial actions needed to statisfy the type system.
    // TODO: this should be a data class - no need for private members.
    public class MultiTransition : ITransition, ITransitionBuilder {
        public Graph m_graph;
        public IState m_dest;
        public List<Func<bool>> m_check = new List<Func<bool>>();
        // Actions that "enable/disable the transition occurring" - e.g. subscribing to a button's onClick event
        public Action m_enable = null;
        public Action m_disable = null;

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

    // TODO: this should be a data class - no need for private members.
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

    // This is the interface for use by the graph.
    public interface ITransition {
        bool CheckAndPassData();
        // Graph will call this to activate the next state; transition should already have filled that state with needed tokens first
        IState GetDestination();

        void Enable();
        void Disable();
    }

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
}