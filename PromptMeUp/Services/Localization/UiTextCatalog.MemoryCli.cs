// SPDX-License-Identifier: MIT

namespace PromptMeUp.Services;

internal static partial class UiTextCatalog
{
    /// <summary>Adds all six translations for direct saved-memory commands and their verified outcomes.</summary>
    private static void AddMemoryCliEntries(Dictionary<string, LocalizedText> entries)
    {
        entries.Add("MemoryCli.Usage", new(
            English: "Use hm --remember [global|project] <text> or hm --forget <id or description>.",
            Italian: "Usa hm --remember [global|project] <testo> oppure hm --forget <id o descrizione>.",
            French: "Utilisez hm --remember [global|project] <texte> ou hm --forget <id ou description>.",
            German: "Verwenden Sie hm --remember [global|project] <Text> oder hm --forget <ID oder Beschreibung>.",
            Spanish: "Usa hm --remember [global|project] <texto> o hm --forget <id o descripción>.",
            Vietnamese: "Dùng hm --remember [global|project] <nội dung> hoặc hm --forget <ID hoặc mô tả>."));
        entries.Add("MemoryCli.RememberHelp", new(
            English: "Save a note locally; project scope is the default",
            Italian: "Salva una nota locale; l'ambito predefinito è il progetto",
            French: "Enregistrer une note locale ; portée projet par défaut",
            German: "Notiz lokal speichern; Standard ist der Projektbereich",
            Spanish: "Guardar una nota local; ámbito de proyecto por defecto",
            Vietnamese: "Lưu ghi chú cục bộ; mặc định thuộc phạm vi dự án"));
        entries.Add("MemoryCli.ForgetHelp", new(
            English: "Delete one note by ID or exact text; descriptions use AI lookup",
            Italian: "Elimina una nota per ID o testo esatto; le descrizioni usano la ricerca AI",
            French: "Supprimer une note par ID ou texte exact ; recherche IA pour les descriptions",
            German: "Eine Notiz nach ID oder genauem Text löschen; Beschreibungen nutzen KI-Suche",
            Spanish: "Eliminar una nota por ID o texto exacto; las descripciones usan búsqueda con IA",
            Vietnamese: "Xóa một ghi chú theo ID hoặc nội dung chính xác; mô tả dùng AI để tìm"));
        entries.Add("MemoryCli.Choose", new(
            English: "Choose the memory to forget", Italian: "Scegli la memoria da dimenticare",
            French: "Choisissez la mémoire à oublier", German: "Erinnerung zum Vergessen auswählen",
            Spanish: "Elige la memoria que quieres olvidar", Vietnamese: "Chọn ghi nhớ muốn quên"));
        entries.Add("MemoryCli.Page", new(
            English: "Page {0} of {1}", Italian: "Pagina {0} di {1}", French: "Page {0} sur {1}",
            German: "Seite {0} von {1}", Spanish: "Página {0} de {1}", Vietnamese: "Trang {0} trên {1}"));
        entries.Add("MemoryCli.Next", new(
            English: "Next page (returns to the first after the last)", Italian: "Pagina successiva (dopo l'ultima torna alla prima)",
            French: "Page suivante (revient à la première après la dernière)", German: "Nächste Seite (nach der letzten wieder die erste)",
            Spanish: "Página siguiente (tras la última vuelve a la primera)", Vietnamese: "Trang tiếp (sau trang cuối trở về trang đầu)"));
        entries.Add("MemoryCli.DigitHint", new(
            English: "Press a displayed number; no Enter needed. 0 cancels.",
            Italian: "Premi uno dei numeri mostrati; non serve Invio. 0 annulla.",
            French: "Appuyez sur un numéro affiché ; Entrée inutile. 0 annule.",
            German: "Drücken Sie eine angezeigte Ziffer; kein Enter nötig. 0 bricht ab.",
            Spanish: "Pulsa un número mostrado; no hace falta Intro. 0 cancela.",
            Vietnamese: "Nhấn một số đang hiển thị; không cần Enter. 0 để hủy."));
        entries.Add("MemoryCli.Changed", new(
            English: "The note changed or was removed during lookup. Nothing was deleted; review the memories and retry.",
            Italian: "La nota è cambiata o è stata rimossa durante la ricerca. Non è stato eliminato nulla; controlla le memorie e riprova.",
            French: "La note a changé ou a été retirée pendant la recherche. Rien n'a été supprimé ; vérifiez les notes et réessayez.",
            German: "Die Notiz wurde während der Suche geändert oder entfernt. Nichts wurde gelöscht; prüfen Sie die Notizen und versuchen Sie es erneut.",
            Spanish: "La nota cambió o se quitó durante la búsqueda. No se eliminó nada; revisa las notas y vuelve a intentarlo.",
            Vietnamese: "Ghi chú đã thay đổi hoặc bị xóa trong lúc tìm. Chưa xóa gì; kiểm tra ghi chú rồi thử lại."));
        entries.Add("MemoryCli.Forgotten", new(
            English: "Memory {0} deleted.", Italian: "Memoria {0} eliminata.", French: "Mémoire {0} supprimée.",
            German: "Erinnerung {0} gelöscht.", Spanish: "Memoria {0} eliminada.", Vietnamese: "Đã xóa ghi nhớ {0}."));
        entries.Add("MemoryCli.InvalidResponse", new(
            English: "AI memory lookup returned an invalid selection. Nothing was deleted.",
            Italian: "La ricerca AI delle memorie ha restituito una selezione non valida. Non è stato eliminato nulla.",
            French: "La recherche IA a renvoyé une sélection non valide. Rien n'a été supprimé.",
            German: "Die KI-Suche lieferte eine ungültige Auswahl. Nichts wurde gelöscht.",
            Spanish: "La búsqueda con IA devolvió una selección no válida. No se eliminó nada.",
            Vietnamese: "AI tìm ghi chú trả về lựa chọn không hợp lệ. Chưa xóa gì."));
    }
}
