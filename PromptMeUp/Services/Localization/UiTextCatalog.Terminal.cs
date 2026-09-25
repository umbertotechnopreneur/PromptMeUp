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
        entries.Add("Terminal.TurnPosition", new("Turn {0}/{1}", "Turno {0}/{1}", "Tour {0}/{1}", "Beitrag {0}/{1}", "Turno {0}/{1}", "Lượt {0}/{1}"));
        entries.Add("Terminal.HistoryKeys", new(
            "←/→ turns · ↑/↓ scroll · PgUp/PgDn page · Enter/Esc back",
            "←/→ turni · ↑/↓ scorri · PgSu/PgGiù pagina · Invio/Esc torna",
            "←/→ tours · ↑/↓ défiler · PgUp/PgDn page · Entrée/Esc retour",
            "←/→ Beiträge · ↑/↓ scrollen · PgUp/PgDn Seite · Enter/Esc zurück",
            "←/→ turnos · ↑/↓ desplazar · PgUp/PgDn página · Intro/Esc volver",
            "←/→ lượt · ↑/↓ cuộn · PgUp/PgDn trang · Enter/Esc trở lại"));
        entries.Add("Terminal.NavigationKeys", new(
            "Ctrl+PgUp/PgDn turns · F2 details", "Ctrl+PgSu/PgGiù turni · F2 dettagli",
            "Ctrl+PgUp/PgDn tours · F2 détails", "Ctrl+PgUp/PgDn Beiträge · F2 Details",
            "Ctrl+PgUp/PgDn turnos · F2 detalles", "Ctrl+PgUp/PgDn lượt · F2 chi tiết"));
        entries.Add("Terminal.DetailsAvailable", new(
            "F2: open full details", "F2: apri i dettagli completi", "F2 : ouvrir les détails complets",
            "F2: vollständige Details öffnen", "F2: abrir todos los detalles", "F2: mở toàn bộ chi tiết"));
        entries.Add("Terminal.ReviewKeys", new(
            "Enter: continue · Ctrl+PgUp/PgDn: turns · Esc: close",
            "Invio: continua · Ctrl+PgSu/PgGiù: turni · Esc: chiudi",
            "Entrée : continuer · Ctrl+PgUp/PgDn : tours · Esc : fermer",
            "Enter: weiter · Ctrl+PgUp/PgDn: Beiträge · Esc: schließen",
            "Intro: continuar · Ctrl+PgUp/PgDn: turnos · Esc: cerrar",
            "Enter: tiếp tục · Ctrl+PgUp/PgDn: lượt · Esc: đóng"));
    }
}
