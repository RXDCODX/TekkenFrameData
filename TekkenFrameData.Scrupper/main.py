from scraper.scraper import Scraper 
import sys

from scraper.utils import load_page

# Проверка версии Python
MIN_PYTHON_VERSION = (3, 11)

if sys.version_info < MIN_PYTHON_VERSION:
    sys.exit(
        f"Требуется Python {MIN_PYTHON_VERSION[0]}.{MIN_PYTHON_VERSION[1]} или выше. Текущая версия: {sys.version}")

# Основной код скрипта
print("Всё в порядке, скрипт запущен на Python 3.11 или новее!")

# main.py

def main():
    base_url = "https://tekkendocs.com"
    scraper = Scraper(base_url)
    html = load_page(base_url)

    # Парсим персонажей
    characters = scraper.scrape_characters(html)
    for character in characters:
        print(character)

if __name__ == "__main__":
    main()