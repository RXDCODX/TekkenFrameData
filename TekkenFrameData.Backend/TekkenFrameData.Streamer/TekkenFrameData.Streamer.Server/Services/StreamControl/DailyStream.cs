using System.Timers;
using Microsoft.AspNetCore.SignalR.Client;
using TekkenFrameData.Library.Models.SignalRInterfaces;
using Timer = System.Timers.Timer;

namespace TekkenFrameData.Streamer.Server.Services.StreamControl;

class MoscowDailyTimer : BackgroundService
{
    private readonly Timer _timer;
    private readonly TimeZoneInfo _moscowTimeZone;
    private readonly HubConnection _connection;

    public MoscowDailyTimer(HubConnection connection)
    {
        _connection = connection;
        _moscowTimeZone =
            TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time")
            ?? TimeZoneInfo.FindSystemTimeZoneById("Europe/Moscow");

        _timer = new Timer { AutoReset = false };
        _timer.Elapsed += OnTimerElapsed;
    }

    public void Start()
    {
        ScheduleNextRun();
    }

    private void ScheduleNextRun()
    {
        var utcNow = DateTime.UtcNow;
        var moscowNow = TimeZoneInfo.ConvertTimeFromUtc(utcNow, _moscowTimeZone);

        // Рассчитываем следующие моменты срабатывания по Москве
        var next10Am = moscowNow.Date.AddHours(10);
        var next10Pm = moscowNow.Date.AddHours(22);

        var nextRunTime =
            moscowNow < next10Am ? next10Am
            : moscowNow < next10Pm ? next10Pm
            : next10Am.AddDays(1);

        // Конвертируем обратно в UTC для таймера
        var nextRunUtc = TimeZoneInfo.ConvertTimeToUtc(nextRunTime, _moscowTimeZone);
        var interval = nextRunUtc - utcNow;

        _timer.Interval = interval.TotalMilliseconds;
        _timer.Start();

        _connection.InvokeAsync(
            nameof(IMainHubCommands.SendToAdminsTelegramMessage),
            $"Следующее срабатывание: {nextRunTime:HH:mm} MSK (через {interval.TotalHours:F1} часов)"
        );
    }

    private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        var moscowTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _moscowTimeZone);
        _connection.InvokeAsync(
            nameof(IMainHubCommands.SendToAdminsTelegramMessage),
            $"[{moscowTime:HH:mm:ss} MSK] Таймер сработал!"
        );
        ScheduleNextRun();
    }

    public void Stop()
    {
        _timer.Stop();
        _connection.InvokeAsync(
            nameof(IMainHubCommands.SendToAdminsTelegramMessage),
            "Таймер остановлен"
        );
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.CompletedTask;
    }
}
