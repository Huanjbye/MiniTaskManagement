namespace MiniTaskManagement.Api.Entities;

public enum ChatRoomType
{
    Private = 0,
    Support = 1,
    Group = 2,
    Project = 3,
    Task = 4
}

public enum ChatRoomMemberRole
{
    Owner = 0,
    Admin = 1,
    Member = 2
}

public enum ChatMessageType
{
    Text = 0,
    Image = 1,
    File = 2,
    System = 3
}
