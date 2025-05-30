﻿using Microsoft.EntityFrameworkCore;
using TekkenFrameData.Library.DB;
using TekkenFrameData.UpdateService.Services.TelegramBotService.Abstract;

namespace TekkenFrameData.UpdateService.Services.TelegramBotService;

// Compose Polling and ReceiverService implementations
public class PollingService(IServiceProvider serviceProvider, ILogger<PollingService> logger)
    : PollingServiceBase<ReceiverService>(serviceProvider, logger);
