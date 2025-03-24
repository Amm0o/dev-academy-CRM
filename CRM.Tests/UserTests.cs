using CRM.Models;
using Xunit;

namespace CRM.Tests;

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
        Assert.Equal(passwordHash, user.PasswordHash);
        Assert.Equal(role, user.Role);
        Assert.True(user.UserCreateTime <= DateTime.UtcNow);
        Assert.Equal(user.UserCreateTime, user.UserCreateTime);
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
        Assert.Equal("Password Hash cannot be empty (Parameter 'passwordHash')", exception.Message);
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
        var exception = Assert.Throws<ArgumentException>(() => user.UpdateName(""));
        Assert.Equal("Name must not be empty and > 1 < 101", exception.Message);
    }

    [Fact]
    public void UpdateName_NameTooLong_ThrowsArgumentException()
    {
        var user = new User("John Doe", "john@example.com", "hashedpass123");
        string longName = new string('A', 101);
        var exception = Assert.Throws<ArgumentException>(() => user.UpdateName(longName));
        Assert.Equal("Name must not be empty and > 1 < 101", exception.Message);
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
        var exception = Assert.Throws<ArgumentException>(() => user.UpdateEmail("invalid-email"));
        Assert.Equal("Invalid Email Format (Parameter 'email')", exception.Message);
    }

    [Fact]
    public void SetPasswordHash_ValidHash_UpdatesCorrectly()
    {
        var user = new User("John Doe", "john@example.com", "hashedpass123");
        string newHash = "newhashedpass456";
        var originalUpdateTime = user.UserUpdateTime;
        Thread.Sleep(100);
        user.SetPasswordHash(newHash);
        Assert.Equal(newHash, user.PasswordHash);
        Assert.True(user.UserUpdateTime > originalUpdateTime);
    }

    [Fact]
    public void SetPasswordHash_EmptyHash_ThrowsArgumentException()
    {
        var user = new User("John Doe", "john@example.com", "hashedpass123");
        var exception = Assert.Throws<ArgumentException>(() => user.SetPasswordHash(""));
        Assert.Equal("Password Hash cannot be empty (Parameter 'passwordHash')", exception.Message);
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