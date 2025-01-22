import pytest
import requests_mock
from scraper.scraper import Scraper
from scraper.models import Character, Move
from scraper.utils import load_page  # Импортируем load_page из utils

# Пример HTML-страницы с персонажами
SAMPLE_HTML = """
<html>
    <ul>
        <li class="cursor-pointer">
            <a class="cursor-pointer" href="/character1">Link</a>
            <div class="overflow-hidden text-ellipsis whitespace-nowrap text-center capitalize text-text-primary max-xs:text-xs">Character 1</div>
            <img src="https://example.com/image1.jpg">
        </li>
        <li class="cursor-pointer">
            <a class="cursor-pointer" href="/character2">Link</a>
            <div class="overflow-hidden text-ellipsis whitespace-nowrap text-center capitalize text-text-primary max-xs:text-xs">Character 2</div>
            <img src="https://example.com/image2.jpg">
        </li>
    </ul>
</html>
"""

# Пример HTML-страницы с движениями
SAMPLE_MOVELIST_HTML = """
<html>
    <tbody>
        <tr class="rt-TableRow">
            <td class="rt-TableCell"><a data-discover="true">1,2,3</a></td>
            <td class="rt-TableCell">Mid</td>
            <td class="rt-TableCell">10</td>
            <td class="rt-TableCell">i10</td>
            <td class="rt-TableCell">+5</td>
            <td class="rt-TableCell">+10</td>
            <td class="rt-TableCell">+15</td>
            <td class="rt-TableCell">
                <div>Power Crush</div>
                <div>Homing</div>
            </td>
        </tr>
    </tbody>
</html>
"""

# Фикстура для создания экземпляра Scraper
@pytest.fixture
def scraper():
    return Scraper("https://tekkendocs.com")

# Тест: Загрузка HTML-страницы
def test_load_page():
    with requests_mock.Mocker() as m:
        m.get("https://tekkendocs.com", text=SAMPLE_HTML)
        html = load_page("https://tekkendocs.com")  # Используем load_page из utils
        assert html == SAMPLE_HTML

# Тест: Парсинг персонажей
def test_scrape_characters(scraper):
    with requests_mock.Mocker() as m:
        m.get("https://tekkendocs.com", text=SAMPLE_HTML)
        m.get("https://tekkendocs.com/alisa", text=SAMPLE_MOVELIST_HTML)  # Мок для движений
        characters = scraper.scrape_characters(SAMPLE_HTML)
        assert len(characters) == 2
        assert isinstance(characters[0], Character)
        assert characters[0].name == "Character 1"
        assert characters[0].image_url == "https://tekkendocs/assets/alisa-128-DBtqQyfd.webp"
        assert characters[0].href == "/character1"

# Тест: Парсинг движений
def test_scrape_movelist(scraper):
    character = Character("Character 1", "https://tekkendocs/assets/alisa-128-DBtqQyfd.webp", "/character1")
    with requests_mock.Mocker() as m:
        m.get("https://tekkendocs.com/alisa", text=SAMPLE_MOVELIST_HTML)
        movelist = scraper.scrape_movelist(character, "https://tekkendocs.com/alisa")
        assert len(movelist) == 1
        assert isinstance(movelist[0], Move)
        assert movelist[0].command == "1,2,3"
        assert movelist[0].hit_level == "mid"
        assert movelist[0].damage == "10"
        assert movelist[0].start_up_frame == "i10"
        assert movelist[0].block_frame == "+5"
        assert movelist[0].hit_frame == "+10"
        assert movelist[0].counter_hit_frame == "+15"
        assert movelist[0].notes == "power crush\nhoming"
        assert movelist[0].power_crush is True
        assert movelist[0].homing is True