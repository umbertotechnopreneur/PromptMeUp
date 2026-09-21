// SPDX-License-Identifier: MIT

namespace PromptMeUp.Models;

/// <summary>Describes the minimal runtime facts that make terminal guidance actionable without exposing a machine or account identity.</summary>
public sealed record RuntimeContext(
    string OperatingSystem,
    string CommandShell,
    string PreferredScriptInterpreter,
    bool IsPreferredScriptInterpreterAvailable,
    string WorkingDirectory)
{
    /// <summary>Formats approved technical facts, revealing the sanitized working directory only to an operational prompt.</summary>
    public string ToPromptBlock(string language, bool includeWorkingDirectory)
    {
        var text = RuntimeContextPromptText.For(language);
        var lines = new List<string>
        {
            text.Title,
            string.Format(text.OperatingSystem, OperatingSystem),
            string.Format(text.CommandShell, CommandShell),
            string.Format(
                text.PreferredScriptInterpreter,
                PreferredScriptInterpreter,
                IsPreferredScriptInterpreterAvailable ? text.Available : text.Unavailable)
        };
        if (includeWorkingDirectory)
        {
            lines.Add(string.Format(text.WorkingDirectory, WorkingDirectory));
        }

        lines.Add(text.PrivacyBoundary);
        return string.Join(Environment.NewLine, lines);
    }
}

/// <summary>Keeps the provider-bound runtime-context wording complete for every supported interface language.</summary>
internal sealed record RuntimeContextPromptText(
    string Title,
    string OperatingSystem,
    string CommandShell,
    string PreferredScriptInterpreter,
    string WorkingDirectory,
    string Available,
    string Unavailable,
    string PrivacyBoundary)
{
    /// <summary>Returns the runtime-context wording for a supported language, with English as the defensive fallback.</summary>
    public static RuntimeContextPromptText For(string? language) => language?.Trim().ToLowerInvariant() switch
    {
        "it" => new(
            "Contesto runtime fornito da PromptMeUp. Consideralo autorevole; non dedurre dettagli non disponibili:",
            "- Sistema operativo: {0}",
            "- Shell effettiva per i comandi approvati: {0}",
            "- Interprete script preferito: {0} ({1})",
            "- Cartella di lavoro corrente (sanitizzata): {0}",
            "disponibile localmente",
            "non rilevato localmente",
            "- Limite di privacy: non sono forniti nome utente, nome host, percorsi personali, valori delle variabili d'ambiente o segreti. Non richiederli, dedurli o inventarli."),
        "fr" => new(
            "Contexte d'exécution fourni par PromptMeUp. Considérez-le comme fiable ; n'inférez pas de détails indisponibles :",
            "- Système d'exploitation : {0}",
            "- Interpréteur de commandes effectif pour les commandes approuvées : {0}",
            "- Interpréteur de script préféré : {0} ({1})",
            "- Répertoire de travail actuel (nettoyé) : {0}",
            "disponible localement",
            "non détecté localement",
            "- Limite de confidentialité : aucun nom d'utilisateur, nom d'hôte, chemin personnel, valeur de variable d'environnement ou secret n'est fourni. Ne les demandez pas, ne les déduisez pas et ne les inventez pas."),
        "de" => new(
            "Von PromptMeUp bereitgestellter Laufzeitkontext. Behandle ihn als verlässlich und leite keine nicht verfügbaren Details ab:",
            "- Betriebssystem: {0}",
            "- Tatsächliche Befehls-Shell für genehmigte Befehle: {0}",
            "- Bevorzugter Skriptinterpreter: {0} ({1})",
            "- Aktuelles Arbeitsverzeichnis (bereinigt): {0}",
            "lokal verfügbar",
            "lokal nicht erkannt",
            "- Datenschutzgrenze: Es werden kein Benutzername, Hostname, persönlicher Pfad, Umgebungsvariablenwert oder Geheimnis bereitgestellt. Frage nicht danach, leite nichts davon ab und erfinde nichts."),
        "es" => new(
            "Contexto de ejecución proporcionado por PromptMeUp. Trátalo como fiable; no infieras detalles no disponibles:",
            "- Sistema operativo: {0}",
            "- Shell efectiva para comandos aprobados: {0}",
            "- Intérprete de scripts preferido: {0} ({1})",
            "- Directorio de trabajo actual (saneado): {0}",
            "disponible localmente",
            "no detectado localmente",
            "- Límite de privacidad: no se proporciona ningún nombre de usuario, nombre de host, ruta personal, valor de variable de entorno ni secreto. No los solicites, infieras ni inventes."),
        "vi" => new(
            "Ngữ cảnh thời gian chạy do PromptMeUp cung cấp. Hãy xem đây là thông tin đáng tin cậy; không suy diễn chi tiết không có sẵn:",
            "- Hệ điều hành: {0}",
            "- Shell thực tế cho lệnh đã được phê duyệt: {0}",
            "- Trình thông dịch script ưu tiên: {0} ({1})",
            "- Thư mục làm việc hiện tại (đã lọc): {0}",
            "có sẵn cục bộ",
            "chưa được phát hiện cục bộ",
            "- Ranh giới riêng tư: không cung cấp tên người dùng, tên máy chủ, đường dẫn cá nhân, giá trị biến môi trường hoặc bí mật. Không yêu cầu, suy diễn hoặc bịa ra chúng."),
        _ => new(
            "Runtime context supplied by PromptMeUp. Treat it as authoritative; do not infer unavailable details:",
            "- Operating system: {0}",
            "- Effective command shell for approved commands: {0}",
            "- Preferred script interpreter: {0} ({1})",
            "- Current working directory (sanitized): {0}",
            "available locally",
            "not detected locally",
            "- Privacy boundary: no user name, host name, personal path, environment-variable value, or secret is supplied. Do not request, infer, or invent them.")
    };
}
