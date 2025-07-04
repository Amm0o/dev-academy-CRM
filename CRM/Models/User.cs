using System.ComponentModel.DataAnnotations;

namespace CRM.Models;

public enum UserRole
{
    Regular,
    Admin
}

public sealed class User : Entity // Sealed to prevent inheritance
{
    private string _name = string.Empty;
    private string _password = string.Empty;
    private string _email = string.Empty;
    private UserRole _role = UserRole.Regular;

    [Required(ErrorMessage = "Name is required")]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get => _name; private set => _name = value; }

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get => _email; private set => _email = value; }

    [Required(ErrorMessage = "Password hash is required")]
    public string Password { get => _password; private set => _password = value; }

    public UserRole Role { get => _role; private set => _role = value; }

    public DateTime UserCreateTime { get; private set; }
    public DateTime UserUpdateTime { get; private set; }

    // Constructor for validation and encapsulation
    public User(string name, string email, string password, UserRole role = UserRole.Regular)
    {
        UserCreateTime = DateTime.UtcNow;
        UserUpdateTime = UserCreateTime;
        UpdateName(name);
        UpdateEmail(email);
        SetPasswordHash(password);
        UpdateRole(role);
    }

    // Add a constructor that allows setting the ID (for database retrieval)
    public User(int id, string name, string email, string password, UserRole role = UserRole.Regular)
    {
        Id = id; // Set the inherited Id
        UserCreateTime = DateTime.UtcNow;
        UserUpdateTime = UserCreateTime;
        UpdateName(name);
        UpdateEmail(email);
        SetPasswordHash(password);
        UpdateRole(role);
    }

    public void SetPasswordHash(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Password Hash cannot be empty", nameof(password));
        }

        Password = password;
        UserUpdateTime = DateTime.UtcNow;
    }

    private void UpdateTimeStamp() => UserUpdateTime = DateTime.UtcNow;

    public void UpdateEmail(string email)
    {
        if (!new EmailAddressAttribute().IsValid(email))
        {
            throw new ArgumentException("Invalid Email Format", nameof(email));
        }

        Email = email;
        UpdateTimeStamp();
    }

    public void UpdateName(string name)
    {

        if (string.IsNullOrWhiteSpace(name) || name.Length < 2 || name.Length > 100)
            throw new ArgumentException("Name must not be empty and > 1 < 101");

        Name = name;
        UpdateTimeStamp();
    }

    public void UpdateRole(UserRole role)
    {
        if (Role == role)
            return; // Avoid unnecessary updates
        Role = role;
        UpdateTimeStamp();
    }


}