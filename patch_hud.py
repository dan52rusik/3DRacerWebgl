import codecs

path = r"c:\MyGames\3DRacerWebgl\Assets\Scripts\Runtime\GlitchRacerHud.cs"
with open(path, 'r', encoding='utf-8-sig') as f:
    text = f.read()

text = text.replace("Screen.width", "vWidth").replace("Screen.height", "vHeight")

with open(path, 'w', encoding='utf-8') as f:
    f.write(text)
