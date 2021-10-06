using System;
using System.Collections.Generic;

namespace Hapn {
    public class StateGroup : IEnterable {
        public string name;
        public Graph graph;
        // Shared with State:
        // entry, exit, everyFrame actions
        public List<Action> entryActions { get; } = new List<Action>();
        public List<Action> everyFrame { get; } = new List<Action>();
        public List<Action> exitActions { get; } = new List<Action>();

        public List<Action> negativeEntryActions { get; } = new List<Action>();
        public List<Action> negativeEveryFrameActions { get; } = new List<Action>();
        public List<Action> negativeExitActions { get; } = new List<Action>();

        public List<ITransition> transitions { get; } = new List<ITransition>();


        public float entryTime { get; set; } = 0f;
        public float exitTime { get; set; } = 0f;
        // NOT transitions... yet

        public StateGroup(string name, Graph graph) {
            this.name = name;
            this.graph = graph;
        }

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

    public static class StateGroupExtensions {
        public static void TransitionWhen(this StateGroup stateGroup, Func<bool> test) {

        }

        public static StateGroupTransition MakeDanglingTransition(this StateGroup src, Func<bool> when) {
            var t = new StateGroupTransition(src.graph, src);
            t.When(when);

            src.AddTransition(t);
            return t;
        }

    }
}