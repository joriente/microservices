---
tags:
  - configuration
  - api-keys
  - secrets
  - setup
  - security
  - user-secrets
---
# Developer Setup - API Keys

## Quick Start

New to the project? Run this script to set up your API keys:

```powershell
.\Setup-UserSecrets.ps1
```

## What This Does

The script will:
1. Initialize user secrets for Payment Service
2. Prompt you for your Stripe API keys
3. Store them securely on your machine (outside source control)

## Getting Your Keys

### Stripe Test Keys
1. Go to https://dashboard.stripe.com/test/apikeys
2. Copy your **Publishable key** (starts with `pk_test_`)
3. Copy your **Secret key** (starts with `sk_test_`)

## Already Have Secrets?

View your configured secrets:
```powershell
cd src/Services/PaymentService/ProductOrderingSystem.PaymentService.WebAPI
dotnet user-secrets list
```

## Full Documentation

See [docs/User-Secrets-Setup.md](docs/User-Secrets-Setup.md) for complete details.

## Test Cards

Use these Stripe test cards during development:
- **Success**: `4242 4242 4242 4242`
- **Decline**: `4000 0000 0000 0002`
- **Insufficient funds**: `4000 0000 0000 9995`

(Use any future expiry date and any 3-digit CVC)

## Pre-seeded Test Users

The system automatically creates these users for testing:

- **Admin User**: 
  - Username: `admin`
  - Password: `P@ssw0rd`
  - Access: Full admin panel access
  
- **Shopper User**: 
  - Username: `steve.hopper`
  - Password: `P@ssw0rd`
  - Access: Regular customer account

Use these to test the application without creating new accounts!

