# scraper/scraper.py
from datetime import datetime
from bs4 import BeautifulSoup
from .models import Character, Move
from .utils import load_page


class Scraper:
    """
    :param base_url: URL of the base page from which to scrape character information
    """
    def __init__(self, base_url):
        self.base_url = base_url

    def scrape_characters(self, html):
        """Парсит список персонажей из HTML."""
        soup = BeautifulSoup(html, 'html.parser')
        characters = []

        ul_node = soup.find('ul')
        if ul_node:   
            li_nodes = ul_node.find_all('li', class_='cursor-pointer')
            for li_node in li_nodes:
                a_node = li_node.find('a', class_='cursor-pointer')
                href = a_node['href'] if a_node else ""

                name_node = li_node.find('div', class_='overflow-hidden text-ellipsis whitespace-nowrap text-center capitalize text-text-primary max-xs:text-xs')
                name = name_node.text.strip() if name_node else ""

                # Получение текущей даты и времени
                now = datetime.now()

                # Вывод результата
                print(f"[{now}]: {name}")
                
                img_node = li_node.find('img')
                image_url = img_node['src'] if img_node else ""

                character = Character(name, image_url, href)
                if href:
                    character.movelist = self.scrape_movelist(character, self.base_url + href)
                characters.append(character)

        return characters

    def scrape_movelist(self, character, url):
        """Парсит список движений для персонажа."""
        html = load_page(url)
        soup = BeautifulSoup(html, 'html.parser')
        movelist = []

        table_node = soup.find('tbody')
        if table_node:
            row_nodes = table_node.find_all('tr', class_='rt-TableRow')
            for row_node in row_nodes:
                command_node = row_node.find('a', {'data-discover': 'true'})
                command = command_node.text.strip().lower() if command_node else ""
                if not command:
                    continue

                cell_nodes = row_node.find_all('td', class_='rt-TableCell')
                if len(cell_nodes) >= 8:
                    hit_level = cell_nodes[1].text.strip().lower()
                    damage = cell_nodes[2].text.strip().lower()
                    start_up_frame = cell_nodes[3].text.strip().lower()
                    block_frame = cell_nodes[4].text.strip().lower()
                    hit_frame = cell_nodes[5].text.strip().lower()
                    counter_hit_frame = cell_nodes[6].text.strip().lower()

                    note_divs = cell_nodes[7].find_all('div')
                    notes = "\n".join(div.text.strip().lower() for div in note_divs) if note_divs else ""

                    move = Move(
                        character.name,
                        command,
                        hit_level,
                        damage,
                        start_up_frame,
                        block_frame,
                        hit_frame,
                        counter_hit_frame,
                        notes
                    )
                    movelist.append(move)

        return movelist
