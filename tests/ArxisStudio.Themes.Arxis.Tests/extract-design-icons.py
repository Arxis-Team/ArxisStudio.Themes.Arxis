# -*- coding: utf-8 -*-
"""Извлекает из дизайн-проекта пути всех иконок в JSON.

Источник: design/ArxisStudio Design Project.dc.html, зона 30 — карточка каждой
иконки несёт свой путь и своё имя, набранное моношрифтом, тем же PascalCase,
что и в AxIcons.
"""
import io, json, re, sys, os

HANDOFF = r'C:\Users\Maxim\Desktop\ArxisStudio\design_handoff_arxis'

html = io.open(os.path.join(HANDOFF, 'design', 'ArxisStudio Design Project.dc.html'), encoding='utf-8').read()

icons = {}
for path, name in re.findall(
        r"""<path d="([^"]+)"[^>]*></path></svg><span style="font-family:'Cascadia Code'[^>]*>([A-Za-z0-9]+)</span>""",
        html):
    icons.setdefault(name, path)

print('иконок:', len(icons))

dest = sys.argv[1] if len(sys.argv) > 1 else 'design-icons.json'
io.open(dest, 'w', encoding='utf-8').write(
    json.dumps({'icons': icons}, ensure_ascii=False, indent=2, sort_keys=True))
print('записано в', dest)
