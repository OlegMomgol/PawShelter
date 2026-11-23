using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;

namespace PawShelter
{
    public class AnimalRow
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string SpeciesName { get; set; } = string.Empty;
        public string EnclosureCode { get; set; } = "Без вольера";
        public decimal AgeYears { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class SpeciesItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class EnclosureItem
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
    }

    public class DatabaseService
    {
        
        private const string ConnectionString =
            "Server=localhost\\SQLEXPRESS;Database=PawShelterDB;Trusted_Connection=True;TrustServerCertificate=True;";

        public List<SpeciesItem> GetSpecies()
        {
            var result = new List<SpeciesItem>();
            using var conn = new SqlConnection(ConnectionString);
            conn.Open();

            using var cmd = new SqlCommand("SELECT Id, Name FROM Species ORDER BY Name;", conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Add(new SpeciesItem
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1)
                });
            }
            return result;


        }

        public List<EnclosureItem> GetOpenEnclosures()
        {
            var result = new List<EnclosureItem>();
            using var conn = new SqlConnection(ConnectionString);
            conn.Open();

            const string sql = @"
                SELECT Id, Code
                FROM Enclosures
                WHERE Status = N'Открыт' AND CurrentCount < Capacity
                ORDER BY Code;";

            using var cmd = new SqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Add(new EnclosureItem
                {
                    Id = reader.GetInt32(0),
                    Code = reader.GetString(1)
                });
            }
            return result;
        }

        public List<AnimalRow> GetAnimals(string searchText, int? speciesId, string? status)
        {
            var result = new List<AnimalRow>();
            using var conn = new SqlConnection(ConnectionString);
            conn.Open();

            const string sql = @"
                SELECT
                    a.Id,
                    a.Name,
                    s.Name AS SpeciesName,
                    ISNULL(e.Code, N'Без вольера') AS EnclosureCode,
                    a.AgeYears,
                    a.Status
                FROM Animals a
                INNER JOIN Species s ON a.SpeciesId = s.Id
                LEFT JOIN Enclosures e ON a.EnclosureId = e.Id
                WHERE a.IsArchived = 0
                  AND (@Search = N'' OR a.Name LIKE N'%' + @Search + N'%')
                  AND (@SpeciesId IS NULL OR a.SpeciesId = @SpeciesId)
                  AND (@Status IS NULL OR @Status = N'Все' OR a.Status = @Status)
                ORDER BY a.Id DESC;";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Search", (searchText ?? string.Empty).Trim());
            cmd.Parameters.AddWithValue("@SpeciesId", (object?)speciesId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Status", (object?)status ?? "Все");

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Add(new AnimalRow
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    SpeciesName = reader.GetString(2),
                    EnclosureCode = reader.GetString(3),
                    AgeYears = reader.GetDecimal(4),
                    Status = reader.GetString(5)
                });
            }
            return result;
        }

       
        public void AddAnimalWithEnclosure(string name, int speciesId, int? enclosureId, decimal ageYears)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Введите кличку животного.");
            if (ageYears < 0)
                throw new ArgumentException("Возраст не может быть отрицательным.");

            using var conn = new SqlConnection(ConnectionString);
            conn.Open();
            using var tx = conn.BeginTransaction();

            try
            {
                if (enclosureId.HasValue)
                {
                    const string checkEnclosureSql = @"
                        SELECT Capacity, CurrentCount, Status
                        FROM Enclosures
                        WHERE Id = @EnclosureId;";

                    int capacity = 0;
                    int currentCount = 0;
                    string status = string.Empty;

                    using (var checkCmd = new SqlCommand(checkEnclosureSql, conn, tx))
                    {
                        checkCmd.Parameters.AddWithValue("@EnclosureId", enclosureId.Value);
                        using var reader = checkCmd.ExecuteReader();
                        if (!reader.Read())
                            throw new InvalidOperationException("Выбранный вольер не найден.");

                        capacity = reader.GetInt32(0);
                        currentCount = reader.GetInt32(1);
                        status = reader.GetString(2);
                    }

                    if (status != "Открыт")
                        throw new InvalidOperationException($"Нельзя заселить в вольер со статусом «{status}».");

                    if (currentCount >= capacity)
                        throw new InvalidOperationException("В выбранном вольере нет свободных мест.");

                    const string updateEnclosureSql = @"
                        UPDATE Enclosures
                        SET CurrentCount = CurrentCount + 1
                        WHERE Id = @EnclosureId;";

                    using var updCmd = new SqlCommand(updateEnclosureSql, conn, tx);
                    updCmd.Parameters.AddWithValue("@EnclosureId", enclosureId.Value);
                    updCmd.ExecuteNonQuery();
                }

                const string insertAnimalSql = @"
                    INSERT INTO Animals (Name, SpeciesId, EnclosureId, AgeYears, Status, IsArchived)
                    VALUES (@Name, @SpeciesId, @EnclosureId, @AgeYears, N'В приюте', 0);";

                using (var insCmd = new SqlCommand(insertAnimalSql, conn, tx))
                {
                    insCmd.Parameters.AddWithValue("@Name", name.Trim());
                    insCmd.Parameters.AddWithValue("@SpeciesId", speciesId);
                    insCmd.Parameters.AddWithValue("@EnclosureId", (object?)enclosureId ?? DBNull.Value);
                    insCmd.Parameters.AddWithValue("@AgeYears", ageYears);
                    insCmd.ExecuteNonQuery();
                }

                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }
        public void ChangeAnimalStatus(int animalId, string newStatus)
        {
            if (string.IsNullOrWhiteSpace(newStatus))
                throw new ArgumentException("Выберите статус.");

            using var conn = new SqlConnection(ConnectionString);
            conn.Open();
            using var tx = conn.BeginTransaction();

            try
            {
                const string getSql = @"
                    SELECT Status, EnclosureId
                    FROM Animals
                    WHERE Id = @Id AND IsArchived = 0;";

                string currentStatus = string.Empty;
                int? enclosureId = null;

                using (var getCmd = new SqlCommand(getSql, conn, tx))
                {
                    getCmd.Parameters.AddWithValue("@Id", animalId);
                    using var reader = getCmd.ExecuteReader();
                    if (!reader.Read())
                        throw new InvalidOperationException("Животное не найдено или находится в архиве.");

                    currentStatus = reader.GetString(0);
                    enclosureId = reader.IsDBNull(1) ? null : reader.GetInt32(1);
                }

                if (currentStatus == "Передан" && newStatus != "Передан")
                    throw new InvalidOperationException("Нельзя вернуть обратно статус уже переданного животного.");

                // Если животное передаётся новому хозяину и было в вольере — освобождаем место
                if (currentStatus != "Передан" && newStatus == "Передан" && enclosureId.HasValue)
                {
                    const string freePlaceSql = @"
                        UPDATE Enclosures
                        SET CurrentCount = CASE WHEN CurrentCount > 0 THEN CurrentCount - 1 ELSE 0 END
                        WHERE Id = @EnclosureId;";

                    using var freeCmd = new SqlCommand(freePlaceSql, conn, tx);
                    freeCmd.Parameters.AddWithValue("@EnclosureId", enclosureId.Value);
                    freeCmd.ExecuteNonQuery();

                    const string updWithReleaseSql = @"
                        UPDATE Animals
                        SET Status = @Status, EnclosureId = NULL
                        WHERE Id = @Id;";

                    using var updCmd = new SqlCommand(updWithReleaseSql, conn, tx);
                    updCmd.Parameters.AddWithValue("@Status", newStatus);
                    updCmd.Parameters.AddWithValue("@Id", animalId);
                    updCmd.ExecuteNonQuery();
                }
                else
                {
                    const string updSql = @"
                        UPDATE Animals
                        SET Status = @Status
                        WHERE Id = @Id;";

                    using var updCmd = new SqlCommand(updSql, conn, tx);
                    updCmd.Parameters.AddWithValue("@Status", newStatus);
                    updCmd.Parameters.AddWithValue("@Id", animalId);
                    updCmd.ExecuteNonQuery();
                }

                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public void ArchiveAnimal(int animalId)
        {
            using var conn = new SqlConnection(ConnectionString);
            conn.Open();
            using var tx = conn.BeginTransaction();

            try
            {
                const string selectSql = @"
                    SELECT EnclosureId, Status
                    FROM Animals
                    WHERE Id = @Id AND IsArchived = 0;";

                int? enclosureId = null;
                string status = string.Empty;

                using (var selCmd = new SqlCommand(selectSql, conn, tx))
                {
                    selCmd.Parameters.AddWithValue("@Id", animalId);
                    using var reader = selCmd.ExecuteReader();
                    if (!reader.Read())
                        throw new InvalidOperationException("Запись уже в архиве или не найдена.");

                    enclosureId = reader.IsDBNull(0) ? null : reader.GetInt32(0);
                    status = reader.GetString(1);
                }

                if (enclosureId.HasValue && status != "Передан")
                {
                    const string decSql = @"
                        UPDATE Enclosures
                        SET CurrentCount = CASE WHEN CurrentCount > 0 THEN CurrentCount - 1 ELSE 0 END
                        WHERE Id = @EnclosureId;";

                    using var decCmd = new SqlCommand(decSql, conn, tx);
                    decCmd.Parameters.AddWithValue("@EnclosureId", enclosureId.Value);
                    decCmd.ExecuteNonQuery();
                }

                const string archiveSql = @"
                    UPDATE Animals
                    SET IsArchived = 1, EnclosureId = NULL
                    WHERE Id = @Id;";

                using var archCmd = new SqlCommand(archiveSql, conn, tx);
                archCmd.Parameters.AddWithValue("@Id", animalId);
                archCmd.ExecuteNonQuery();

                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public string GetSummaryReport()
        {
            using var conn = new SqlConnection(ConnectionString);
            conn.Open();

            var lines = new List<string>();
            lines.Add("=== СВОДКА ПО ЖИВОТНЫМ (ПО ВИДАМ И СТАТУСАМ) ===");

            const string bySpeciesSql = @"
                SELECT s.Name, a.Status, COUNT(*) AS TotalCount
                FROM Animals a
                INNER JOIN Species s ON a.SpeciesId = s.Id
                WHERE a.IsArchived = 0
                GROUP BY s.Name, a.Status
                ORDER BY s.Name, a.Status;";

            using (var cmd = new SqlCommand(bySpeciesSql, conn))
            using (var reader = cmd.ExecuteReader())
            {
                bool any = false;
                while (reader.Read())
                {
                    any = true;
                    lines.Add($"• {reader.GetString(0)} | Статус: {reader.GetString(1)} — {reader.GetInt32(2)} шт.");
                }
                if (!any) lines.Add("• Нет активных записей.");
            }

            lines.Add("");
            lines.Add("=== ВОЛЬЕРЫ (ЗАНЯТОСТЬ И СВОБОДНЫЕ МЕСТА) ===");

            const string enclosuresSql = @"
                SELECT Code, Status, CurrentCount, Capacity, (Capacity - CurrentCount) AS FreePlaces
                FROM Enclosures
                ORDER BY Code;";

            using (var cmd = new SqlCommand(enclosuresSql, conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    lines.Add($"• {reader.GetString(0)} ({reader.GetString(1)}): {reader.GetInt32(2)}/{reader.GetInt32(3)}, свободно: {reader.GetInt32(4)}");
                }
            }

            return string.Join(Environment.NewLine, lines);
        }
    }

}