# GitFlow Branching Strategy

This document outlines the GitFlow branching strategy, a robust workflow design for managing feature development, releases, and hotfixes.

## Branch Types

- **`main` (or `master`)**: The production branch containing released code
- **`develop`**: The integration branch for development work
- **`feature/*`**: Short-lived branches for new features or improvements
- **`release/*`**: Preparation branches for upcoming releases
- **`hotfix/*`**: Emergency fixes for production issues
- **`bugfix/*`**: Non-critical bug fixes developed from `develop`

## Branching Model Visualization

```mermaid
%%{init: { 'logLevel': 'debug', 'theme': 'base', 'gitGraph': {'showBranches': true, 'showCommitLabel':true, 'mainBranchName': 'main'}} }%%
gitGraph
   commit id: "initial commit"
   branch develop
   checkout develop
   commit id: "start development"

   %% Feature 1 development
   branch feature/login
   checkout feature/login
   commit id: "add login form"
   commit id: "add validation"
   checkout develop
   merge feature/login id: "merge: login feature"
   
   %% Feature 2 development
   branch feature/dashboard
   checkout feature/dashboard
   commit id: "create dashboard"
   commit id: "add widgets"
   checkout develop
   merge feature/dashboard id: "merge: dashboard feature"
   
   %% Release branch
   branch release/1.0.0
   checkout release/1.0.0
   commit id: "version bump"
   commit id: "final fixes"
   checkout main
   merge release/1.0.0 id: "release 1.0.0" tag: "v1.0.0"
   checkout develop
   merge release/1.0.0 id: "sync release back"

   %% Production hotfix
   checkout main
   branch hotfix/1.0.1
   checkout hotfix/1.0.1
   commit id: "fix critical bug"
   checkout main
   merge hotfix/1.0.1 id: "release hotfix" tag: "v1.0.1"
   checkout develop
   merge hotfix/1.0.1 id: "sync hotfix to develop"

   %% Continue development
   checkout develop
   commit id: "continue development"
   branch feature/reports
   checkout feature/reports
   commit id: "add reporting"
   checkout develop
   merge feature/reports id: "merge: reporting"
   
   %% Bug fix branch
   branch bugfix/styling
   checkout bugfix/styling
   commit id: "fix styling issues"
   checkout develop
   merge bugfix/styling id: "merge: styling fix"
   
   %% Another release
   branch release/1.1.0
   checkout release/1.1.0
   commit id: "version bump to 1.1.0"
   commit id: "release adjustments"
   checkout main
   merge release/1.1.0 id: "release 1.1.0" tag: "v1.1.0"
   checkout develop
   merge release/1.1.0 id: "sync 1.1.0 back"
```

## Workflow Description

### Main Development Flow

1. Development work happens on the `develop` branch
2. Features are created in `feature/*` branches from `develop`
3. When complete, features are merged back into `develop`

### Release Process

1. When `develop` has enough features, create a `release/*` branch
2. The release branch undergoes testing and final adjustments
3. When ready, merge into `main` and tag with a version number
4. Also merge back into `develop` to ensure fixes are in future releases

### Hotfix Process

1. For urgent production issues, create `hotfix/*` branches from `main`
2. Fix the issue and merge back to `main` with a version bump tag
3. Also merge into `develop` to include the fix in future releases

### Bug Fix Process

1. For non-urgent bugs, create `bugfix/*` branches from `develop` 
2. Fix the issue and merge back to `develop`

## Best Practices

1. **Branch Naming**:
   - `feature/descriptive-name` or `feature/JIRA-123-short-description`
   - `release/1.2.3` (using semantic versioning)
   - `hotfix/1.2.4` or `hotfix/critical-issue-description`
   - `bugfix/issue-description`

2. **Commit Guidelines**:
   - Write clear, concise commit messages
   - Reference issue/ticket numbers when applicable
   - Use conventional commit format: `type(scope): message`

3. **Merging Strategy**:
   - Use pull requests/merge requests for code review
   - Consider squashing feature commits when merging
   - Keep feature branches short-lived (days, not weeks)

4. **Tagging**:
   - Tag all releases on the `main` branch
   - Use semantic versioning: `v1.2.3`
   - Include release notes with tags when possible