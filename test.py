import json
from PIL import Image, ImageDraw, ImageFont

img_path = r"C:\Users\Daryl Banks\.gemini\antigravity\playground\ionic-helix\MapSearchApp\map.png"
img = Image.open(img_path)

left = 148
top = 145
right = 90
bottom = 140

gridWidth = 1024 - left - right
gridHeight = 1024 - top - bottom

secW = gridWidth / 42.0
secH = gridHeight / 36.0

draw = ImageDraw.Draw(img)

test_cases = [
    ("6307", 0, 31),
    ("6418", 6, 32),
    ("5319", 0, 27),
]

for name, col, row in test_cases:
    x = left + col * secW
    y = top + row * secH
    # draw a red bounding box
    draw.rectangle([x, y, x + secW, y + secH], outline="red", width=2)
    
    # Save a crop of the bounding box
    crop = img.crop((int(x)-10, int(y)-10, int(x+secW)+10, int(y+secH)+10))
    crop.save(rf"C:\Users\Daryl Banks\.gemini\antigravity\playground\ionic-helix\MapSearchApp\test_crop_{name}.png")

img.save(r"C:\Users\Daryl Banks\.gemini\antigravity\playground\ionic-helix\MapSearchApp\test_map_marked.png")
print("Saved marked map and crops.")
