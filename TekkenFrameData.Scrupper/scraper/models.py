# scraper/models.py

class Character:
    def __init__(self, name, image_url, href):
        self.name = name
        self.image_url = image_url
        self.href = href
        self.movelist = []

    def __repr__(self):
        return f"Character(name={self.name}, image_url={self.image_url}, movelist={len(self.movelist)} moves)"


class Move:
    def __init__(self, character_name, command, hit_level, damage, start_up_frame, block_frame, hit_frame, counter_hit_frame, notes):
        self.character_name = character_name
        self.command = command
        self.hit_level = hit_level
        self.damage = damage
        self.start_up_frame = start_up_frame
        self.block_frame = block_frame
        self.hit_frame = hit_frame
        self.counter_hit_frame = counter_hit_frame
        self.notes = notes
        self.power_crush = 'power crush' in notes.lower()
        self.heat_burst = 'heat burst' in notes.lower()
        self.heat_engage = 'heat engager' in notes.lower()
        self.heat_smash = 'heat smash' in notes.lower()
        self.requires_heat = command.startswith('h')
        self.tornado = 'tornado' in notes.lower()
        self.homing = 'homing' in notes.lower()
        self.throw = 'th' in hit_level.lower() or 't' in hit_level.lower()

    def __repr__(self):
        return f"Move(character={self.character_name}, command={self.command})"