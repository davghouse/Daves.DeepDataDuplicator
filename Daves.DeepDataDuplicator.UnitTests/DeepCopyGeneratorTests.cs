﻿using Daves.DeepDataDuplicator.Metadata;
using Daves.DeepDataDuplicator.UnitTests.SampleCatalogs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace Daves.DeepDataDuplicator.UnitTests
{
    [TestClass]
    public sealed class DeepCopyGeneratorTests
    {
        [TestMethod]
        public void GenerateCustomizedProcedureBody_ForUnrootedWorld()
        {
            var updateParameters = new Dictionary<Column, Parameter>
            {
                { UnrootedWorld.Catalog.FindColumn("Provinces", "Motto"), new Parameter("@toMotto", "NVARCHAR(50)")},
            };

            string procedure = DeepCopyGenerator.GenerateProcedure(
                UnrootedWorld.Catalog,
                UnrootedWorld.Catalog.FindTable("Nations"),
                "DeepCopyNation",
                "@fromNationID",
                updateParameters);

            Assert.AreEqual(
@"CREATE PROCEDURE [dbo].[DeepCopyNation]
    @fromNationID INT,
    @toMotto NVARCHAR(50)
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
        [FoundedDate])
    VALUES (
        Source.[Name],
        Source.[FoundedDate])
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
        [Motto],
        [LeaderResidentID])
    VALUES (
        j0InsertedID,
        Source.[Name],
        @toMotto,
        Source.[LeaderResidentID])
    OUTPUT Source.[ID], Inserted.[ID]
    INTO @ProvinceIDPairs;

    MERGE INTO [dbo].[Residents] AS Target
    USING (
        SELECT
            copy.*,
            j0.InsertedID j0InsertedID,
            j1.InsertedID j1InsertedID
        FROM (
            SELECT *
            FROM [dbo].[Residents]
            WHERE [ProvinceID] IN (SELECT ExistingID FROM @ProvinceIDPairs)
                 OR [NationalityNationID] IN (SELECT ExistingID FROM @NationIDPairs)
        ) AS copy
        LEFT JOIN @ProvinceIDPairs j0
            ON copy.[ProvinceID] = j0.ExistingID
        LEFT JOIN @NationIDPairs j1
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
        COALESCE(j0InsertedID, [ProvinceID]),
        COALESCE(j1InsertedID, [NationalityNationID]),
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
        copy.[SpouseResidentID] = COALESCE(j0.InsertedID, copy.[SpouseResidentID]),
        copy.[FavoriteProvinceID] = COALESCE(j1.InsertedID, copy.[FavoriteProvinceID])
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
