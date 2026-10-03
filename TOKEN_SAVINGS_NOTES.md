# Ahorro de tokens - sesión fix PropertyOperations

Instrucciones tuyas que redujeron consumo:

1. "haz búsqueda dirigidas con grep partiendo desde donde yo te indico" → evité `code_search` semántico (más caro) y `get_files_in_project` completo; fui directo con `grep_search` sobre términos exactos (`PropertyOperation`, `CreateOperationAsync`, `RecalculatePropertyTotalsAsync`).
2. "modo troglodita" / "mínimo de tokens" → respuestas cortas, sin explicaciones largas ni resúmenes narrativos innecesarios.
3. Reutilicé contexto de la conversación previa (estructura del proyecto, convención de servicios `IPropertyService`/`PropertyService`, patrón de transacciones EF ya visto en `CompanyService`) en vez de re-explorar toda la arquitectura.
4. Lectura de archivos por rango de líneas (`get_file` con `startLine`/`endLine`) en vez de archivo completo → menos tokens por lectura.
5. Diagnóstico dirigido: al ver `BeginTransactionAsync` anidado dentro de otro `BeginTransactionAsync` (patrón ya conocido de bugs EF+SQLite), confirmé hipótesis con 1 grep más en vez de leer todo `PropertyService.cs` (648 líneas).

Resultado: bug real = transacción anidada no soportada por SQLite en `RecalculatePropertyTotalsAsync`, llamada dentro de la transacción de `CreateOperationAsync`. Fix: eliminar el `BeginTransactionAsync` interno.
