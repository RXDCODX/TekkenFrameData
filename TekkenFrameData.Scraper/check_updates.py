import subprocess
import sys

# Проверяем устаревшие пакеты
result = subprocess.run(["pip", "list", "--outdated"], capture_output=True, text=True)

# Если есть устаревшие пакеты, завершаем с ошибкой
if result.stdout:
    print("Обнаружены устаревшие пакеты:")
    print(result.stdout)
    sys.exit(1)
else:
    print("Все пакеты актуальны.")