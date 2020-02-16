

create table [dbo].[Notification](
	NotificationId int identity(1,1) primary key,
	Message nvarchar(MAX) NOT NULL,
	state nvarchar(20) NOT NULL,
	AddedOn datetime NULL
);

create table [dbo].[User](
	UserId int identity(1,1) primary key,
	StudentId int NULL foreign key references [dbo].[Student](StudentId),
	TeacherId int NULL foreign key references [dbo].[Teacher](TeacherId),
	Email nvarchar(max) NOT NULL,
	Password nvarchar(max) NOT NULL
);

Create table [dbo].[Activity_Suggested_Date](
	Suggested_DateId int identity(1,1) primary key NOT NULL,
	ActivityId int not null,
	UserId int not null,
	Suggested_Date Date not null
)

create table [dbo].[UserGuide](
	UserGuideId int identity(1,1) primary key,
	Message nvarchar(max) NOT NULL,
	CreatedOn datetime NOT NULL
);

create table [dbo].[Students_Teachers](
	Students_TeachersId int identity(1,1) primary key,
	StudentId int NOT NULL foreign key references [dbo].[Student](StudentId),
	TeacherId int NOT NULL foreign key references [dbo].[Teacher](TeacherId)
);

create table [dbo].[WorkPlan](
	PlanId int identity(1,1) primary key,
	PlanFile varchar(max),
	TfcId int foreign key references [dbo].[Tfc](TfcId)
);

create table [dbo].[Tfc](
	TfcId int identity(1,1) primary key,
	TfcType bit not null,
	TfcFile nvarchar(max)
);

create table [dbo].[InternshipContract](
	ContractId int identity(1,1) primary key,
	ContractFile nvarchar(max),
	ToId int foreign key references [dbo].[TO](TOId)
);

create table [dbo].[TO](
	TOId int identity(1,1) primary key,
	FirstName nvarchar(max),
	LastName nvarchar(max)
);

create table [dbo].[ProposalDO](
	ProposalId int identity(1,1) primary key,
	TeacherIdFk int NOT NULL, 
	StudentNumber int NOT NULL
);

create table [dbo].[TfcProposal](
	TfcProposalId int identity(1,1) primary key,
	StudentNumber int NOT NULL,
	TfcId int
);

drop table ProposalDO

alter table [dbo].[Tfc] alter column [location] nvarchar(max)

insert into [dbo].[Students_Teachers](StudentId, TeacherId) values(5, 2)

