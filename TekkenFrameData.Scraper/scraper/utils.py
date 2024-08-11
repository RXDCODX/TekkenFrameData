# scraper/utils.py
"""Загружает HTML-страницу по URL."""
import requests

def load_page(url):
    """Загружает HTML-страницу по URL."""
    response = requests.get(url)
    if response.status_code == 200:
        return response.text
    else:
        raise Exception(f"Не удалось загрузить страницу: {response.status_code}")