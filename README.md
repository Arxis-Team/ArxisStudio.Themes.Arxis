# ArxisStudio.Themes.Arxis

Тема «Arxis» — внешний вид ArxisStudio. Аналог `Avalonia.Themes.Fluent` для пары
[`ArxisStudio.Controls`](../ArxisStudio.Controls/): ControlTheme-шаблоны всех
Ax\*-контролов и палитры **Dark / Light** по дизайн-токенам студии
(дизайн-проект `design_handoff_arxis`: токены — «10 Основы», контролы и
состояния — «20 Контролы», критерии приёмки — «40 Приёмка»).

## Подключение

В M0 тема подключается поверх `FluentTheme` — базового слоя для не-Ax примитивов
(окно, попапы, тултипы); приложение регистрирует шрифт Inter:

```csharp
AppBuilder.Configure<App>().UsePlatformDetect().WithInterFont();
```

```xml
<Application.Styles>
    <FluentTheme/>
    <themes:ArxisTheme/>
</Application.Styles>
```

Вариант темы переключается штатно: `Application.RequestedThemeVariant =
ThemeVariant.Dark / Light` — все токены объявлены в theme dictionaries и
меняются динамически.

## Откуда значения

Значения приходят из дизайн-проекта студии, а он опирается на Int UI —
дизайн-систему IntelliJ. Ключевое: строка списка 24, контур фокуса 2px,
скругление 4, кнопка 28 высотой с отступами 12×6. Правило одно: значение цвета
или размера объявляется здесь и больше нигде — в шаблонах, разметке и коде
панелей их быть не должно.

## Содержимое

- `Palette.axaml` — восемь шкал (`AxGray1…14`, `AxBlue1…13`, `AxGreen*`,
  `AxRed*`, `AxYellow*`, `AxOrange*`, `AxPurple*`, `AxTeal*`) и семантические
  токены поверх них × 2 варианта: фоны `AxBg1…AxBg4`, рамки `AxBrd/AxBrd2`,
  текст `AxFg/AxFg2/AxFg3/AxFgDisabled`, акцент `AxAcc/AxAccHover/AxAccPressed`
  и `AxAccStrong/AxAccStrongHover` для залитых поверхностей с текстом, `AxOnAcc`,
  выделение `AxSel/AxSelInactive`, холст `AxCanvas/AxDot`, поле `AxInp`,
  смысловые `AxGrn/AxRed/AxYel/AxOrg/AxPur` и отдельно их текстовые пары
  `AxGreenText/AxRedText/AxYellowText`, ссылки `AxLink*` вместе с `AxLinkOn`,
  фоны и рамки сообщений, подсветка кода `AxCode*`, тени. Каждый — как `*Color`
  и `*Brush`; кисти объявлены внутри словарей вариантов, поэтому вложенный
  `ThemeVariantScope` перекрашивается вместе с ними.
- `Typography.axaml` — `AxFontFamily` (Inter), `AxFontFamilyMono` (Fira Code,
  едет в сборке темы; системные — запасными), кегли 13 / 11.5 / 10.5 и
  заголовочные 14 / 15.
- `Metrics.axaml` — размеры: `AxControlHeight` и `AxControlHeightCompact`,
  `AxRowHeight`, `AxFocusOutlineWidth`, радиусы 4 / 8 / 3, отступы кнопки, поля и
  комбобокса, размер флажка и тумблера, размер и обводка иконки.
- `ControlThemes/` — темы всех Ax\*-контролов, меню и контекстных меню, тултипа,
  тонких скроллбаров (8px) ключом `{x:Type ScrollBar}` для всего приложения.
- Глобальные стили: база окна (фон, цвет, шрифт), задержка тултипа и текстовые
  роли `TextBlock.mono / .dim / .dimmer / .section` плюс размерные
  `.small / .caption / .title`.

## Шрифты в поставке

Fira Code (Regular, Medium, SemiBold, Bold) лежит в `src/Fonts` и включается в
сборку ресурсом: моноширинный шрифт студии не зависит от того, что установлено в
системе. Лицензия SIL OFL 1.1 — рядом с файлами.

## Тесты

`tests/ArxisStudio.Themes.Arxis.Tests` — headless-контракт темы: каждый
Ax\*-контрол получает шаблон в обоих вариантах и не теряет его при переключении
`RequestedThemeVariant` у живого окна; токены приёмки существуют в обеих темах;
метрики тумблера и иконки отдаёт тема, а не шаблон; моноширинный стек начинается
с Fira Code; длины раскладки чётные, а нечётные исключения спецификации названы
поимённо.

```bash
dotnet test
```

Репозиторий ожидает `ArxisStudio.Controls` рядом с собой (sibling checkout);
сборка честно скажет об этом, если его нет.
