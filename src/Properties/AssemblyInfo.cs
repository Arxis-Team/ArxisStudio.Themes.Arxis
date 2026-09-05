using Avalonia.Metadata;

// Тот же словарь разметки, что и у остальных библиотек ArxisStudio. Тему
// подключают одной строкой — <ArxisTheme/> в стилях приложения, — и ради этой
// строки объявлять отдельный псевдоним не за что.
[assembly: XmlnsDefinition("https://github.com/Arxis-Team/ArxisStudio", "ArxisStudio.Themes.Arxis")]

// Префикс, который предложит инструмент, когда адрес объявляют псевдонимом.
[assembly: XmlnsPrefix("https://github.com/Arxis-Team/ArxisStudio", "ax")]