Insert into [dbo].[UserGuide] (Message, CreatedOn) values ('Para registro de utilizadores o RUC pode aceder ao seu perfil, e inserir então um ficheiro excel com as seguintes informações, a partir da 2ª linha:
- Primeiro nome do aluno na primeira coluna, ultímo nome na segunda coluna, e-mail na terceira coluna, número de estudante na quarta coluna e área do curso na quinta coluna.
Será então enviado um e-mail com as credencais para cada aluno inserido, onde estes poderão então efetuar o login no nosso site.
O aluno quando já está autenticado pode pesquisar por outros utilizadores no sistema, filtrando-os pelo número de aluno. Pode também recuperar a sua password, alterando-a no painel de login.', GETDATE());

alter table [dbo].[WorkPlan] add constraint [FK_ContractId] foreign key (ContractId) references InternshipContract(ContractId) 
GO

ALTER TABLE [dbo].[WorkPlan] ALTER COLUMN Confirmed INT NOT NULL

alter table [dbo].[WorkPlan] add [TfcType] nvarchar(max)

alter table [dbo].[Tfc] add name nvarchar(25), details nvarchar(max), company nvarchar(25), location nvarchar(25)
GO

ALTER TABLE Orders
ADD CONSTRAINT FK_PersonOrder
FOREIGN KEY (PersonID) REFERENCES Persons(PersonID);

Select Email
from [dbo].[User] as u
join [dbo].[Notification] as n
on u.UserId = n.UserId
where n.NotificationId in (Select TOP 1 NotificationId
						   FROM [dbo].Notification 
						   ORDER BY NotificationId DESC)

delete from  [dbo].[User]
delete from  [dbo].[Student]
select * from [dbo].[User]
select * from [dbo].[Student]
select * from [dbo].[Teacher]
select * from [dbo].[Notification]
select * from [dbo].Students_Teachers
select * from [dbo].WorkPlan
select * from [dbo].ProposalDO


Insert into [dbo].[User](Email, TeacherId, Password) values('bruno.goonie@gmail.com', 4, 'same')

delete from [dbo].WorkPlan 

alter table WorkPlan alter column ContractId 

Select * From Teacher as t join Students_Teachers as st on t.TeacherId = st.TeacherIdFk where st.StudentIdFk = 6

insert into [dbo].[Students_Teachers] (StudentIdFk, TeacherIdFk) values (7, 3)

Insert into [dbo].Notification (Message, state, AddedOn, UserId) values('Nova Notificação para mufino' , 'Fechada', GETDATE(), 5)

select * from Notification

Select * from notification;

ALTER TABLE [dbo].[Notification]
ADD CONSTRAINT default_Date
DEFAULT GETDATE() FOR AddedOn;

ALTER TABLE [dbo].[Notification] DROP COLUMN ReadNotification

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[NotificationLog](
	Id INT PRIMARY KEY IDENTITY(1,1) NOT NULL,
	ChangeDate DATETIME DEFAULT GETDATE() NOT NULL,
	Command NCHAR(6) NOT NULL,
	OldMessage NVARCHAR(MAX) NULL,
	NewMessage NVARCHAR(MAX) NULL,
	OldState NVARCHAR(20) NULL,
	NewState NVARCHAR(20) NULL,
	OldDate DATETIME2(7) NULL,
	NewDate DATETIME2(7) NULL,
)
GO

alter table [dbo].[NotificationLog]
drop column OldDate

alter table [dbo].[NotificationLog]
drop column NewDate;

create table [dbo].[Ata](
	AtaId INT PRIMARY KEY IDENTITY(1,1) NOT NULL,
	StudentId INT NOT NULL,
	FilePath NVARCHAR(MAX) NOT NULL,
	MettingDate DATETIME2 NOT NULL
)

CREATE TRIGGER notification_change
ON [dbo].[Notification]
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
	DECLARE @operation CHAR(6)
		SET @operation = CASE
				WHEN EXISTS(SELECT * FROM inserted) AND EXISTS(SELECT * FROM deleted)
					THEN 'Update'
				WHEN EXISTS(SELECT * FROM inserted)
					THEN 'Insert'
				WHEN EXISTS(SELECT * FROM deleted)
					THEN 'Delete'
				ELSE NULL
		END
	IF @operation = 'Delete'
			INSERT INTO [dbo].[NotificationLog] (Command, ChangeDate, OldMessage, OldState)
			SELECT @operation, GETDATE(),  d.Message, d.state
			FROM deleted d
 
	IF @operation = 'Insert'
			INSERT INTO [dbo].[NotificationLog] (Command, ChangeDate, NewMessage, NewState)
			SELECT @operation, GETDATE(), i.Message, i.state
			FROM inserted i
 
	IF @operation = 'Update'
			INSERT INTO [dbo].[NotificationLog] (Command, ChangeDate, NewMessage, OldMessage, NewState, OldState)
			SELECT @operation, GETDATE(), d.Message, i.Message, d.state, i.state
			FROM deleted d, inserted i
END
GO

CREATE TRIGGER UpdateWhenNumberEqual ON [dbo].[Student]
INSTEAD OF INSERT
AS
if exists (select * from [dbo].[Student] c 
                    inner join inserted i 
                        on c.StudentNumber = i.StudentNumber 
                        and c.StudentId <> i.StudentId)
begin
	DECLARE @New_FirstName nvarchar(max)
	SET @New_FirstName = (select i.FirstName from [dbo].[Student] c 
                    inner join inserted i 
                        on c.StudentNumber = i.StudentNumber 
                        and c.StudentId <> i.StudentId)

	DECLARE @New_LastName nvarchar(max)
	SET @New_LastName = (select i.LastName from [dbo].[Student] c 
                    inner join inserted i 
                        on c.StudentNumber = i.StudentNumber 
                        and c.StudentId <> i.StudentId)

	DECLARE @New_TFC_Id nvarchar(max)
	SET @New_TFC_Id = (select i.TFC_Id from [dbo].[Student] c 
                    inner join inserted i 
                        on c.StudentNumber = i.StudentNumber 
                        and c.StudentId <> i.StudentId)

	DECLARE @New_TFC_Type nvarchar(max)
	SET @New_TFC_Type = (select i.TFC_Type from [dbo].[Student] c 
                    inner join inserted i 
                        on c.StudentNumber = i.StudentNumber 
                        and c.StudentId <> i.StudentId)

	DECLARE @New_TFC_Proposal nvarchar(max)
	SET @New_TFC_Proposal = (select i.TFC_Proposal from [dbo].[Student] c 
                    inner join inserted i 
                        on c.StudentNumber = i.StudentNumber 
                        and c.StudentId <> i.StudentId)

	DECLARE @New_Progress nvarchar(max)
	SET @New_Progress = (select i.Progress from [dbo].[Student] c 
                    inner join inserted i 
                        on c.StudentNumber = i.StudentNumber 
                        and c.StudentId <> i.StudentId)

	DECLARE @New_Email nvarchar(max)
	SET @New_Email = (select i.Email from [dbo].[Student] c 
                    inner join inserted i 
                        on c.StudentNumber = i.StudentNumber 
                        and c.StudentId <> i.StudentId)

	DECLARE @New_StudentNumber int
	SET @New_StudentNumber = (select i.StudentNumber from [dbo].[Student] c 
                    inner join inserted i 
                        on c.StudentNumber = i.StudentNumber 
                        and c.StudentId <> i.StudentId)

    UPDATE [dbo].[Student] SET FirstName = @New_FirstName, LastName = @New_LastName, Email = @New_Email, TFC_Id = @New_TFC_Id, TFC_Type = @New_TFC_Type, Progress = @New_Progress, TFC_Proposal = @New_TFC_Proposal WHERE StudentNumber = @New_StudentNumber
end
GO

CREATE OR ALTER PROCEDURE sp_check_username (@email nvarchar(50), @password nvarchar(50))
AS
BEGIN
	DECLARE @Count int
	SELECT @Count = COUNT(*) FROM [dbo].[User] WHERE Email = @email AND Password = @password

	IF (@Count) > 0
		SELECT 1 AS UserExists
	ELSE
		SELECT 0 AS UserExists
END
GO

INSERT INTO [dbo].Teacher(Taught_Area, Email, FirstName, LastName, Role) VALUES ('Software', 'nuno.pina@outlook.com', 'Nuno', 'Pina', 'DO');
INSERT INTO [dbo].[User](Email, StudentId, TeacherId, Password) VALUES ('nuno.pina@outlook.com', NULL, 2, 'same');

