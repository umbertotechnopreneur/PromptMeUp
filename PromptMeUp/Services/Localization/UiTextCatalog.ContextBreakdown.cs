// SPDX-License-Identifier: MIT

namespace PromptMeUp.Services;

internal static partial class UiTextCatalog
{
    /// <summary>Adds the six-language context legend, separating command output while keeping the guide within system.</summary>
    private static void AddContextBreakdownEntries(Dictionary<string, LocalizedText> entries)
    {
        entries.Add("Shell.ContextSystem", new(
            English: "System",
            Italian: "Sistema",
            French: "Système",
            German: "System",
            Spanish: "Sistema",
            Vietnamese: "Hệ thống"));
        entries.Add("Shell.ContextUser", new(
            English: "User prompts",
            Italian: "Prompt utente",
            French: "Messages utilisateur",
            German: "Nutzereingaben",
            Spanish: "Mensajes del usuario",
            Vietnamese: "Lời nhắc người dùng"));
        entries.Add("Shell.ContextTool", new(
            English: "Tool output",
            Italian: "Output tool",
            French: "Résultat d’outil",
            German: "Tool-Ausgabe",
            Spanish: "Salida de herramientas",
            Vietnamese: "Đầu ra công cụ"));
        entries.Add("Shell.ContextAssistant", new(
            English: "AI replies",
            Italian: "Risposte AI",
            French: "Réponses IA",
            German: "KI-Antworten",
            Spanish: "Respuestas de IA",
            Vietnamese: "Phản hồi AI"));
        entries.Add("Shell.ContextFree", new(
            English: "Free capacity",
            Italian: "Spazio libero",
            French: "Espace disponible",
            German: "Freier Platz",
            Spanish: "Espacio disponible",
            Vietnamese: "Dung lượng còn trống"));
        entries.Add("Shell.ContextGuideIncluded", new(
            English: "Guide included in system",
            Italian: "Guida inclusa nel sistema",
            French: "Guide inclus dans le système",
            German: "Anleitung im System enthalten",
            Spanish: "Guía incluida en el sistema",
            Vietnamese: "Hướng dẫn nằm trong hệ thống"));
        entries.Add("Shell.ContextTokenEstimate", new(
            English: "~{0} tokens",
            Italian: "~{0} token",
            French: "~{0} jetons",
            German: "~{0} Tokens",
            Spanish: "~{0} tokens",
            Vietnamese: "~{0} token"));
    }
}
