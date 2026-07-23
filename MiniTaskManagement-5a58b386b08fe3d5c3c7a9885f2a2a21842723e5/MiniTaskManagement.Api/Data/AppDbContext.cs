using Microsoft.EntityFrameworkCore;
using MiniTaskManagement.Api.Entities;

namespace MiniTaskManagement.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<TaskItem> Tasks => Set<TaskItem>();

    public DbSet<Project> Projects => Set<Project>();

    public DbSet<TaskSubtask> TaskSubtasks => Set<TaskSubtask>();

    public DbSet<TaskComment> TaskComments => Set<TaskComment>();

    public DbSet<TaskActivity> TaskActivities => Set<TaskActivity>();

    public DbSet<Tag> Tags => Set<Tag>();

    public DbSet<TaskTag> TaskTags => Set<TaskTag>();

    public DbSet<ChatRoom> ChatRooms => Set<ChatRoom>();

    public DbSet<ChatRoomMember> ChatRoomMembers => Set<ChatRoomMember>();

    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();

    public DbSet<ChatMessageRead> ChatMessageReads => Set<ChatMessageRead>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>()
            .HasIndex(x => x.Email)
            .IsUnique();

        modelBuilder.Entity<TaskItem>()
            .HasOne(x => x.User)
            .WithMany(x => x.Tasks)
            .HasForeignKey(x => x.UserId);

        modelBuilder.Entity<Project>()
            .HasOne(x => x.Owner)
            .WithMany(x => x.Projects)
            .HasForeignKey(x => x.OwnerId);

        modelBuilder.Entity<TaskItem>()
            .HasOne(x => x.Project)
            .WithMany(x => x.Tasks)
            .HasForeignKey(x => x.ProjectId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<TaskSubtask>()
            .HasOne(x => x.TaskItem)
            .WithMany(x => x.Subtasks)
            .HasForeignKey(x => x.TaskItemId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TaskComment>()
            .HasOne(x => x.TaskItem)
            .WithMany(x => x.Comments)
            .HasForeignKey(x => x.TaskItemId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TaskComment>()
            .HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId);

        modelBuilder.Entity<TaskActivity>()
            .HasOne(x => x.TaskItem)
            .WithMany(x => x.Activities)
            .HasForeignKey(x => x.TaskItemId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TaskActivity>()
            .HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId);

        modelBuilder.Entity<Tag>()
            .HasIndex(x => new { x.UserId, x.Name })
            .IsUnique();

        modelBuilder.Entity<Tag>()
            .HasOne(x => x.User)
            .WithMany(x => x.Tags)
            .HasForeignKey(x => x.UserId);

        modelBuilder.Entity<TaskTag>()
            .HasKey(x => new { x.TaskItemId, x.TagId });

        modelBuilder.Entity<TaskTag>()
            .HasOne(x => x.TaskItem)
            .WithMany(x => x.TaskTags)
            .HasForeignKey(x => x.TaskItemId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TaskTag>()
            .HasOne(x => x.Tag)
            .WithMany(x => x.TaskTags)
            .HasForeignKey(x => x.TagId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ChatRoom>()
            .HasOne(x => x.CreatedByUser)
            .WithMany()
            .HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ChatRoom>()
            .HasOne(x => x.Project)
            .WithMany()
            .HasForeignKey(x => x.ProjectId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<ChatRoom>()
            .HasOne(x => x.Task)
            .WithMany()
            .HasForeignKey(x => x.TaskId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<ChatRoomMember>()
            .HasIndex(x => new { x.ChatRoomId, x.UserId })
            .IsUnique();

        modelBuilder.Entity<ChatRoomMember>()
            .HasOne(x => x.ChatRoom)
            .WithMany(x => x.Members)
            .HasForeignKey(x => x.ChatRoomId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ChatRoomMember>()
            .HasOne(x => x.User)
            .WithMany(x => x.ChatRoomMembers)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ChatMessage>()
            .HasIndex(x => new { x.ChatRoomId, x.CreatedAt });

        modelBuilder.Entity<ChatMessage>()
            .HasOne(x => x.ChatRoom)
            .WithMany(x => x.Messages)
            .HasForeignKey(x => x.ChatRoomId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ChatMessage>()
            .HasOne(x => x.Sender)
            .WithMany(x => x.ChatMessages)
            .HasForeignKey(x => x.SenderId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ChatMessage>()
            .HasOne(x => x.ReplyToMessage)
            .WithMany(x => x.Replies)
            .HasForeignKey(x => x.ReplyToMessageId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ChatMessageRead>()
            .HasIndex(x => new { x.MessageId, x.UserId })
            .IsUnique();

        modelBuilder.Entity<ChatMessageRead>()
            .HasOne(x => x.Message)
            .WithMany(x => x.Reads)
            .HasForeignKey(x => x.MessageId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ChatMessageRead>()
            .HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
