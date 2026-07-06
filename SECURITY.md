# Security Policy

## Supported Versions

Only the latest `main` branch and the latest tagged release are actively reviewed.

## Reporting a Vulnerability

Do not open a public issue for vulnerabilities or accidental secret exposure.
Use GitHub private vulnerability reporting when available, or contact the repository
owner privately through GitHub.

Please include:

- affected version or commit
- Unity version and render pipeline
- reproduction steps
- impact and expected behavior
- any relevant logs or screenshots with secrets removed

## Public Repository Hygiene

Before making this repository public or cutting a release:

- run the Unity EditMode test suite
- run a secret scan over tracked files
- verify no local paths, screenshots, temporary files, generated projects, or credentials are tracked
- confirm the license terms are intentional for the release
