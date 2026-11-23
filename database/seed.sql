USE PawShelterDB;
GO

IF NOT EXISTS (SELECT 1 FROM Species)
BEGIN
    INSERT INTO Species (Name) VALUES (N'Собака'), (N'Кошка'), (N'Кролик');

    INSERT INTO Enclosures (Code, Capacity, CurrentCount, Status) VALUES
    (N'В-101', 3, 2, N'Открыт'),
    (N'К-201', 4, 1, N'Открыт'),
    (N'Р-301', 2, 0, N'На ремонте');

    INSERT INTO Animals (Name, SpeciesId, EnclosureId, AgeYears, AdmissionDate, Status) VALUES
    (N'Барсик', 2, 2, 2.0, '2026-10-10', N'В приюте'),
    (N'Рекс', 1, 1, 4.5, '2026-10-12', N'В приюте'),
    (N'Луна', 1, 1, 1.0, '2026-10-20', N'На лечении');

    INSERT INTO Adopters (FullName, Phone, Email, IsBlacklisted) VALUES
    (N'Иванова Анна Сергеевна', N'+7-900-111-22-33', N'anna@mail.ru', 0),
    (N'Петров Дмитрий Олегович', N'+7-900-444-55-66', N'petrov@mail.ru', 1);

    INSERT INTO MedicalRecords (AnimalId, RecordDate, Diagnosis, VaccinationDone) VALUES
    (1, '2026-10-11', N'Здоров, плановый осмотр', 1),
    (2, '2026-10-13', N'Здоров, прививка от бешенства', 1),
    (3, '2026-10-21', N'Восстановление после травмы лапы', 0);
END
GO