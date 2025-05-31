#!/bin/bash

REPO_DIR="$HOME/Загрузки/git/TekkenFrameData"
REPO_URL="https://github.com/RXDCODX/TekkenFrameData"

# Проверяем существование директории
if [ -d "$REPO_DIR" ]; then
    echo "Директория существует, обновляю репозиторий..."
    cd "$REPO_DIR"
    git pull
else
    echo "Директория не найдена, клонирую репозиторий..."
    gh repo clone "$REPO_URL" "$REPO_DIR"
fi

cd $HOME/Загрузки/git/TekkenFrameData/utils

sh docker_build_only_watcher.sh