IF DB_ID(N'PawShelterDB') IS NULL
BEGIN
    CREATE DATABASE PawShelterDB;
END
GO

USE PawShelterDB;
GO

IF OBJECT_ID(N'dbo.Species', N'U') IS NULL
CREATE TABLE Species (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(50) NOT NULL UNIQUE
);

IF OBJECT_ID(N'dbo.Enclosures', N'U') IS NULL
CREATE TABLE Enclosures (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Code NVARCHAR(20) NOT NULL UNIQUE,
    Capacity INT NOT NULL CHECK (Capacity > 0),
    CurrentCount INT NOT NULL DEFAULT 0 CHECK (CurrentCount >= 0),
    Status NVARCHAR(20) NOT NULL DEFAULT N'Открыт'
        CHECK (Status IN (N'Открыт', N'На ремонте', N'Закрыт'))
);

IF OBJECT_ID(N'dbo.Animals', N'U') IS NULL
CREATE TABLE Animals (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(80) NOT NULL,
    SpeciesId INT NOT NULL REFERENCES Species(Id),
    EnclosureId INT NULL REFERENCES Enclosures(Id),
    AgeYears DECIMAL(4,1) NOT NULL CHECK (AgeYears >= 0),
    AdmissionDate DATE NOT NULL DEFAULT CAST(GETDATE() AS DATE),
    Status NVARCHAR(20) NOT NULL DEFAULT N'В приюте'
        CHECK (Status IN (N'В приюте', N'На лечении', N'Передан')),
    IsArchived BIT NOT NULL DEFAULT 0
);

IF OBJECT_ID(N'dbo.Adopters', N'U') IS NULL
CREATE TABLE Adopters (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    FullName NVARCHAR(120) NOT NULL,
    Phone NVARCHAR(30) NOT NULL,
    Email NVARCHAR(100) NULL,
    IsBlacklisted BIT NOT NULL DEFAULT 0
);

IF OBJECT_ID(N'dbo.MedicalRecords', N'U') IS NULL
CREATE TABLE MedicalRecords (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    AnimalId INT NOT NULL REFERENCES Animals(Id),
    RecordDate DATE NOT NULL DEFAULT CAST(GETDATE() AS DATE),
    Diagnosis NVARCHAR(200) NOT NULL,
    VaccinationDone BIT NOT NULL DEFAULT 0
);

IF OBJECT_ID(N'dbo.Adoptions', N'U') IS NULL
CREATE TABLE Adoptions (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    AnimalId INT NOT NULL REFERENCES Animals(Id),
    AdopterId INT NOT NULL REFERENCES Adopters(Id),
    AdoptionDate DATE NOT NULL DEFAULT CAST(GETDATE() AS DATE),
    Notes NVARCHAR(300) NULL
);
GO