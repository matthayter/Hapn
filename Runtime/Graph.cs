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
                    sg.RunEntryActions();
                }
                // Negative groups
                m_inactiveGroups = new HashSet<StateGroup>(m_allGroups.Except(m_activeGroups));
                foreach (var sg in m_inactiveGroups) {
                    sg.RunNegativeEntryActions();
                }

                foreach (IState i in m_stateSet) {
                    if (i == m_currentState) continue;
                    i.RunNegativeEntryActions();
                }
                m_currentState.RunEntryActions();
                foreach (ITransition t in m_currentState.transitions) {
                    t.Enable();
                }
            }
            bool m_keepCheckingTransitions = true;
            while (m_keepCheckingTransitions) {
                m_keepCheckingTransitions = false;
                foreach (ITransition t in m_currentState.transitions) {
                    if (t.CheckAndPassData()) {
                        DoTransitionWork(t.GetDestination());
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

        // Transition should already have inserted any tokens into the
        // destination state before calling this.
        public void TriggerManualTransition(IState destination) {
            DoTransitionWork(destination);
        }

        // Tries to avoid allocation at the possible expense of time
        private void DoTransitionWork(IState destination) {
            if (m_logDebug) Debug.LogFormat("Graph [{0}]: {1} -> {2}", m_name, m_currentState.Name, destination.Name);
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
            m_currentState = destination;
            if (m_currentState == null) throw new Exception("Somebody messed up the transitions!");
            m_currentState.entryTime = Time.time;
            //////

            // Entering Groups again: destGroups.except(active)
            foreach (var sg in destination.groups) {
                if (!m_activeGroups.Contains(sg)) {
                    sg.RunEntryActions();
                }
            }

            // Exiting groups again: active.except(destGroups)
            foreach (var sg in m_activeGroups) {
                if (!destination.groups.Contains(sg)) {
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
            outgoingState.RunNegativeEntryActions();
            m_currentState.RunEntryActions();
            foreach (ITransition u in m_currentState.transitions) {
                u.Enable();
            }

        }
    }
}