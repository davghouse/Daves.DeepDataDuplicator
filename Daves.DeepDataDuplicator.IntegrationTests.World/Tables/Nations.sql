CREATE TABLE [dbo].[Nations] (
    [ID]          INT           IDENTITY (1, 1) NOT NULL,
    [Name]        NVARCHAR (50) NOT NULL,
    [FoundedDate] DATE          NOT NULL,
    CONSTRAINT [PK_Nations] PRIMARY KEY CLUSTERED ([ID] ASC)
);

