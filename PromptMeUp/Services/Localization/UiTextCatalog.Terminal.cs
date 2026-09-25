// SPDX-License-Identifier: MIT

namespace PromptMeUp.Services;

internal static partial class UiTextCatalog
{
    /// <summary>Adds the shared identities and quiet status vocabulary for all waterfall surfaces.</summary>
    private static void AddTerminalEntries(Dictionary<string, LocalizedText> entries)
    {
        entries.Add("Terminal.Role.User", new("You", "Tu", "Vous", "Du", "Tú", "Bạn"));
        entries.Add("Terminal.Role.Assistant", new("PromptMeUp", "PromptMeUp", "PromptMeUp", "PromptMeUp", "PromptMeUp", "PromptMeUp"));
        entries.Add("Terminal.Role.Plan", new("Plan", "Piano", "Plan", "Plan", "Plan", "Kế hoạch"));
        entries.Add("Terminal.Role.Script", new("Script", "Script", "Script", "Skript", "Script", "Mã lệnh"));
        entries.Add("Terminal.Role.Tool", new("Tool", "Tool", "Outil", "Werkzeug", "Herramienta", "Công cụ"));
        entries.Add("Terminal.Mode.Chat", new("Chat", "Chat", "Discussion", "Chat", "Chat", "Trò chuyện"));
        entries.Add("Terminal.Mode.Plan", new("Plan", "Piano", "Plan", "Plan", "Plan", "Kế hoạch"));
        entries.Add("Terminal.Mode.Script", new("Script", "Script", "Script", "Skript", "Script", "Mã lệnh"));
        entries.Add("Terminal.Mode.Diagnose", new("Diagnose", "Diagnosi", "Diagnostic", "Diagnose", "Diagnóstico", "Chẩn đoán"));
        entries.Add("Terminal.Mode.Explain", new("Explain", "Spiegazione", "Explication", "Erklärung", "Explicación", "Giải thích"));
        entries.Add("Terminal.State.Ready", new("Ready", "Pronto", "Prêt", "Bereit", "Listo", "Sẵn sàng"));
        entries.Add("Terminal.State.Working", new("Working", "Sto lavorando", "En cours", "In Arbeit", "Trabajando", "Đang xử lý"));
        entries.Add("Terminal.State.NeedsInput", new("Your choice", "Serve una scelta", "À vous de choisir", "Auswahl nötig", "Elige una opción", "Cần lựa chọn"));
        entries.Add("Terminal.State.Completed", new("Completed", "Completato", "Terminé", "Abgeschlossen", "Completado", "Hoàn tất"));
        entries.Add("Terminal.State.Cancelled", new("Cancelled", "Annullato", "Annulé", "Abgebrochen", "Cancelado", "Đã hủy"));
        entries.Add("Terminal.State.Failed", new("Failed", "Non riuscito", "Échec", "Fehlgeschlagen", "Error", "Thất bại"));
    }
}
