# Lanflix Wiki Structure

Complete documentation structure for the Lanflix project.

## 📁 Folder Organization

```
docs/
├── README.md                          # Wiki home page
├── WIKI-STRUCTURE.md                  # This file
│
├── getting-started/                   # New user guides
│   ├── overview.md                    # Project overview
│   ├── quick-start.md                 # 5-minute setup guide
│   ├── installation.md                # Detailed installation
│   └── configuration.md               # Environment setup
│
├── architecture/                      # System design
│   ├── system-overview.md             # High-level architecture
│   ├── backend.md                     # Backend structure
│   ├── frontend.md                    # Frontend structure
│   ├── database.md                    # Database schema
│   └── caching.md                     # Caching strategy
│
├── api/                               # API documentation
│   ├── overview.md                    # API introduction
│   ├── content.md                     # Content API
│   ├── library.md                     # Library API
│   ├── streaming.md                   # Streaming API
│   ├── profile.md                     # Profile API
│   └── settings.md                    # Settings API
│
├── features/                          # Feature guides
│   ├── content-discovery.md           # Browse & search
│   ├── download-management.md         # Download queue
│   ├── video-player.md                # Video player
│   ├── multi-profile.md               # Profile management
│   ├── watch-history.md               # Progress tracking
│   └── metadata-caching.md            # Metadata system
│
├── integration/                       # External services
│   ├── sonarr.md                      # Sonarr setup
│   ├── radarr.md                      # Radarr setup
│   ├── prowlarr.md                    # Prowlarr setup
│   └── tmdb.md                        # TMDB setup
│
├── deployment/                        # Production deployment
│   ├── production.md                  # Production guide
│   ├── docker.md                      # Docker setup
│   ├── reverse-proxy.md               # Nginx/Apache
│   └── ssl.md                         # HTTPS setup
│
├── development/                       # Developer guides
│   ├── setup.md                       # Dev environment
│   ├── contributing.md                # Contribution guide
│   ├── code-style.md                  # Coding standards
│   ├── testing.md                     # Testing guide
│   └── debugging.md                   # Troubleshooting
│
├── tasks/                             # Project management
│   ├── current-tasks.md               # Active tasks
│   ├── roadmap.md                     # Feature roadmap
│   ├── known-issues.md                # Bugs & limitations
│   └── completed.md                   # Finished work
│
├── troubleshooting/                   # Problem solving
│   ├── common-issues.md               # FAQ
│   ├── video-playback.md              # Video problems
│   ├── api-connections.md             # Service issues
│   └── performance.md                 # Optimization
│
└── versions/                          # Version history
    ├── README.md                      # Version index
    ├── v0.1/                          # Version 0.1.x
    ├── v0.2/                          # Version 0.2.x
    └── v0.3/                          # Version 0.3.x (current)
        ├── CHANGELOG.md               # Detailed changes
        ├── RELEASE-NOTES.md           # User-facing notes
        └── MIGRATION.md               # Upgrade guide
```

## 📊 Documentation Status

### ✅ Completed (15 files)

