CREATE TABLE [dbo].[Provinces] (
    [ID]               INT            IDENTITY (1, 1) NOT NULL,
    [NationID]         INT            NOT NULL,
    [Name]             NVARCHAR (100) NOT NULL,
    [Motto]            NVARCHAR (200) NULL,
    [LeaderResidentID] INT            NULL,
    CONSTRAINT [PK_Provinces] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_Provinces_LeaderResidentID_Residents_ID] FOREIGN KEY ([LeaderResidentID]) REFERENCES [dbo].[Residents] ([ID]),
    CONSTRAINT [FK_Provinces_NationID_Nations_ID] FOREIGN KEY ([NationID]) REFERENCES [dbo].[Nations] ([ID])
);

