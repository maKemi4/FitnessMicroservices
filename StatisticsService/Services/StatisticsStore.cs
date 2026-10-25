using StatisticsService.Models;

namespace StatisticsService.Services
{
    public class StatisticsStore
    {
        private readonly object _lock = new();

        private readonly List<TrainingCompletedEvent>
            _completedTrainings = new();

        private readonly HashSet<Guid> _processedEvents = new();

        public bool AddTraining(TrainingCompletedEvent training)
        {
            lock (_lock)
            {
                if (_processedEvents.Contains(training.EventId))
                    return false;

                _completedTrainings.Add(training);
                _processedEvents.Add(training.EventId);

                return true;
            }
        }

        public List<TrainingCompletedEvent> GetAll()
        {
            lock (_lock)
            {
                return _completedTrainings.ToList();
            }
        }

        public List<TrainingCompletedEvent> GetByUser(int userId)
        {
            lock (_lock)
            {
                return _completedTrainings
                    .Where(t => t.UserId == userId)
                    .ToList();
            }
        }
    }
}