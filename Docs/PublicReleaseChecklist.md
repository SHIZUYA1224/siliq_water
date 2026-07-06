# Public Release Checklist

Use this before switching the GitHub repository to public or cutting a product release.

## Repository Hygiene

- [ ] `git status -sb` is clean
- [ ] `.gitignore` excludes Unity generated folders and local IDE files
- [ ] `.gitattributes` marks Unity text assets and binary textures correctly
- [ ] every Unity package asset has a matching `.meta` file
- [ ] no screenshots, temporary captures, local test projects, or generated materials are tracked

## Legal And Package Metadata

- [ ] `LICENSE.md` matches the intended distribution model
- [ ] `package.json` has the correct version, author, URLs, and license reference
- [ ] `README.md` links to docs, changelog, security policy, and material preset guide
- [ ] public text avoids internal-only notes or emotional language

## Quality

- [ ] Unity batch tests pass
- [ ] `git diff --check` passes
- [ ] generated/prebaked textures import without warnings
- [ ] water studio opens without layout errors at narrow and wide window sizes
- [ ] purpose-first presets set reasonable maps, quality, and material options
- [ ] Quick Apply creates moving, usable materials for clear sea, pool, blood sea, and liquid metal

## Security

- [ ] tracked-file secret scan returns no credentials
- [ ] no personal local paths are present in tracked text files
- [ ] `SECURITY.md` explains private vulnerability reporting

## Release

- [ ] changelog has an entry for the release version
- [ ] Git tag is created after final validation
- [ ] GitHub repository description and topics match the package
- [ ] repository visibility is changed only after the above checks pass
