ALTER TABLE dbo.Provinces NOCHECK CONSTRAINT FK_Provinces_LeaderResidentID_Residents_ID;
ALTER TABLE dbo.Residents NOCHECK CONSTRAINT FK_Residents_SpouseResidentID_Residents_ID;

:r ".\Data\Nations.data.sql"
:r ".\Data\Provinces.data.sql"
:r ".\Data\Residents.data.sql"

ALTER TABLE dbo.Provinces WITH CHECK CHECK CONSTRAINT FK_Provinces_LeaderResidentID_Residents_ID;
ALTER TABLE dbo.Residents WITH CHECK CHECK CONSTRAINT FK_Residents_SpouseResidentID_Residents_ID;