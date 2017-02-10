SET IDENTITY_INSERT [dbo].[Residents] ON
INSERT INTO [dbo].[Residents] ([ID], [Name], [ProvinceID], [NationalityNationID], [SpouseResidentID], [FavoriteProvinceID]) VALUES (1, N'Liam', 1, 1, NULL, 3)
INSERT INTO [dbo].[Residents] ([ID], [Name], [ProvinceID], [NationalityNationID], [SpouseResidentID], [FavoriteProvinceID]) VALUES (2, N'Emma', 1, 2, NULL, 4)
INSERT INTO [dbo].[Residents] ([ID], [Name], [ProvinceID], [NationalityNationID], [SpouseResidentID], [FavoriteProvinceID]) VALUES (3, N'Jacob', 2, 1, 5, 11)
INSERT INTO [dbo].[Residents] ([ID], [Name], [ProvinceID], [NationalityNationID], [SpouseResidentID], [FavoriteProvinceID]) VALUES (4, N'Olivia', 2, 1, NULL, 13)
INSERT INTO [dbo].[Residents] ([ID], [Name], [ProvinceID], [NationalityNationID], [SpouseResidentID], [FavoriteProvinceID]) VALUES (5, N'William', 2, 2, 3, NULL)
SET IDENTITY_INSERT [dbo].[Residents] OFF
