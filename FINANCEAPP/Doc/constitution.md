# Constitución de FINANCEAPP

Proyecto personal y educativo: debe poder mantenerlo alguien que empieza a programar. Estos seis principios no se negocian.

1. **Stack mínimo.** Solo .NET MAUI Blazor, EF Core y SQLite; ningún paquete NuGet ni librería JS nueva sin aprobación expresa.
2. **La spec manda.** Ningún cambio de comportamiento sin su spec, plan y tareas en `doc/specs/`; al terminar, la spec base refleja el código.
3. **Lógica fuera de la interfaz.** Las páginas `.razor` solo muestran y recogen datos; cálculos y reglas viven en `Services/`.
4. **Tests sin dependencias.** No se instala ningún framework de pruebas; cada tarea lleva su lista de comprobación manual, y nada se da por hecho sin compilar y recorrerla.
5. **Datos del usuario intocables.** La BD real nunca se borra ni se usa para pruebas: copia y backup antes, y el esquema evoluciona solo con `DatabaseSchemaUpgrader`.
6. **Idioma.** Textos visibles, mensajes y documentación en español; código, carpetas, ficheros, tablas y rutas nuevas en inglés.
