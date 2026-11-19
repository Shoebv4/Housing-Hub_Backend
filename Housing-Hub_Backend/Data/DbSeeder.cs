using HousingHubBackend.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HousingHubBackend.Data
{
    public static class DbSeeder
    {
        private const string SuperAdminRole = "super_admin";

        /// <summary>
        /// Creates a super admin account from the "SeedSuperAdmin" configuration
        /// section if no super admin exists yet. Safe to call on every startup.
        /// </summary>
        public static async Task SeedSuperAdminAsync(
            HousingHubDBContext context,
            IConfiguration configuration,
            ILogger logger)
        {
            var section = configuration.GetSection("SeedSuperAdmin");
            var email = section["Email"]?.Trim().ToLowerInvariant();
            var password = section["Password"];

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                logger.LogWarning(
                    "Super admin seeding skipped: configure SeedSuperAdmin:Email and SeedSuperAdmin:Password.");
                return;
            }

            if (await context.UserAccounts.AnyAsync(u => u.Role == SuperAdminRole))
            {
                logger.LogInformation("A super admin already exists; skipping seeding.");
                return;
            }

            if (await context.UserAccounts.AnyAsync(u => u.Email == email))
            {
                logger.LogWarning(
                    "Seeding skipped: a user with email {Email} already exists with a different role.", email);
                return;
            }

            var superAdmin = new UserAccount
            {
                Name = section["Name"] ?? "Super Admin",
                Email = email,
                Phone = section["Phone"],
                Role = SuperAdminRole,
                SocietyId = null,
                FlatId = null,
                IsVerified = true
            };

            var passwordHasher = new PasswordHasher<UserAccount>();
            superAdmin.Password = passwordHasher.HashPassword(superAdmin, password);

            context.UserAccounts.Add(superAdmin);
            await context.SaveChangesAsync();

            logger.LogInformation("Seeded super admin account {Email}.", email);
        }
    }
}
