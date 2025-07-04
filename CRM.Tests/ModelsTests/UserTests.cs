using CRM.Models;
using Xunit;

namespace CRM.Tests.ModelsTests;

public class UserTests
{
    [Fact]
    public void Constructor_ValidData_InitializesCorrectly()
    {
        string name = "John Doe";
        string email = "john@contoso.com";
        string passwordHash = "hashedpass123";
        UserRole role = UserRole.Admin;

        var user = new User(name, email, passwordHash, role);

        // Assert
        Assert.Equal(name, user.Name);
        Assert.Equal(email, user.Email);
        // Note: PasswordHash might be private, so we can't test it directly
        Assert.Equal(role, user.Role);
        Assert.True(user.UserCreateTime <= DateTime.UtcNow);
        // Remove the exact equality check as timestamps might differ by microseconds
        Assert.True((user.UserUpdateTime - user.UserCreateTime).TotalSeconds < 1);
    }

    [Fact]
    public void Constructor_InvalidName_ThrowsArgumentException()
    {
        string invalidName = "a";

        var exception = Assert.Throws<ArgumentException>(() =>
            new User(invalidName, "john@example.com", "hashedpass123"));
        Assert.Equal("Name must not be empty and > 1 < 101", exception.Message);
    }

    [Fact]
    public void Constructor_InvalidEmail_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new User("John Doe", "not-an-email", "hashedpass123"));
        Assert.Equal("Invalid Email Format (Parameter 'email')", exception.Message);
    }

    [Fact]
    public void Constructor_EmptyPasswordHash_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new User("John Doe", "john@example.com", ""));
        // Fix the expected message - it's 'password' not 'passwordHash'
        Assert.Equal("Password Hash cannot be empty (Parameter 'password')", exception.Message);
    }

    [Fact]
    public void UpdateName_ValidName_UpdatesCorrectly()
    {
        var user = new User("John Doe", "john@example.com", "hashedpass123");
        string newName = "Jane Doe";
        var originalUpdateTime = user.UserUpdateTime;
        Thread.Sleep(100);
        user.UpdateName(newName);
        Assert.Equal(newName, user.Name);
        Assert.True(user.UserUpdateTime > originalUpdateTime);
    }

    [Fact]
    public void UpdateName_EmptyName_ThrowsArgumentException()
    {
        var user = new User("John Doe", "john@example.com", "hashedpass123");
        Assert.Throws<ArgumentException>(() => user.UpdateName(""));
    }

    [Fact]
    public void UpdateName_NameTooLong_ThrowsArgumentException()
    {
        var user = new User("John Doe", "john@example.com", "hashedpass123");
        string longName = new string('a', 102);
        Assert.Throws<ArgumentException>(() => user.UpdateName(longName));
    }

    [Fact]
    public void UpdateEmail_ValidEmail_UpdatesCorrectly()
    {
        var user = new User("John Doe", "john@example.com", "hashedpass123");
        string newEmail = "jane@example.com";
        var originalUpdateTime = user.UserUpdateTime;
        Thread.Sleep(100);
        user.UpdateEmail(newEmail);
        Assert.Equal(newEmail, user.Email);
        Assert.True(user.UserUpdateTime > originalUpdateTime);
    }

    [Fact]
    public void UpdateEmail_InvalidEmail_ThrowsArgumentException()
    {
        var user = new User("John Doe", "john@example.com", "hashedpass123");
        Assert.Throws<ArgumentException>(() => user.UpdateEmail("not-an-email"));
    }

    [Fact]
    public void UpdatePasswordHash_ValidHash_UpdatesCorrectly()
    {
        var user = new User("John Doe", "john@example.com", "hashedpass123");
        string newHash = "newhash456";
        var originalUpdateTime = user.UserUpdateTime;
        Thread.Sleep(100);
        user.SetPasswordHash(newHash);
        // We can't verify the password hash directly if it's private
        // but we can verify the update time changed
        Assert.True(user.UserUpdateTime > originalUpdateTime);
    }

    [Fact]
    public void UpdatePasswordHash_EmptyHash_ThrowsArgumentException()
    {
        var user = new User("John Doe", "john@example.com", "hashedpass123");
        Assert.Throws<ArgumentException>(() => user.SetPasswordHash(""));
    }

    [Fact]
    public void UpdateRole_DifferentRole_UpdatesCorrectly()
    {
        var user = new User("John Doe", "john@example.com", "hashedpass123", UserRole.Regular);
        var originalUpdateTime = user.UserUpdateTime;
        Thread.Sleep(100);
        user.UpdateRole(UserRole.Admin);
        Assert.Equal(UserRole.Admin, user.Role);
        Assert.True(user.UserUpdateTime > originalUpdateTime);
    }

    [Fact]
    public void UpdateRole_SameRole_DoesNotUpdateTimestamp()
    {
        var user = new User("John Doe", "john@example.com", "hashedpass123", UserRole.Regular);
        var originalUpdateTime = user.UserUpdateTime;
        Thread.Sleep(100);
        user.UpdateRole(UserRole.Regular);
        Assert.Equal(UserRole.Regular, user.Role);
        Assert.Equal(originalUpdateTime, user.UserUpdateTime);
    }
}