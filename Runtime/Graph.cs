using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace Hapn {
    public class Graph {
        // Used to make unique names if names are not provided.
        private static int anonCounter = 0;

        public IState m_currentState = null;
        private bool m_isTransitioning = false;
        private IState m_nextStateToTransitionTo = null;
        private bool m_hasStartedInitState = false;
        private HashSet<IState> m_stateSet = new HashSet<IState>();
        private HashSet<StateGroup> m_allGroups = new HashSet<StateGroup>();
        private HashSet<StateGroup> m_activeGroups = new HashSet<StateGroup>();
        // Initialised on first update
        private HashSet<StateGroup> m_inactiveGroups = null;
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

        public void AddGroup(StateGroup sg) {
            m_allGroups.Add(sg);
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
                // Groups
                foreach (StateGroup sg in m_currentState.groups) {
                    m_activeGroups.Add(sg);
                    sg.entryTime = Time.time;
                    sg.RunEntryActions();
                }
                // Negative groups
                m_inactiveGroups = new HashSet<StateGroup>(m_allGroups.Except(m_activeGroups));
                foreach (var sg in m_inactiveGroups) {
                    sg.exitTime = Time.time;
                    sg.RunNegativeEntryActions();
                }

                foreach (IState i in m_stateSet) {
                    if (i == m_currentState) continue;
                    i.exitTime = Time.time;
                    i.RunNegativeEntryActions();
                }
                m_currentState.entryTime = Time.time;
                m_currentState.RunEntryActions();
                foreach (ITransition t in m_currentState.transitions) {
                    // This may call TriggerManualTransition() - caution needed.
                    t.Enable();
                }
            }
            bool m_keepCheckingTransitions = true;
            while (m_keepCheckingTransitions) {
                m_keepCheckingTransitions = false;
                foreach (ITransition t in m_currentState.transitions) {
                    if (t.CheckAndPassData()) {
                        m_nextStateToTransitionTo = t.GetDestination();
                        DoTransitionLoopWhileExternallyStimulatedTransitionsContinueToOccur();
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
            foreach (var sg in m_activeGroups) {
                sg.RunEveryFrameActions();
            }
            foreach (var sg in m_inactiveGroups) {
                sg.RunNegativeEveryFrameActions();
            }
            
        }

        // Transition should already have inserted any tokens into the destination state before calling this.
        // This is the only entry point that can cause transitions triggered by some outside stimulus. All of the careful state-management goes here.
        public void TriggerManualTransition(IState source, IState destination) {
            if (m_currentState != source) {
                if (m_logDebug) Debug.LogFormat("Graph [{0}]: Manual transition was triggered from {1}, but current state is {2}. Ignoring.", m_name, source.Name, m_currentState.Name);
                return;
            }
            // This transition was triggered as part of some transition work. May be valid...
            if (m_nextStateToTransitionTo == null) {
                // ...like a transition triggered from state A as part of state A's OnEnable actions; or invalid...
                m_nextStateToTransitionTo = destination;
            } else {
                // ...like if from state B but triggered as part of state B's exit actions.
                if (m_logDebug) Debug.LogFormat("Graph [{0}]: Manual transition was triggered from {1}, which is the current state, but we are already in the process of transitioning to {2}.", m_name, source.Name, m_nextStateToTransitionTo.Name);

            }
            if (m_isTransitioning) {
                return;
            }
            DoTransitionLoopWhileExternallyStimulatedTransitionsContinueToOccur();
        }

        private void DoTransitionLoopWhileExternallyStimulatedTransitionsContinueToOccur() {
            m_isTransitioning = true;
            while (m_nextStateToTransitionTo != null) {
                DoTransitionWork();
            }
            m_isTransitioning = false;
        }

        /* For now, Ordering of events looks like this, when exiting state A and entering state B:
         * IN STATE A
         * Disable A transitions
         * Exit State A
         * Exit State A Groups (includes negative(B))
         * -----------
         * Enter State B Groups (includes negative(A))
         * Enter State B
         * Enable B transitions
         * IN STATE B
        */
        // Tries to avoid allocation at the possible expense of time by testing Set membership multiple times instead of creating new sets with SetA.except(SetB) etc
        private void DoTransitionWork() {
            var destination = m_nextStateToTransitionTo;

            if (destination == null) throw new Exception("Hapn: transition occured with null destination!");
            var outgoingState = m_currentState;
            foreach (ITransition u in m_currentState.transitions) {
                u.Disable();
            }
            m_currentState.RunExitActions();

            // Run exit actions for the negativeState of the incoming state: we're exiting out of the meta-state represented by not(destination)
            destination.RunNegativeExitActions();
            // StateGroups
            // Exiting groups: active.except(destGroups)
            foreach (var sg in m_activeGroups) {
                if (!destination.groups.Contains(sg)) {
                    if (m_logDebug) Debug.LogFormat("Graph [{0}]: Exiting group {1}", m_name, sg.name);
                    sg.RunExitActions();
                }
            }
            // Entering Groups: destGroups.except(active)
            foreach (var sg in destination.groups) {
                if (!m_activeGroups.Contains(sg)) {
                    sg.RunNegativeExitActions();
                }
            }

            //////
            if (m_logDebug) Debug.LogFormat("Graph [{0}]: {1} -> {2}", m_name, m_currentState.Name, destination.Name);
            m_currentState = m_nextStateToTransitionTo;
            m_nextStateToTransitionTo = null;
            m_currentState.entryTime = Time.time;
            if (m_currentState.transitions.Count == 0) {
                Debug.LogWarningFormat("Current state '{0}' has no transitions. This state will never exit.", m_currentState.Name);
            }
            //////

            // Entering Groups again: destGroups.except(active)
            foreach (var sg in destination.groups) {
                if (!m_activeGroups.Contains(sg)) {
                    if (m_logDebug) Debug.LogFormat("Graph [{0}]: Entering group {1}", m_name, sg.name);
                    sg.entryTime = Time.time;
                    sg.RunEntryActions();
                }
            }

            // Exiting groups again: active.except(destGroups)
            foreach (var sg in m_activeGroups) {
                if (!destination.groups.Contains(sg)) {
                    sg.exitTime = Time.time;
                    sg.RunNegativeEntryActions();
                }
            }

            // Update active groups
            m_activeGroups.Clear();
            foreach (var sg in destination.groups) {
                m_activeGroups.Add(sg);
            }
            m_inactiveGroups.Clear();
            foreach (var sg in m_allGroups) {
                if (!m_activeGroups.Contains(sg)) {
                    m_inactiveGroups.Add(sg);
                }
            }

            // Run entry actions of the negativeState for the outgoing state
            outgoingState.exitTime = Time.time;
            outgoingState.RunNegativeEntryActions();

            m_currentState.RunEntryActions();
            foreach (ITransition u in m_currentState.transitions) {
                // This may call TriggerManualTransition() - caution needed.
                u.Enable();
            }
        }
    }
}
