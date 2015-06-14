CREATE PROCEDURE [dbo].[LogUserTransaction]
	@UserId       INT,
	@ActivityTime DATETIME
AS
  BEGIN

      SET NOCOUNT ON;

      IF EXISTS(SELECT 1
                FROM   [dbo].[UserActivities]
                WHERE  [UserID] = @UserId)
        UPDATE [dbo].[UserActivities]
        SET    [LastVisualStudioActivity] = @ActivityTime
        WHERE  UserID = @UserId
      ELSE
        INSERT INTO [dbo].[UserActivities]
                    (UserID,
                     LastVisualStudioActivity)
        VALUES      (@UserId,
                     @ActivityTime)

  END 
