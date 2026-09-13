// SPDX-License-Identifier: MIT

namespace PromptMeUp.Services;

internal static partial class UiTextCatalog
{
    /// <summary>Adds the complete six-language errors UI catalog.</summary>
    private static void AddErrorsEntries(Dictionary<string, LocalizedText> entries)
    {
        entries.Add("Error.ApiKeyMissing", new(
            English: "The environment variable {0} is missing or invalid.",
            Italian: "La variabile ambiente {0} è mancante o non valida.",
            French: "La variable d'environnement {0} est absente ou invalide.",
            German: "Die Umgebungsvariable {0} fehlt oder ist ungültig.",
            Spanish: "La variable de entorno {0} falta o no es válida.",
            Vietnamese: "Biến môi trường {0} bị thiếu hoặc không hợp lệ."));
        entries.Add("Error.InteractiveRequired", new(
            English: "This command requires an interactive terminal.",
            Italian: "Questo comando richiede un terminale interattivo.",
            French: "Cette commande nécessite un terminal interactif.",
            German: "Dieser Befehl benötigt ein interaktives Terminal.",
            Spanish: "Este comando requiere un terminal interactivo.",
            Vietnamese: "Lệnh này cần terminal tương tác."));
        entries.Add("Error.KeyRestartRequired", new(
            English: "If you recently changed the key, restart the terminal prompt or open a new session before trying again.",
            Italian: "Se hai modificato recentemente la chiave, riavvia il prompt del terminale o apri una nuova sessione prima di riprovare.",
            French: "Si vous avez récemment modifié la clé, redémarrez l’invite du terminal ou ouvrez une nouvelle session avant de réessayer.",
            German: "Wenn Sie den Schlüssel kürzlich geändert haben, starten Sie die Terminal-Eingabeaufforderung neu oder öffnen Sie eine neue Sitzung, bevor Sie es erneut versuchen.",
            Spanish: "Si has cambiado la clave recientemente, reinicia el terminal o abre una sesión nueva antes de volver a intentarlo.",
            Vietnamese: "Nếu bạn vừa thay đổi khóa, hãy khởi động lại dấu nhắc terminal hoặc mở phiên mới trước khi thử lại."));
        entries.Add("Error.PromptMissing", new(
            English: "Prompt resource '{0}' was not found.",
            Italian: "Risorsa prompt '{0}' non trovata.",
            French: "La ressource de prompt '{0}' est introuvable.",
            German: "Prompt-Ressource '{0}' wurde nicht gefunden.",
            Spanish: "No se encontró el recurso de prompt '{0}'.",
            Vietnamese: "Không tìm thấy tài nguyên prompt '{0}'."));
        entries.Add("Error.RequestFailed", new(
            English: "OpenAI request failed: {0}",
            Italian: "Richiesta OpenAI fallita: {0}",
            French: "La requête OpenAI a échoué : {0}",
            German: "OpenAI-Anfrage fehlgeschlagen: {0}",
            Spanish: "La solicitud de OpenAI falló: {0}",
            Vietnamese: "Yêu cầu OpenAI thất bại: {0}"));
        entries.Add("Error.SetupRequired", new(
            English: "Run --setup before using AI commands.",
            Italian: "Esegui --setup prima di usare i comandi AI.",
            French: "Exécutez --setup avant les commandes IA.",
            German: "Vor KI-Befehlen --setup ausführen.",
            Spanish: "Ejecuta --setup antes de usar comandos de IA.",
            Vietnamese: "Hãy chạy --setup trước khi dùng lệnh AI."));
    }
}
