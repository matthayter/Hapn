using System;
using System.Collections.Generic;
using UniRx.Async;
using Hapn.UniRx;

namespace Hapn
{
    // Relies on UniRx.Async for now
    public class FluentBuilder {

        private Graph m_graph;
        private IStateConstruction m_stateInContext;
        private List<IBaseTransitionBuilder> m_transitionsToNextState;

        public FluentBuilder(Graph g) {
            m_graph = g;
            m_stateInContext = null;
            m_transitionsToNextState = new List<IBaseTransitionBuilder>(5);
        }

        public FluentBuilder State(string name) {
            NoTokenStateConstruction newState = m_graph.NewState(name);
            m_stateInContext = newState;

            foreach (var t in m_transitionsToNextState) {
                var castTransition = t as ITransitionBuilder;
                if (castTransition == null) throw new InvalidOperationException("A transition was waiting to be connected to a state with a token but the new state has no token");
                castTransition.To(newState);
            }
            m_transitionsToNextState.Clear();

            return this;
        }

        public FluentBuilder State<T>(string name) {
            WithTokenStateConstruction<T> newState = m_graph.NewState<T>(name);
            m_stateInContext = newState;

            foreach (var t in m_transitionsToNextState) {
                var castTransition = t as ITransitionBuilder<T>;
                if (t == null) throw new InvalidOperationException("A transition was waiting to be connected to a state with a different token type (or no token) compared to the new state");
                castTransition.To(newState);
            }
            m_transitionsToNextState.Clear();

            return this;
        }

        public FluentBuilder Init() {
            m_graph.SetInitState(m_stateInContext.ToRuntimeState());
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

        public FluentBuilder TransitionAfter(Func<UniTask> f) {
            var t = m_stateInContext.BindAsyncLamdaAndTransitionOnDone(f);
            m_transitionsToNextState.Add(t);
            return this;
        }

        public FluentBuilder OnEntry(Action action) {
            m_stateInContext.AddEntryAction(action);
            return this;
        }
    }
}
