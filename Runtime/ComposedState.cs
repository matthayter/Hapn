
using System;
using System.Collections.Generic;

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
        public float exitTime { get; set; } = 0f;
        public Graph Graph { get; set; }
        public string Name { get; }

        public ComposedState (Graph graph, string name) {
            Graph = graph;
            Name = name;
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
        public float exitTime { get; set; } = 0f;
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

}