1. **docs/README.md** - Wiki home page with navigation
2. **docs/QUICK-REFERENCE.md** - Fast access reference guide
3. **docs/WIKI-STRUCTURE.md** - Documentation structure guide
4. **docs/CHANGES.md** - Recent changes log
5. **docs/getting-started/overview.md** - Project overview and features
6. **docs/getting-started/quick-start.md** - Quick setup guide
7. **docs/architecture/system-overview.md** - Complete architecture
8. **docs/api/overview.md** - API reference and examples
9. **docs/features/video-player.md** - Video player guide
10. **docs/tasks/current-tasks.md** - Active development tasks
11. **docs/tasks/known-issues.md** - Bug tracking
12. **docs/tasks/roadmap.md** - Feature roadmap
13. **docs/troubleshooting/video-playback.md** - Video troubleshooting
14. **docs/versions/README.md** - Version history index
15. **docs/versions/v0.3/** - Version 0.3.x documentation (4 files)

### ⏳ To Be Created (18 files)

#### Getting Started (2)
- `getting-started/installation.md` - Detailed installation steps
- `getting-started/configuration.md` - Environment configuration

#### Architecture (4)
- `architecture/backend.md` - Backend deep dive
- `architecture/frontend.md` - Frontend deep dive
- `architecture/database.md` - Database schema details
- `architecture/caching.md` - Caching implementation

#### API (5)
- `api/content.md` - Content endpoints
- `api/library.md` - Library endpoints
- `api/streaming.md` - Streaming endpoints
- `api/profile.md` - Profile endpoints
- `api/settings.md` - Settings endpoints

#### Features (5)
- `features/content-discovery.md` - Discovery features
- `features/download-management.md` - Download system
- `features/multi-profile.md` - Profile system
- `features/watch-history.md` - History tracking
- `features/metadata-caching.md` - Metadata system

#### Integration (4)
- `integration/sonarr.md` - Sonarr integration
- `integration/radarr.md` - Radarr integration
- `integration/prowlarr.md` - Prowlarr integration
- `integration/tmdb.md` - TMDB integration

#### Deployment (4)
- `deployment/production.md` - Production deployment
- `deployment/docker.md` - Docker setup
- `deployment/reverse-proxy.md` - Proxy configuration
- `deployment/ssl.md` - SSL/TLS setup

#### Development (5)
- `development/setup.md` - Development environment
- `development/contributing.md` - How to contribute
- `development/code-style.md` - Code standards
- `development/testing.md` - Testing guide
- `development/debugging.md` - Debug tips

#### Tasks (1)
- `tasks/completed.md` - Completed tasks archive

#### Troubleshooting (3)
- `troubleshooting/common-issues.md` - Common problems
- `troubleshooting/api-connections.md` - API issues
- `troubleshooting/performance.md` - Performance tips

#### Versions (Ongoing)
- `versions/v0.1/` - Version 0.1.x documentation
- `versions/v0.2/` - Version 0.2.x documentation
- Each version folder contains:
  - `CHANGELOG.md` - Detailed changes
  - `RELEASE-NOTES.md` - User-facing notes
  - `MIGRATION.md` - Upgrade instructions
  - `BREAKING-CHANGES.md` - Breaking changes (if any)

## 🎯 Documentation Principles

### 1. Organization
- **Folders by topic** - Related docs grouped together
- **Clear hierarchy** - Easy to navigate
- **Consistent naming** - Lowercase with hyphens
- **Logical flow** - From beginner to advanced

### 2. Content
- **Comprehensive** - Cover all aspects
- **Practical** - Include examples and code
- **Up-to-date** - Maintain accuracy
- **Searchable** - Use clear headings

### 3. Style
- **Clear language** - Easy to understand
- **Code examples** - Show, don't just tell
- **Visual aids** - Diagrams and tables
- **Cross-references** - Link related docs

## 📝 Writing Guidelines

### Document Structure

```markdown
# Title

Brief introduction (1-2 paragraphs)

## Overview

High-level explanation

## Features/Sections

Detailed content with:
- Code examples
- Screenshots (when applicable)
- Tables
- Lists

## Examples

Practical examples

## Troubleshooting

Common issues and solutions

## Related Documentation

Links to related docs

**Last Updated**: Date
```

### Code Examples

Always include:
- Language identifier
- Comments explaining key parts
- Complete, runnable examples
- Error handling

```javascript
// Good example
async function fetchContent() {
  try {
    const response = await fetch('/api/content/discover');
    const data = await response.json();
    return data;
  } catch (error) {
    console.error('Failed to fetch content:', error);
    throw error;
  }
}
```

### Cross-References

Use relative links:
```markdown
See [API Overview](../api/overview.md) for details.
```

## 🔄 Maintenance

### Update Frequency
- **Weekly**: Current tasks, known issues
- **Monthly**: Roadmap, feature docs
- **Per Release**: API docs, architecture
- **As Needed**: Troubleshooting, guides

### Version Control
- All docs in Git
- Review changes in PRs
- Update "Last Updated" date
- Maintain changelog

## 🎨 Formatting Standards

### Headings
```markdown
# H1 - Document Title (once per file)
## H2 - Major Sections
### H3 - Subsections
#### H4 - Details (use sparingly)
```

### Lists
```markdown
- Unordered lists for features
- Use consistent bullet style
- Keep items parallel

1. Ordered lists for steps
2. Number sequentially
3. Use for procedures
```

### Code Blocks
````markdown
```language
code here
```
````

### Tables
```markdown
| Column 1 | Column 2 |
|----------|----------|
| Data 1   | Data 2   |
```

### Emphasis
```markdown
**Bold** for important terms
*Italic* for emphasis
`code` for inline code
```

## 🔍 Search Optimization

### Keywords
Include relevant keywords in:
- Document titles
- Headings
- First paragraph
- Code comments

### Tags
Use consistent terminology:
- "API" not "api" or "Api"
- "TypeScript" not "typescript"
- "Node.js" not "NodeJS"

## 📱 Platform-Specific Docs

### Windows
```bash
# Windows commands
copy .env.example .env
dir
```

### macOS/Linux
```bash
# Unix commands
cp .env.example .env
ls -la
```

### Cross-Platform
Use Node.js scripts when possible:
```json
{
  "scripts": {
    "setup": "node scripts/setup.js"
  }
}
```

## 🌐 Internationalization (Future)

Planned language support:
- English (primary)
- Spanish
- French
- German
- Japanese
- Chinese

Structure:
```
docs/
├── en/  (English)
├── es/  (Spanish)
├── fr/  (French)
└── ...
```

## 📊 Documentation Metrics

Track:
- Page views
- Search queries
- User feedback
- Broken links
- Outdated content

## 🤝 Contributing to Docs

1. **Fork repository**
2. **Create branch**: `docs/feature-name`
3. **Write documentation**
4. **Test links and code**
5. **Submit pull request**
6. **Address feedback**

See [Contributing Guide](./development/contributing.md) for details.

## 📞 Documentation Support

- **Questions**: GitHub Discussions
- **Issues**: GitHub Issues
- **Suggestions**: Pull Requests
- **Feedback**: docs@lanflix.com (planned)

## 🎯 Next Steps

### Priority 1 (This Week)
- [ ] Complete API documentation (5 files)
- [ ] Add installation guide
- [ ] Create configuration guide

### Priority 2 (Next Week)
- [ ] Backend architecture details
- [ ] Frontend architecture details
- [ ] Database schema documentation

### Priority 3 (This Month)
- [ ] Integration guides (4 files)
- [ ] Deployment guides (4 files)
- [ ] Development guides (5 files)

## 📈 Success Metrics

Documentation is successful when:
- ✅ New users can set up in <10 minutes
- ✅ Developers can contribute without asking questions
- ✅ Common issues are self-serviceable
- ✅ API is fully documented with examples
- ✅ Search finds relevant content quickly

---

**Last Updated**: October 30, 2025  
**Maintained By**: Lanflix Team  
**License**: MIT
