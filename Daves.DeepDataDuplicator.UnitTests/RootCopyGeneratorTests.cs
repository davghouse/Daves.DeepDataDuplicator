using Microsoft.VisualStudio.TestTools.UnitTesting;

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

            Assert.AreEqual(procedure,
@"CREATE PROCEDURE [world].[CopyNation]
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

    MERGE INTO [world].[Nations] AS Target
    USING (
        SELECT *
        FROM [world].[Nations]
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

    MERGE INTO [world].[Provinces] AS Target
    USING (
        SELECT
            copy.*,
            j0.InsertedID j0InsertedID
        FROM [world].[Provinces] copy
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

    MERGE INTO [world].[Residents] AS Target
    USING (
        SELECT
            copy.*,
            j0.InsertedID j0InsertedID
        FROM [world].[Residents] copy
        JOIN @ProvinceIDPairs j0
            ON copy.[ProvinceID] = j0.ExistingID
    ) AS Source
    ON 1 = 0
    WHEN NOT MATCHED BY TARGET THEN
    INSERT (
        [ProvinceID])
    VALUES (
        j0InsertedID);
END;");
        }

        [TestMethod]
        public void GenerateDefaultProcedureBody_ForRootedWorld()
        {
            string procedureBody = RootCopyGenerator.GenerateProcedureBody(
                SampleCatalogs.RootedWorld,
                SampleCatalogs.RootedWorld.FindTable("Nations"));

            Assert.AreEqual(procedureBody,
@"    SET NOCOUNT ON;

    DECLARE @NationIDPairs TABLE (
        ExistingID INT NOT NULL UNIQUE,
        InsertedID INT NOT NULL UNIQUE
    );

    DECLARE @ProvinceIDPairs TABLE (
        ExistingID INT NOT NULL UNIQUE,
        InsertedID INT NOT NULL UNIQUE
    );

    MERGE INTO [world].[Nations] AS Target
    USING (
        SELECT *
        FROM [world].[Nations]
        WHERE [ID] = 
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

    MERGE INTO [world].[Provinces] AS Target
    USING (
        SELECT
            copy.*,
            j0.InsertedID j0InsertedID
        FROM [world].[Provinces] copy
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

    MERGE INTO [world].[Residents] AS Target
    USING (
        SELECT
            copy.*,
            j0.InsertedID j0InsertedID
        FROM [world].[Residents] copy
        JOIN @ProvinceIDPairs j0
            ON copy.[ProvinceID] = j0.ExistingID
    ) AS Source
    ON 1 = 0
    WHEN NOT MATCHED BY TARGET THEN
    INSERT (
        [ProvinceID])
    VALUES (
        j0InsertedID);
");
        }

        [TestMethod]
        public void GenerateCustomizedProcedure_ForRootedWorld()
        {

        }
    }
}
