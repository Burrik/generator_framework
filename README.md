# Система процедурной генерации

Фреймворк для создания гибких генераторов контента в Unity с поддержкой многослойной генерации, валидации данных и асинхронного выполнения.

## Описание

Фреймворк предоставляет гибкую архитектуру для создания генераторов различного контента:
- Многослойная система генерации
- Асинхронное выполнение через UniTask
- Отслеживание прогресса с отображением в редакторе
- Поддержка отмены операций
- Валидация данных и зависимостей
- Расширяемая архитектура

## Обзор компонентов

### Генератор
- BaseGenerator - базовый класс для всех генераторов
- BaseProcessGenerator - базовый класс процессов генерации 

### Слои и процессы
- ProcessLayer - базовый класс для слоев генерации
- LayersContainer - контейнер для управления слоями
- LayerContext - контекст выполнения слоя

### Данные
- GeneratorData - контейнер данных генерации
- IGeneratorData - интерфейс для данных

## Структура проекта
Assets/
└── Scripts/
└── Generate/
└── Core/
├── Attributes/ # Атрибуты для настройки компонентов
├── Data/ # Система хранения данных
├── Editor/ # Редакторные расширения Unity
├── Generator/ # Базовые классы генераторов
├── Layers/ # Система слоев
├── Template/ # Примеры использования
├── Utils/ # Вспомогательные классы
└── README.md # Документация фреймворка

## Примечания

- Требуется Unity 2021.3 или новее
- Используется UniTask для асинхронности
- Есть примеры использования