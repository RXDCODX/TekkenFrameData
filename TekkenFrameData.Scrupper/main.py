from scraper.scraper import Scraper
from scraper.utils import load_page
import psycopg2
import sys

# Проверка версии Python
MIN_PYTHON_VERSION = (3, 11)

if sys.version_info < MIN_PYTHON_VERSION:
    sys.exit(
        f"Требуется Python {MIN_PYTHON_VERSION[0]}.{MIN_PYTHON_VERSION[1]} или выше. Текущая версия: {sys.version}")

# Основной код скрипта
print("Всё в порядке, скрипт запущен на Python 3.11 или новее!")

# Функция для подключения к базе данных
def connect_to_db():
    try:
        conn = psycopg2.connect(
            dbname="tekken_db",
            user="postgres",
            password="postgres",
            host="localhost",
            port="5432"
        )
        return conn
    except Exception as e:
        print(f"Ошибка подключения к базе данных: {e}")
        return None

# Функция для сохранения персонажей в базу данных
def save_characters_to_db(conn, characters):
    try:
        with conn.cursor() as cursor:
            for character in characters:
                # Вставляем персонажа, если он ещё не существует
                cursor.execute("""
                    INSERT INTO tekken_characters (name, image_url, href)
                    VALUES (%s, %s, %s)
                    ON CONFLICT (name) DO NOTHING;
                """, (character.name, character.image_url, character.href))

                # Вставляем движения персонажа
                for move in character.movelist:
                    cursor.execute("""
                        INSERT INTO tekken_moves (
                            character_name, command, hit_level, damage, start_up_frame, block_frame, hit_frame, counter_hit_frame, notes,
                            power_crush, heat_burst, heat_engage, heat_smash, requires_heat, tornado, homing, throw, stance_code, stance_name
                        )
                        VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s)
                        ON CONFLICT (character_name, command) DO NOTHING;
                    """, (
                        move.character_name, move.command, move.hit_level, move.damage, move.start_up_frame, move.block_frame, move.hit_frame, move.counter_hit_frame, move.notes,
                        move.power_crush, move.heat_burst, move.heat_engage, move.heat_smash, move.requires_heat, move.tornado, move.homing, move.throw, move.stance_code, move.stance_name
                    ))

        conn.commit()
        print("Данные успешно сохранены в базу данных.")
    except Exception as e:
        print(f"Ошибка при сохранении данных в базу данных: {e}")
        conn.rollback()

# Основная функция
def main():
    base_url = "https://tekkendocs.com"
    scraper = Scraper(base_url)
    html = load_page(base_url)

    # Парсим персонажей
    characters = scraper.scrape_characters(html)

    # Подключаемся к базе данных
    conn = connect_to_db()
    if conn:
        # Сохраняем данные в базу данных
        save_characters_to_db(conn, characters)
        conn.close()

if __name__ == "__main__":
    main()