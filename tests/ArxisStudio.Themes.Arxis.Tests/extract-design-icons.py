# -*- coding: utf-8 -*-
"""Извлекает из дизайн-проекта пути всех иконок в JSON.

Источник: design/30 Icons.dc.html — карточка каждой иконки несёт свой путь
и своё имя, набранное Fira Code, ровно тем же PascalCase, что и в AxIcons.
"""
import io, json, re, sys, os

HANDOFF = r'C:\Users\Maxim\Desktop\ArxisStudio\design_handoff_arxis'

html = io.open(os.path.join(HANDOFF, 'design', '30 Icons.dc.html'), encoding='utf-8').read()

icons = {}
for path, name in re.findall(
        r"""<path d="([^"]+)"[^>]*></path></svg><span style="font-family:'Fira Code'[^>]*>([A-Za-z0-9]+)</span>""",
        html):
    icons.setdefault(name, path)

print('иконок:', len(icons))

dest = sys.argv[1] if len(sys.argv) > 1 else 'design-icons.json'
io.open(dest, 'w', encoding='utf-8').write(
    json.dumps({'icons': icons}, ensure_ascii=False, indent=2, sort_keys=True))
print('записано в', dest)
