using Microsoft.Extensions.Options;
using Wordrise.Modules.Learning.Application.Abstractions;

namespace Wordrise.Modules.Learning.Infrastructure.Services;

internal sealed class LearningCalendar(TimeProvider clock, IOptions<LearningOptions> options) : ILearningCalendar
{
    private readonly TimeZoneInfo _timeZone = TimeZoneInfo.FindSystemTimeZoneById(options.Value.TimeZone);

    public DateOnly Today => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(clock.GetUtcNow(), _timeZone).DateTime);
}
