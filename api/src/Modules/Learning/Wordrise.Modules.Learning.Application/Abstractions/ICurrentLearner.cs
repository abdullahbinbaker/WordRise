namespace Wordrise.Modules.Learning.Application.Abstractions;

/// <summary>The signed-in learner making the request.</summary>
public interface ICurrentLearner
{
    Guid Id { get; }
}
