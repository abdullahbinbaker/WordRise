namespace Wordrise.Modules.Learning.Application.Abstractions;

/// <summary>
/// The learner's calendar day. Streaks and reviews change at local midnight, not UTC midnight.
/// </summary>
public interface ILearningCalendar
{
    DateOnly Today { get; }
}
