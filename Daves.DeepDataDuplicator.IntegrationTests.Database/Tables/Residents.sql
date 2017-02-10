CREATE TABLE [dbo].[Residents] (
    [ID]                  INT            IDENTITY (1, 1) NOT NULL,
    [Name]                NVARCHAR (100) NOT NULL,
    [ProvinceID]          INT            NOT NULL,
    [NationalityNationID] INT            NOT NULL,
    [SpouseResidentID]    INT            NULL,
    [FavoriteProvinceID]  INT            NULL,
    CONSTRAINT [PK_Residents] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_Residents_FavoriteProvinceID_Provinces_ID] FOREIGN KEY ([FavoriteProvinceID]) REFERENCES [dbo].[Provinces] ([ID]),
    CONSTRAINT [FK_Residents_NationalityNationID_Nations_ID] FOREIGN KEY ([NationalityNationID]) REFERENCES [dbo].[Nations] ([ID]),
    CONSTRAINT [FK_Residents_ProvinceID_Provinces_ID] FOREIGN KEY ([ProvinceID]) REFERENCES [dbo].[Provinces] ([ID]),
    CONSTRAINT [FK_Residents_SpouseResidentID_Residents_ID] FOREIGN KEY ([SpouseResidentID]) REFERENCES [dbo].[Residents] ([ID])
);

