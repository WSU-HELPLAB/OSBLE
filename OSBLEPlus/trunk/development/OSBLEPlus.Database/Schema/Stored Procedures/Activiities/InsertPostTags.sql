-------------------------------------------------------------------------------------------------
-------------------------------------------------------------------------------------------------
-- sproc [InsertPostTags]
-------------------------------------------------------------------------------------------------
-------------------------------------------------------------------------------------------------
CREATE PROCEDURE [dbo].[InsertPostTags] @postID   INT,
                                        @usertags VARCHAR(max),
                                        @hashtags VARCHAR(max)
AS
  BEGIN
      SET nocount ON;

      IF @hashtags IS NOT NULL
        BEGIN
            DECLARE @hashtagTable TABLE
              (
                 Tag       VARCHAR(200),
                 isInTable BIT
              )

            INSERT INTO @hashtagTable
            SELECT Tag=Items,
                   isInTable = CASE
                                 WHEN b.Content IS NULL THEN 0
                                 ELSE 1
                               END
            FROM   dbo.Split(@hashtags, ',') a
                   LEFT JOIN dbo.HashTags b
                          ON b.Content = a.Items

            INSERT INTO dbo.HashTags
            SELECT content = Tag
            FROM   @hashtagTable
            WHERE  isInTable = 0

            INSERT INTO FeedPostHashtags
            SELECT FeedPostID = @postID,
                   HashTagID = b.ID
            FROM   @hashtagTable a
                   INNER JOIN dbo.HashTags b
                           ON b.Content = a.Tag
        END

      IF @usertags IS NOT NULL
        BEGIN
            DECLARE @nameTable TABLE
              (
                 NAME VARCHAR(200)
              )

            INSERT INTO @nameTable
            SELECT NAME = Items
            FROM   dbo.Split(@usertags, ',')

            INSERT INTO FeedPostUserTags
            SELECT FeedPostID = @postID,
                   UserID = b.ID,
                   Viewed = 0
            FROM   @nameTable a
                   INNER JOIN dbo.UserProfiles b
                           ON b.FirstName + b.LastName = a.NAME
        END
  END 
