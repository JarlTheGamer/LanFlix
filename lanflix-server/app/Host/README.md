# Lanflix.Host

`Lanflix.Host` is the new production composition root. It owns process startup,
configuration, authentication, authorization, migrations, middleware, health checks,
and delivery of the new web application. Product behavior belongs in
`Lanflix.Modules.*`, not in this project.

Concrete SQLite and filesystem adapters live under `Lanflix.Infrastructure/Adapters`,
grouped by feature. No module may reference `Lanflix.Domain`, `Lanflix.Application`,
`Lanflix.Infrastructure`, or `Lanflix.WebApi`.

`Lanflix.WebApi` is compatibility-only during migration. New routes and features must
be implemented in a module and composed here. Once v1 client parity is verified, the
compatibility application can be removed.
