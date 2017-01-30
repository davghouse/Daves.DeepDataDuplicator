using Daves.DeepDataDuplicator.Metadata;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace Daves.DeepDataDuplicator.UnitTests
{
    [TestClass]
    public sealed class RootCopyGeneratorTests
    {
        [TestMethod]
        public void GenerateDefaultProcedure_ForRootedWorld()
        {
            string procedure = RootCopyGenerator.GenerateProcedure(
                SampleCatalogs.RootedWorld,
                SampleCatalogs.RootedWorld.FindTable("Nations"));

            Assert.AreEqual(
@"CREATE PROCEDURE [dbo].[CopyNation]
    @id INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @NationIDPairs TABLE (
        ExistingID INT NOT NULL UNIQUE,
        InsertedID INT NOT NULL UNIQUE
    );

    DECLARE @ProvinceIDPairs TABLE (
        ExistingID INT NOT NULL UNIQUE,
        InsertedID INT NOT NULL UNIQUE
    );

    MERGE INTO [dbo].[Nations] AS Target
    USING (
        SELECT *
        FROM [dbo].[Nations]
        WHERE [ID] = @id
    ) AS Source
    ON 1 = 0
    WHEN NOT MATCHED BY TARGET THEN
    INSERT (
        [Name],
        [DateFounded])
    VALUES (
        Source.[Name],
        Source.[DateFounded])
    OUTPUT Source.[ID], Inserted.[ID]
    INTO @NationIDPairs;

    MERGE INTO [dbo].[Provinces] AS Target
    USING (
        SELECT
            copy.*,
            j0.InsertedID j0InsertedID
        FROM [dbo].[Provinces] copy
        JOIN @NationIDPairs j0
            ON copy.[NationID] = j0.ExistingID
    ) AS Source
    ON 1 = 0
    WHEN NOT MATCHED BY TARGET THEN
    INSERT (
        [NationID],
        [Name],
        [TimeZone])
    VALUES (
        j0InsertedID,
        Source.[Name],
        Source.[TimeZone])
    OUTPUT Source.[ID], Inserted.[ID]
    INTO @ProvinceIDPairs;

    MERGE INTO [dbo].[Residents] AS Target
    USING (
        SELECT
            copy.*,
            j0.InsertedID j0InsertedID
        FROM [dbo].[Residents] copy
        JOIN @ProvinceIDPairs j0
            ON copy.[ProvinceID] = j0.ExistingID
    ) AS Source
    ON 1 = 0
    WHEN NOT MATCHED BY TARGET THEN
    INSERT (
        [ProvinceID])
    VALUES (
        j0InsertedID);
END;", procedure);
        }

        [TestMethod]
        public void GenerateDefaultProcedureBody_ForRootedWorld()
        {
            string procedureBody = RootCopyGenerator.GenerateProcedureBody(
                SampleCatalogs.RootedWorld,
                SampleCatalogs.RootedWorld.FindTable("Nations"));

            Assert.AreEqual(
@"
    DECLARE @NationIDPairs TABLE (
        ExistingID INT NOT NULL UNIQUE,
        InsertedID INT NOT NULL UNIQUE
    );

    DECLARE @ProvinceIDPairs TABLE (
        ExistingID INT NOT NULL UNIQUE,
        InsertedID INT NOT NULL UNIQUE
    );

    MERGE INTO [dbo].[Nations] AS Target
    USING (
        SELECT *
        FROM [dbo].[Nations]
        WHERE [ID] = @id
    ) AS Source
    ON 1 = 0
    WHEN NOT MATCHED BY TARGET THEN
    INSERT (
        [Name],
        [DateFounded])
    VALUES (
        Source.[Name],
        Source.[DateFounded])
    OUTPUT Source.[ID], Inserted.[ID]
    INTO @NationIDPairs;

    MERGE INTO [dbo].[Provinces] AS Target
    USING (
        SELECT
            copy.*,
            j0.InsertedID j0InsertedID
        FROM [dbo].[Provinces] copy
        JOIN @NationIDPairs j0
            ON copy.[NationID] = j0.ExistingID
    ) AS Source
    ON 1 = 0
    WHEN NOT MATCHED BY TARGET THEN
    INSERT (
        [NationID],
        [Name],
        [TimeZone])
    VALUES (
        j0InsertedID,
        Source.[Name],
        Source.[TimeZone])
    OUTPUT Source.[ID], Inserted.[ID]
    INTO @ProvinceIDPairs;

    MERGE INTO [dbo].[Residents] AS Target
    USING (
        SELECT
            copy.*,
            j0.InsertedID j0InsertedID
        FROM [dbo].[Residents] copy
        JOIN @ProvinceIDPairs j0
            ON copy.[ProvinceID] = j0.ExistingID
    ) AS Source
    ON 1 = 0
    WHEN NOT MATCHED BY TARGET THEN
    INSERT (
        [ProvinceID])
    VALUES (
        j0InsertedID);
", procedureBody);
        }

        [TestMethod]
        public void GenerateCustomizedProcedure_ForRootedWorld()
        {
            var updateParameters = new Dictionary<Column, Parameter>
            {
                { SampleCatalogs.RootedWorld.FindColumn("Provinces", "TimeZone"), new Parameter("@toTimeZone", "VARCHAR(50)")},
            };

            string procedure = RootCopyGenerator.GenerateProcedure(
                SampleCatalogs.RootedWorld,
                SampleCatalogs.RootedWorld.FindTable("Nations"),
                "RootCopyNation",
                "@fromNationID",
                updateParameters);

            Assert.AreEqual(
@"CREATE PROCEDURE [dbo].[RootCopyNation]
    @fromNationID INT,
    @toTimeZone VARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @NationIDPairs TABLE (
        ExistingID INT NOT NULL UNIQUE,
        InsertedID INT NOT NULL UNIQUE
    );

    DECLARE @ProvinceIDPairs TABLE (
        ExistingID INT NOT NULL UNIQUE,
        InsertedID INT NOT NULL UNIQUE
    );

    MERGE INTO [dbo].[Nations] AS Target
    USING (
        SELECT *
        FROM [dbo].[Nations]
        WHERE [ID] = @fromNationID
    ) AS Source
    ON 1 = 0
    WHEN NOT MATCHED BY TARGET THEN
    INSERT (
        [Name],
        [DateFounded])
    VALUES (
        Source.[Name],
        Source.[DateFounded])
    OUTPUT Source.[ID], Inserted.[ID]
    INTO @NationIDPairs;

    MERGE INTO [dbo].[Provinces] AS Target
    USING (
        SELECT
            copy.*,
            j0.InsertedID j0InsertedID
        FROM [dbo].[Provinces] copy
        JOIN @NationIDPairs j0
            ON copy.[NationID] = j0.ExistingID
    ) AS Source
    ON 1 = 0
    WHEN NOT MATCHED BY TARGET THEN
    INSERT (
        [NationID],
        [Name],
        [TimeZone])
    VALUES (
        j0InsertedID,
        Source.[Name],
        @toTimeZone)
    OUTPUT Source.[ID], Inserted.[ID]
    INTO @ProvinceIDPairs;

    MERGE INTO [dbo].[Residents] AS Target
    USING (
        SELECT
            copy.*,
            j0.InsertedID j0InsertedID
        FROM [dbo].[Residents] copy
        JOIN @ProvinceIDPairs j0
            ON copy.[ProvinceID] = j0.ExistingID
    ) AS Source
    ON 1 = 0
    WHEN NOT MATCHED BY TARGET THEN
    INSERT (
        [ProvinceID])
    VALUES (
        j0InsertedID);
END;", procedure);
        }

        [TestMethod]
        public void GenerateCustomizedProcedureBody_ForUnrootedWorld()
        {
            var updateParameters = new Dictionary<Column, Parameter>
            {
                { SampleCatalogs.RootedWorld.FindColumn("Provinces", "TimeZone"), new Parameter("@toTimeZone", "VARCHAR(50)")},
            };

            // Note it doesn't really make sense to use root copy on the unrooted world. Compare to the corresponding deep copy test.
            string procedure = RootCopyGenerator.GenerateProcedure(
                SampleCatalogs.UnrootedWorld,
                SampleCatalogs.UnrootedWorld.FindTable("Nations"),
                "RootCopyNation",
                "@fromNationID",
                updateParameters);

            Assert.AreEqual(
@"CREATE PROCEDURE [dbo].[RootCopyNation]
    @fromNationID INT,
    @toTimeZone VARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @NationIDPairs TABLE (
        ExistingID INT NOT NULL UNIQUE,
        InsertedID INT NOT NULL UNIQUE
    );

    DECLARE @ProvinceIDPairs TABLE (
        ExistingID INT NOT NULL UNIQUE,
        InsertedID INT NOT NULL UNIQUE
    );

    DECLARE @ResidentIDPairs TABLE (
        ExistingID INT NOT NULL UNIQUE,
        InsertedID INT NOT NULL UNIQUE
    );

    MERGE INTO [dbo].[Nations] AS Target
    USING (
        SELECT *
        FROM [dbo].[Nations]
        WHERE [ID] = @fromNationID
    ) AS Source
    ON 1 = 0
    WHEN NOT MATCHED BY TARGET THEN
    INSERT (
        [Name],
        [DateFounded])
    VALUES (
        Source.[Name],
        Source.[DateFounded])
    OUTPUT Source.[ID], Inserted.[ID]
    INTO @NationIDPairs;

    MERGE INTO [dbo].[Provinces] AS Target
    USING (
        SELECT
            copy.*,
            j0.InsertedID j0InsertedID
        FROM [dbo].[Provinces] copy
        JOIN @NationIDPairs j0
            ON copy.[NationID] = j0.ExistingID
    ) AS Source
    ON 1 = 0
    WHEN NOT MATCHED BY TARGET THEN
    INSERT (
        [NationID],
        [Name],
        [TimeZone],
        [LeaderResidentID])
    VALUES (
        j0InsertedID,
        Source.[Name],
        Source.[TimeZone],
        Source.[LeaderResidentID])
    OUTPUT Source.[ID], Inserted.[ID]
    INTO @ProvinceIDPairs;

    MERGE INTO [dbo].[Residents] AS Target
    USING (
        SELECT
            copy.*,
            j0.InsertedID j0InsertedID,
            j1.InsertedID j1InsertedID
        FROM [dbo].[Residents] copy
        JOIN @ProvinceIDPairs j0
            ON copy.[ProvinceID] = j0.ExistingID
        JOIN @NationIDPairs j1
            ON copy.[NationalityNationID] = j1.ExistingID
    ) AS Source
    ON 1 = 0
    WHEN NOT MATCHED BY TARGET THEN
    INSERT (
        [ProvinceID],
        [NationalityNationID],
        [SpouseResidentID],
        [FavoriteProvinceID])
    VALUES (
        j0InsertedID,
        j1InsertedID,
        Source.[SpouseResidentID],
        Source.[FavoriteProvinceID])
    OUTPUT Source.[ID], Inserted.[ID]
    INTO @ResidentIDPairs;

    UPDATE copy
    SET
        copy.[LeaderResidentID] = j0.InsertedID
    FROM [dbo].[Provinces] copy
    JOIN @ResidentIDPairs j0
        ON copy.[LeaderResidentID] = j0.ExistingID
    WHERE copy.[ID] IN (SELECT InsertedID FROM @ProvinceIDPairs);

    UPDATE copy
    SET
        copy.[SpouseResidentID] = j0.InsertedID,
        copy.[FavoriteProvinceID] = j1.InsertedID
    FROM [dbo].[Residents] copy
    LEFT JOIN @ResidentIDPairs j0
        ON copy.[SpouseResidentID] = j0.ExistingID
    LEFT JOIN @ProvinceIDPairs j1
        ON copy.[FavoriteProvinceID] = j1.ExistingID
    WHERE copy.[ID] IN (SELECT InsertedID FROM @ResidentIDPairs);
END;", procedure);
        }
    }
}
