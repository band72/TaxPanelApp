import json
from PIL import Image, ImageDraw, ImageFont

img_path = r"C:\Users\Daryl Banks\.gemini\antigravity\playground\ionic-helix\MapSearchApp\map.png"
img = Image.open(img_path)

left = 135  # Tweak?
top = 135
right = 80
bottom = 130

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
    # draw a bigger context red bounding box
    draw.rectangle([x, y, x + secW, y + secH], outline="red", width=2)
    
    # Save a larger context crop
    crop = img.crop((int(x)-100, int(y)-100, int(x+secW)+100, int(y+secH)+100))
    crop.save(rf"C:\Users\Daryl Banks\.gemini\antigravity\playground\ionic-helix\MapSearchApp\test_crop_large_{name}.png")

img.save(r"C:\Users\Daryl Banks\.gemini\antigravity\playground\ionic-helix\MapSearchApp\test_map_marked_large.png")
print("Saved larger marked map and crops.")
