# User Secrets Configuration Guide

This project uses .NET User Secrets to keep sensitive API keys out of source control. Each developer needs to configure their own secrets locally.

## Quick Setup

Run the setup script from the repository root:

```powershell
.\Setup-UserSecrets.ps1
```

This will prompt you for your API keys and configure them securely.

## Manual Setup

If you prefer to set up secrets manually:

### Payment Service (Stripe)

```powershell
cd src/Services/PaymentService/ProductOrderingSystem.PaymentService.WebAPI

# Initialize user secrets (only needed once)
dotnet user-secrets init

# Set your Stripe keys
dotnet user-secrets set "Stripe:PublishableKey" "pk_test_your_key_here"
dotnet user-secrets set "Stripe:SecretKey" "sk_test_your_key_here"
```

## Getting API Keys

### Stripe (Payment Service)
1. Sign up at https://stripe.com
2. Go to Developers → API Keys: https://dashboard.stripe.com/test/apikeys
3. Use the **test mode** keys (start with `pk_test_` and `sk_test_`)
4. Copy both the Publishable key and Secret key

### Test Cards
You can use these test cards with Stripe:
- **Success**: `4242 4242 4242 4242` (any future expiry, any CVC)
- **Decline**: `4000 0000 0000 0002`
- **Insufficient funds**: `4000 0000 0000 9995`

## Viewing Your Secrets

To see what secrets are configured:

```powershell
cd src/Services/PaymentService/ProductOrderingSystem.PaymentService.WebAPI
dotnet user-secrets list
```

## Where Are Secrets Stored?

User secrets are stored securely on your local machine:
- **Windows**: `%APPDATA%\Microsoft\UserSecrets\<user_secrets_id>\secrets.json`
- **macOS/Linux**: `~/.microsoft/usersecrets/<user_secrets_id>/secrets.json`

They are **never** committed to source control.

## For CI/CD

In production or CI/CD environments, use:
- **Azure**: Azure Key Vault
- **AWS**: AWS Secrets Manager
- **Environment Variables**: Set via deployment configuration

## Troubleshooting

### "Configuration value not found"
Make sure you've run the setup script or manually set the secrets for the service.

### Secrets not working after setup
1. Verify secrets are set: `dotnet user-secrets list`
2. Make sure you're in the correct project directory
3. Rebuild the solution: `dotnet build`

### Sharing secrets with team (for development only)
If you need to share development API keys with your team:
1. Create a secure shared location (e.g., Azure Key Vault, 1Password)
2. Each developer runs the setup script with the shared keys
3. **Never commit actual keys to the repository**

## Security Best Practices

✅ **DO:**
- Use test/sandbox API keys for development
- Rotate keys regularly
- Use different keys per environment (dev/staging/prod)
- Store production keys in proper secret management (Key Vault, etc.)

❌ **DON'T:**
- Commit API keys to source control
- Share production keys in development
- Use production keys for local testing
- Store keys in plain text files in the repository
