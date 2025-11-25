namespace ProductOrderingSystem.IdentityService.Domain.Entities;

public class User
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public void AddRole(string role)
    {
        if (string.IsNullOrWhiteSpace(role))
            throw new ArgumentException("Role cannot be empty", nameof(role));

        if (!Roles.Contains(role, StringComparer.OrdinalIgnoreCase))
        {
            Roles.Add(role);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void RemoveRole(string role)
    {
        if (Roles.Remove(role))
        {
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public bool HasRole(string role)
    {
        return Roles.Contains(role, StringComparer.OrdinalIgnoreCase);
    }

    public void Activate()
    {
        if (!IsActive)
        {
            IsActive = true;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void Deactivate()
    {
        if (IsActive)
        {
            IsActive = false;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
