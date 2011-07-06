using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models.Users
{
    public abstract class TeamUserMember
    {
        [Key]
        [Required]
        public int ID { get; set; }

        public abstract bool Contains(UserProfile user);
    }
}

/*
[Key]
[Required]
public int ID { get; set; }

/// <summary>
/// This must be safely castable to TeamOrUser enum
/// </summary>
[Required]
public int TeamOrUser { get; set; }

public int? TeamID { get; set; }

public virtual Team Team { get; set; }

public int? UserProfileID { get; set; }

public virtual UserProfile UserProfile { get; set; }

/// <summary>
/// This will create the TeamUser with all the need stuff being set
/// </summary>
/// <param name="user"></param>
public TeamUserMember(Team team)
{
    TeamOrUser = (int)Users.TeamOrUser.Team;
    Team = team;
    TeamID = team.ID;

    UserProfile = null;
    UserProfileID = null;
}

/// <summary>
/// This will create the TeamUser with all the need stuff being set
/// </summary>
/// <param name="user"></param>
public TeamUserMember(UserProfile user)
{
    TeamOrUser = (int)Users.TeamOrUser.User;
    UserProfile = user;
    UserProfileID = user.ID;

    Team = null;
    TeamID = null;
}

//Only to be used by DB
public TeamUserMember()
{
}

public bool Contains(UserProfile user)
{
    if (TeamOrUser == (int)Users.TeamOrUser.User)
    {
        if (UserProfileID == user.ID)
        {
            return true;
        }
    }
    else
    {
        return Team.Contains(user);
    }
    return false;
}
}
}*/