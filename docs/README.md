# Lanflix Documentation

Welcome to the Lanflix documentation. Lanflix is a high-performance media streaming server built with .NET 9 and Vanilla JavaScript.

## 📖 Main Documentation

- **[Architecture](./ARCHITECTURE.md)** - System design and technology stack.
- **[Feature Roadmap](./ROADMAP.md)** - Master product roadmap comparing Jellyfin & Plex.
- **[Build Guide](./BUILD.md)** - How to build and publish the server.
- **[API Overview](./api/overview.md)** - REST API reference.
- **[Tasks & Roadmap](./tasks.md)** - Current development status.

## 🚀 Quick Setup

1. **Build the server**: Run `.\lanflix-server\build.ps1 -Clean`
2. **Launch**: Run `.\lanflix-server\publish\Lanflix.WebApi.exe`
3. **Configure**: Access `http://localhost:5037` and set your media paths.

## 🛠 Troubleshooting

- Check the **Serilog** logs in the `logs/` directory of the published app.
- Ensure **FFmpeg** is in your system PATH.
- Use **Chrome** or **Edge** for the best hardware acceleration support.

---
*Last Updated: January 2026*
