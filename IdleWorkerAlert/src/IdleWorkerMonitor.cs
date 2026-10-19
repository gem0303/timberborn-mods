using System.Collections.Generic;
using Timberborn.BaseComponentSystem;
using Timberborn.BehaviorSystem;
using Timberborn.BuilderHubSystem;
using Timberborn.EntitySystem;
using Timberborn.Hauling;
using Timberborn.Localization;
using Timberborn.StatusSystem;
using Timberborn.TickSystem;
using Timberborn.TimeSystem;
using Timberborn.WorkSystem;

namespace Local.IdleWorkerAlert
{
    public sealed class IdleWorkerMonitor : TickableComponent, IAwakableComponent, IInitializableEntity
    {
        private const string StatusSpriteName = "NothingToDo";
        private const string StatusLocKey = "Local.IdleWorkerAlert.Status";
        private const string AlertLocKey = "Local.IdleWorkerAlert.Alert";

        private readonly ILoc _loc;
        private readonly IDayNightCycle _dayNightCycle;
        private readonly IdleWorkerSettings _settings;
        private readonly Dictionary<Worker, float> _idleDurations = new Dictionary<Worker, float>();
        private readonly List<Worker> _workersToRemove = new List<Worker>();

        private Workplace _workplace;
        private StatusToggle _idleStatus;
        private bool _supportedWorkplace;

        public IdleWorkerMonitor(
            ILoc loc,
            IDayNightCycle dayNightCycle,
            IdleWorkerSettings settings)
        {
            _loc = loc;
            _dayNightCycle = dayNightCycle;
            _settings = settings;
        }

        public int IdleWorkerCount { get; private set; }

        public int AssignedWorkerCount
        {
            get { return _workplace == null ? 0 : _workplace.NumberOfAssignedWorkers; }
        }

        public bool IsSupportedWorkplace
        {
            get { return _supportedWorkplace; }
        }

        public void Awake()
        {
            _workplace = GetComponent<Workplace>();
            _supportedWorkplace = GetComponent<BuilderHubSpec>() != null || GetComponent<HaulingCenter>() != null;

            if (!_supportedWorkplace)
            {
                DisableComponent();
                return;
            }

            _idleStatus = StatusToggle.CreateNormalStatusWithAlert(
                StatusSpriteName,
                _loc.T(StatusLocKey),
                _loc.T(AlertLocKey),
                0f);
        }

        public void InitializeEntity()
        {
            if (!_supportedWorkplace)
            {
                return;
            }

            GetComponent<StatusSubject>().RegisterStatus(_idleStatus);
        }

        public override void Tick()
        {
            if (!_supportedWorkplace)
            {
                return;
            }

            UpdateIdleWorkers();
            _idleStatus.Toggle(IdleWorkerCount > 0);
        }

        private void UpdateIdleWorkers()
        {
            _workersToRemove.Clear();
            foreach (Worker trackedWorker in _idleDurations.Keys)
            {
                if (!IsAssigned(trackedWorker))
                {
                    _workersToRemove.Add(trackedWorker);
                }
            }

            foreach (Worker worker in _workersToRemove)
            {
                _idleDurations.Remove(worker);
            }

            IdleWorkerCount = 0;
            float elapsedHours = _dayNightCycle.FixedDeltaTimeInHours;

            foreach (Worker worker in _workplace.AssignedWorkers)
            {
                BehaviorManager behaviorManager = worker.GetComponent<BehaviorManager>();
                bool isWaitingIdly = behaviorManager != null &&
                    behaviorManager.IsRunningBehavior<WaitInsideIdlyWorkplaceBehavior>();

                if (!isWaitingIdly)
                {
                    _idleDurations.Remove(worker);
                    continue;
                }

                float duration;
                _idleDurations.TryGetValue(worker, out duration);
                duration += elapsedHours;
                _idleDurations[worker] = duration;

                if (duration >= _settings.GracePeriodInHours.Value)
                {
                    IdleWorkerCount++;
                }
            }
        }

        private bool IsAssigned(Worker worker)
        {
            foreach (Worker assignedWorker in _workplace.AssignedWorkers)
            {
                if (assignedWorker == worker)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
