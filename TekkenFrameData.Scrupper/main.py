import sys

# Проверка версии Python
MIN_PYTHON_VERSION = (3, 12)

if sys.version_info < MIN_PYTHON_VERSION:
    sys.exit(
        f"Требуется Python {MIN_PYTHON_VERSION[0]}.{MIN_PYTHON_VERSION[1]} или выше. Текущая версия: {sys.version}")

# Основной код скрипта
print("Всё в порядке, скрипт запущен на Python 3.12 или новее!")


