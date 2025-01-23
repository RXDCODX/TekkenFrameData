# tests/test_all.py
import pytest
from bs4 import BeautifulSoup
from scraper.models import Character, Move
from scraper.scraper import Scraper
from scraper.utils import load_page
import requests_mock

# Тесты для models.py
def test_character_initialization():
    character = Character(name="Test Character", image_url="http://example.com/image.jpg", href="/character/test")
    assert character.name == "Test Character"
    assert character.image_url == "http://example.com/image.jpg"
    assert character.href == "/character/test"
    assert len(character.movelist) == 0

def test_move_initialization():
    move = Move(
        character_name="Test Character",
        command="1, 2, 3",
        hit_level="mid",
        damage="20",
        start_up_frame="10",
        block_frame="-5",
        hit_frame="+5",
        counter_hit_frame="+10",
        notes="power crush"
    )
    assert move.character_name == "Test Character"
    assert move.command == "1, 2, 3"
    assert move.hit_level == "mid"
    assert move.damage == "20"
    assert move.start_up_frame == "10"
    assert move.block_frame == "-5"
    assert move.hit_frame == "+5"
    assert move.counter_hit_frame == "+10"
    assert move.notes == "power crush"

# Тесты для scraper.py
def test_scrape_characters():
    scraper = Scraper(base_url="http://example.com")
    html = """
    <ul>
        <li class="cursor-pointer">
            <a class="cursor-pointer" href="/character/test"></a>
            <div class="overflow-hidden text-ellipsis whitespace-nowrap text-center capitalize text-text-primary max-xs:text-xs">Test Character</div>
            <img src="http://example.com/image.jpg">
        </li>
    </ul>
    """
    
    # Мокируем запрос к /character/test
    with requests_mock.Mocker() as m:
        m.get("http://example.com/character/test", text="<html>Test Movelist</html>")
        characters = scraper.scrape_characters(html)
    
    assert len(characters) == 1
    assert characters[0].name == "Test Character"
    assert characters[0].image_url == "http://example.com/image.jpg"
    assert characters[0].href == "/character/test"

def test_scrape_movelist():
    scraper = Scraper(base_url="http://example.com")
    html = """
    <tbody>
        <tr class="rt-TableRow">
            <td class="rt-TableCell"><a data-discover="true">1, 2, 3</a></td>
            <td class="rt-TableCell">mid</td>
            <td class="rt-TableCell">20</td>
            <td class="rt-TableCell">10</td>
            <td class="rt-TableCell">-5</td>
            <td class="rt-TableCell">+5</td>
            <td class="rt-TableCell">+10</td>
            <td class="rt-TableCell"><div>power crush</div></td>
        </tr>
    </tbody>
    """
    
    character = Character(name="Test Character", image_url="http://example.com/image.jpg", href="/character/test")
    
    # Мокируем запрос к /character/test
    with requests_mock.Mocker() as m:
        m.get("http://example.com/character/test", text=html)
        movelist = scraper.scrape_movelist(character, "http://example.com/character/test")
    
    assert len(movelist) == 1
    assert movelist[0].command == "1, 2, 3"
    assert movelist[0].hit_level == "mid"
    assert movelist[0].damage == "20"
    assert movelist[0].start_up_frame == "10"
    assert movelist[0].block_frame == "-5"
    assert movelist[0].hit_frame == "+5"
    assert movelist[0].counter_hit_frame == "+10"
    assert movelist[0].notes == "power crush"

# Тесты для utils.py
def test_load_page_success():
    with requests_mock.Mocker() as m:
        m.get("http://example.com", text="<html>Test</html>")
        result = load_page("http://example.com")
        assert result == "<html>Test</html>"

def test_load_page_failure():
    with requests_mock.Mocker() as m:
        m.get("http://example.com", status_code=404)
        with pytest.raises(Exception, match="Не удалось загрузить страницу: 404"):
            load_page("http://example.com")