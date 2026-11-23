# База данных PawShelterDB

Скрипты для создания и наполнения базы данных PawShelterDB в Microsoft SQL Server Express (localhost\SQLEXPRESS).

## Файлы папки
- schema.sql — создание базы данных PawShelterDB и 6 таблиц (Species, Enclosures, Animals, Adopters, MedicalRecords, Adoptions) с первичными/внешними ключами и проверками целостности.
- seed.sql — заполнение таблиц начальными тестовыми данными (виды животных, вольеры, животные, усыновители, медицинские записи).

## Порядок выполнения
1. Откройте schema.sql в SQL Server Management Studio (SSMS) (или выполните через sqlcmd) и запустите скрипт.
2. После создания структуры выполните seed.sql для добавления тестовых записей.
