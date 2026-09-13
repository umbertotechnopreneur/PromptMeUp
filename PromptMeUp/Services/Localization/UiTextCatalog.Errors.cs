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
            English: "If you recently changed the key, fully close and reopen the terminal application before trying again (including the IDE for an integrated terminal).",
            Italian: "Se hai modificato recentemente la chiave, chiudi completamente e riapri l'applicazione del terminale prima di riprovare (anche l'IDE se usi un terminale integrato).",
            French: "Si vous avez récemment modifié la clé, fermez complètement puis rouvrez l’application du terminal avant de réessayer (y compris l’IDE pour un terminal intégré).",
            German: "Wenn Sie den Schlüssel kürzlich geändert haben, schließen Sie die Terminalanwendung vollständig und öffnen Sie sie erneut, bevor Sie es noch einmal versuchen (bei einem integrierten Terminal auch die IDE).",
            Spanish: "Si has cambiado la clave recientemente, cierra por completo y vuelve a abrir la aplicación del terminal antes de volver a intentarlo (también el IDE si usas un terminal integrado).",
            Vietnamese: "Nếu bạn vừa thay đổi khóa, hãy đóng hoàn toàn rồi mở lại ứng dụng terminal trước khi thử lại (bao gồm cả IDE nếu dùng terminal tích hợp)."));
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
