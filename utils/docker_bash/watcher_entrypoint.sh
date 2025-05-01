#!/bin/sh
set -e

# Run database migrations
/app/update_database.sh

# Start main application
exec dotnet /app/TekkenFrameData.Watcher.dll